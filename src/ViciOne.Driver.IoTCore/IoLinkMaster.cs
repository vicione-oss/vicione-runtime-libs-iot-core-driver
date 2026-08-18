using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ifm.Common.Variant;
using ifm.IoddParser.Contracts;
using ifm.IoddParser.Helpers;
using ifm.IoTCore.Common.Exceptions;
using ifm.IoTCore.Contracts;
using ifm.IoTCore.ElementManager.Contracts.Elements;
using ifm.IoTCore.Profile.DeviceManagement;
using ifm.IoTCore.Profile.DeviceManagement.ServiceData.Requests;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.IoTCore.Extensions;
using ViciOne.Communication.IoTCore.IoddStore;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.IoTCore.Interfaces;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// Owns the connection to one ifm IO-Link master: mirrors its device tree, polls every port's connected-device
/// identity and process data, and dispatches decoded values to whichever <see cref="IIoLinkSubscriberFunctionBlock"/>s
/// are registered for them.
/// <para>Shared across multiple driver instances via <see cref="DeviceManager"/> rather than owned by any one
/// of them.</para>
/// </summary>
public sealed partial class IoLinkMaster : IDisposable
{
    /// <summary>
    /// The default display name for a connected IO-Link device with no name of its own yet.
    /// </summary>
    public static string DefaultDeviceName { get; } = "IO-Link Device";

    /// <summary>
    /// The default display name for the master itself.
    /// </summary>
    public static string DefaultName { get; } = "IO-Link Master";

    /// <summary>
    /// The default HTTP port for a master address with none specified.
    /// </summary>
    public static int DefaultPort { get; } = 80;

    private const int ErrorWaitTime = 5000;
    private const int PollingWaitTime = 1000;
    // The IO-Link master exposes each connected device's identity and status under these fixed address
    // suffixes on its mirrored device element.
    private const string DeviceIdAddressSuffix = "/deviceid";
    private const string DeviceStatusAddressSuffix = "/status";
    private const string VendorIdAddressSuffix = "/vendorid";

    /// <summary>
    /// The cache lifetime, in seconds, applied to every value read from a mirrored device. Zero keeps
    /// the polling loop reading through to the device: anything above it serves process data from the cache
    /// instead, and the polling loop cannot see values it never fetches. Leaving this unset falls back to the
    /// factory's own default of 30 seconds, which measurably froze process data (1 of 40 one-second samples
    /// carried a new value; with zero it was 39 of 40, at the same cost per read).
    /// </summary>
    private const int ValueCacheSeconds = 0;
    private const string PdInAddressSuffix = "/pdin";

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ifm.IoTCore.Device.Contracts.DeviceManager _deviceManager;
    private readonly DeviceManagementProfileBuilder _deviceProfileBuilder;
    private bool _disposed;
    private readonly Lock _getDataLock = new();
    private readonly SemaphoreSlim _requestGate = new(1, 1);
    private readonly IDisposable _clientNetAdapterFactory;
    private string _lastLoggedMessage = string.Empty;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<int, DeviceProcessDataRegistration> _portProcessDataRegistrations = [];
    private readonly ConcurrentDictionary<IIoLinkMasterDiagnosticSubscriber, byte> _masterDiagnosticSubscribers = [];
    // The master's diagnostic units (°C/mV/mA) are static, so read once on the first dispatch and reused.
    private (string Temperature, string Voltage, string Current)? _masterDiagnosticUnits;
    private bool _mirroringErrorLogged;
    private readonly Task _mirroringTask;
    // The "type" string the DeviceManagementProfileBuilder expects in AddDeviceRequestServiceData -
    // read off the registered IoTCoreDeviceFactory rather than hardcoded, since it's not otherwise documented.
    private readonly string _deviceType;
    // Set once mirroring succeeds (see MirroringAsync) - the actual mounted element's address, needed for
    // RemoveDevice in Dispose since AddDevice does not reliably report it back (see MirroringAsync).
    private string? _mirroredDeviceAddress;
    // Detected but not-yet-reported change in child device availability, see PollingAsync.
    private bool _pendingChildDevicesChanged;
    // Set on a fresh mirror so the first reachable polling cycle announces the tree once, even when no port
    // reports a change - otherwise a master that only becomes reachable after start (its first-ever probe finds
    // the devices already present, which the per-port change detection does not treat as a change) would never
    // publish its now-online tree, and consumers would keep showing it offline. See ReportPendingChildDevicesChanged.
    private bool _announceTreeAfterMirror;
    private Task? _pollingTask;
    private PeriodicTimer? _pollingTimer;
    private Dictionary<int, IoLinkMasterPortDeviceInfo>? _portDeviceInfos;
    private readonly Lazy<RecordingManager> _recordingManager;
    // When set, every port's cyclic data is read in a single "getdatamulti" request per polling cycle instead
    // of one request per element - see PollingAsync/PrefetchPortData.
    private readonly bool _useGetDataMulti;
    // The values read by the current cycle's single "getdatamulti" request, keyed by full element address, or
    // null outside a prefetched cycle - GetDataValueAsValue serves reads from here while it is set.
    private Dictionary<string, VariantValue>? _prefetchedValues;

    /// <summary>
    /// The underlying ifm.IoTCore connection.
    /// </summary>
    public IIoTCore Core { get; }

    /// <summary>
    /// Caches IODD condition variable values already read from this master's devices.
    /// </summary>
    internal ConditionCache ConditionValueCache { get; } = new();

    /// <summary>
    /// Last successfully parsed device tree node per port, keyed by port index.
    /// <para>Reused by <c>IoLinkParser.ParseDeviceAsync</c> for ports whose
    /// <see cref="IoLinkMasterPortDeviceInfo.NeedsFullRefresh"/> is false, instead of re-reading that device's
    /// IODD data and product info again on every single full parse.</para>
    /// </summary>
    internal Dictionary<int, DeviceTreeDevice> CachedPortDeviceNodes { get; } = [];

    /// <summary>
    /// Whether the device tree has been mirrored at least once (see <see cref="MirroringAsync"/>).
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    /// When this (otherwise still-shared) instance should actually be disposed - set by <see cref="DeviceManager"/>.
    /// </summary>
    internal DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// The master's address, as passed to the constructor.
    /// </summary>
    public string Address { get; }

    /// <summary>
    /// Whether the master itself is currently unreachable, as opposed to an individual port's device being unavailable.
    /// </summary>
    public bool Timeout { get; private set; }

    /// <summary>
    /// The master's address, parsed as a <see cref="System.Uri"/>.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// The username this master was configured to authenticate with, or <see langword="null"/> for no authentication.
    /// <para>Carried through to <c>IoLinkParser.ParseDeviceAsync</c>'s published device tree, since that tree is
    /// what the engineering UI uses to configure - and later regenerate - this master's FunctionBlocks.</para>
    /// </summary>
    internal string? Username { get; }

    /// <summary>
    /// The password this master was configured to authenticate with, ignored if <see cref="Username"/> is <see langword="null"/>.
    /// <para>Carried through to <c>IoLinkParser.ParseDeviceAsync</c>'s published device tree for the same reason
    /// as <see cref="Username"/>.</para>
    /// </summary>
    internal string? Password { get; }

    /// <summary>
    /// This master's security info, as reported by its own <c>GetIdentity</c> service's <c>security</c> block.
    /// Queried once, right after a successful mirror (see <see cref="MirroringAsync"/>) - keeps its default
    /// (unauthenticated-looking) values beforehand, or if the master's firmware doesn't report a <c>security</c>
    /// block at all (older masters just omit it).
    /// </summary>
    internal DeviceTreeIoLinkMasterSecurity Security { get; private set; } = new();

    /// <summary>
    /// Raised with a finished BLOB-channel recording's result.
    /// </summary>
    public event Action<Measurement>? BlobDataReceived;

    /// <summary>
    /// Raised when a port's connected-device availability or identity changed.
    /// </summary>
    public event Action? ChildDevicesChanged;

    /// <summary>
    /// Raised when <see cref="Timeout"/> changed.
    /// </summary>
    public event Action? TimeoutChanged;

    /// <summary>
    /// Creates a new master connection and starts mirroring its device tree in the background.
    /// </summary>
    /// <param name="address">The IO-Link master's address.</param>
    /// <param name="logger">The logger to report connection and polling issues to.</param>
    /// <param name="username">The username to authenticate with, or <see langword="null"/> for no authentication.</param>
    /// <param name="password">The password to authenticate with, ignored if <paramref name="username"/> is <see langword="null"/>.</param>
    /// <param name="useGetDataMulti">Whether to read every port's cyclic data in one "getdatamulti" request per polling cycle instead of one request per element.</param>
    public IoLinkMaster(string address, ILogger logger, string? username = null, string? password = null, bool useGetDataMulti = false)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException($"{nameof(Address)} cannot be null or empty.", nameof(address));

        Address = address;
        _logger = logger;
        Username = username;
        Password = password;
        _useGetDataMulti = useGetDataMulti;

        // A master accepts credentials on no transport but HTTPS, so a master reached with a user name is
        // reached over HTTPS on the standard secure port; one reached without is left on the address as given.
        // The presence of a user name is what decides this: it is only ever set for a master that asked for
        // one, so no separate check against the master is needed - and none that could fail while it is
        // unreachable.
        bool authenticate = !string.IsNullOrWhiteSpace(username);
        UriBuilder uriBuilder = new(address);
        if (authenticate)
        {
            uriBuilder.Scheme = "https";
            if (uriBuilder.Port is 80 or -1)
                uriBuilder.Port = 443;
        }
        Uri = uriBuilder.Uri;

        ConnectionComponents connection = IoLinkMasterConnectionFactory.Build(Uri, logger, TimeSpan.FromSeconds(30));
        Core = connection.Core;
        _clientNetAdapterFactory = connection.ClientNetAdapterFactory;
        _deviceManager = connection.DeviceManager;
        _deviceType = connection.DeviceType;
        _deviceProfileBuilder = connection.ProfileBuilder;

        _mirroringTask = Task.Run(MirroringTaskAsync);
        _recordingManager = new Lazy<RecordingManager>(CreateRecordingManager);
    }

    /// <summary>
    /// Acquires exclusive access to the IO-Link Master, pausing the polling loop
    /// until the returned IDisposable is disposed.
    /// </summary>
    internal async Task<IDisposable> AcquireExclusiveAccessAsync(CancellationToken cancellationToken = default)
    {
        await _requestGate.WaitAsync(cancellationToken);
        return new RequestGateReleaser(_requestGate);
    }

    private sealed class RequestGateReleaser(SemaphoreSlim gate) : IDisposable
    {
        public void Dispose() => gate.Release();
    }

    /// <summary>
    /// Evaluates whether an IODD <paramref name="condition"/> is met for the given device, reading and caching
    /// the referenced condition variable's value on the master if not already cached.
    /// </summary>
    private bool CheckCondition(ICondition condition, string deviceAddress, ushort vendorId, uint deviceId)
    {
        // If there is no condition use the item.
        if (condition == null) return true;

        // Reading variables can be very slow, and reading the same variable multiple times is inefficient.
        // So read the value once and store it in a cache. The variable value will not change during runtime.
        if (!ConditionValueCache.Get(deviceAddress, condition.Variable.Index, condition.SubIndex, vendorId, deviceId, out byte value))
        {
            // The variable referenced in the condition must be simple and the value must be convertible to byte.
            byte[]? readValue = null;
            try
            {
                readValue = Core.ReadAcyclic(deviceAddress, condition.Variable.Index, condition.SubIndex);
            }
            catch (Exception ex)
            {
                _logger.AcyclicReadFailed(ex, condition.Variable.Index, condition.SubIndex, deviceAddress);
            }

            if (readValue is { Length: > 0 })
            {
                value = readValue[0];
            }
            else
            {
                // Fallback: take default value from IODD variable definition.
                value = Convert.ToByte(condition.Variable.GetDefaultValue(condition.SubIndex), CultureInfo.InvariantCulture);
            }

            // Store condition value in cache.
            ConditionValueCache.Add(deviceAddress, condition.Variable.Index, condition.SubIndex, vendorId, deviceId, value);
        }

        // Compare condition value against value 
        return condition.Value == value;
    }

    /// <summary>
    /// Builds the per-port tracking state for every IO-Link port found under <paramref name="deviceElement"/>.
    /// </summary>
    private static Dictionary<int, IoLinkMasterPortDeviceInfo> CreatePortInfos(IBaseElement deviceElement)
        => IoLinkParser.GetIoLinkMasterDevices(deviceElement).Select(
            portDeviceElement =>
            {
                int portIndex = IoLinkParser.GetPortIndex(portDeviceElement.Parent);
                return new IoLinkMasterPortDeviceInfo
                {
                    Address = portDeviceElement.Address,
                    DeviceIdAddress = $"{portDeviceElement.Address}{DeviceIdAddressSuffix}",
                    DeviceStatusAddress = $"{portDeviceElement.Address}{DeviceStatusAddressSuffix}",
                    PortModeAddress = $"{portDeviceElement.Parent.Address}/mode",
                    PortIndex = portIndex,
                    VendorIdAddress = $"{portDeviceElement.Address}{VendorIdAddressSuffix}",
                };
            }).ToDictionary(x => x.PortIndex, x => x);

    /// <summary>
    /// Creates this master's lazily-constructed <see cref="RecordingManager"/>, forwarding its
    /// <see cref="RecordingManager.BlobDataReceived"/> event through this master's own <see cref="BlobDataReceived"/>.
    /// </summary>
    private RecordingManager CreateRecordingManager()
    {
        RecordingManager manager = new(Address, _logger);
        manager.BlobDataReceived += BlobDataReceived;
        return manager;
    }

    /// <summary>
    /// Delays for <paramref name="millisecondsDelay"/>, swallowing cancellation so callers on the mirroring/polling
    /// loops don't need their own try/catch around it during shutdown.
    /// </summary>
    private async Task DelayAndIgnoreCancellation(int millisecondsDelay)
    {
        try { await Task.Delay(millisecondsDelay, _cancellationTokenSource.Token); }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Stops the mirroring/polling loops, unmirrors the device tree if mirrored, and releases the underlying
    /// connection and recording resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource.Cancel();

        _mirroringTask?.Wait();

        _pollingTimer?.Dispose();
        _pollingTimer = null;
        _pollingTask?.Wait();

        if (Initialized)
        {
            _deviceProfileBuilder.RemoveDevice(new RemoveDeviceRequestServiceData(_mirroredDeviceAddress!), acquireLock: true);
            Initialized = false;
        }

        ClearConditionValueCache();
        _clientNetAdapterFactory.Dispose();
        _deviceManager.Dispose();
        _requestGate.Dispose();

        if (_recordingManager.IsValueCreated)
        {
            _recordingManager.Value.BlobDataReceived -= BlobDataReceived;
            _recordingManager.Value.Dispose();
        }

        BlobDataReceived = null;

        _portProcessDataRegistrations.Clear();
        _cancellationTokenSource.Dispose();

        _disposed = true;
    }

    /// <summary>
    /// Clears every cached IODD condition variable value for this master.
    /// </summary>
    internal void ClearConditionValueCache()
        => ConditionValueCache.Clear();

    /// <summary>
    /// The current per-port device info tracked by the polling loop, if mirroring has completed at least once.
    /// </summary>
    internal IEnumerable<IoLinkMasterPortDeviceInfo> PortDeviceInfos
        => _portDeviceInfos?.Values ?? Enumerable.Empty<IoLinkMasterPortDeviceInfo>();

    /// <summary>
    /// Gets the last polled availability and identity for the device connected to the given port.
    /// </summary>
    /// <param name="portIndex">The port's index.</param>
    /// <returns>
    /// The port's availability info, or <see langword="null"/> if mirroring has not completed yet or the port
    /// index is unknown.
    /// </returns>
    internal IoLinkMasterPortDeviceAvailabilityInfo? GetPortDeviceAvailabilityInfo(int portIndex)
    {
        //TODO: lock?
        if (_portDeviceInfos is null || _portDeviceInfos.TryGetValue(portIndex, out IoLinkMasterPortDeviceInfo? portDeviceInfo) is false)
            return null;

        return new IoLinkMasterPortDeviceAvailabilityInfo
        {
            Available = portDeviceInfo.Available ?? false,
            DeviceId = portDeviceInfo.DeviceId ?? 0,
            VendorId = portDeviceInfo.VendorId ?? 0,
        };
    }

    /// <summary>
    /// Resolves the IODD device definition for every port registration that doesn't have one yet.
    /// <para>Deferred until here, rather than resolved at registration time, since the FunctionBlock that
    /// registers may run before the IODD catalog is available.</para>
    /// </summary>
    private void InitializeRegistrations()
    {
        foreach (KeyValuePair<int, DeviceProcessDataRegistration> registration in _portProcessDataRegistrations)
        {
            if (registration.Value.DeviceDefinition is not null)
                continue;

            IDevice? device = null;
            try { device = IoddCatalog.GetIoddDevice(registration.Value.VendorId, registration.Value.DeviceId); }
            catch (Exception ex) { _logger.ErrorWhileLoadingIodd(Address, registration.Value.VendorId, registration.Value.DeviceId, ex); }
            if (device is null)
            {
                LogUnknownDeviceFound(registration.Value.VendorId, registration.Value.DeviceId);
                continue;
            }

            registration.Value.DeviceDefinition = device;
        }
    }

    /// <summary>
    /// Reads a single element's value from the master, serializing concurrent calls and tracking the master-wide
    /// <see cref="Timeout"/> flag.
    /// </summary>
    /// <param name="elementAddress">The element address to read.</param>
    /// <param name="skipIfInTimeout">
    /// If <see langword="true"/>, the read is skipped (returning <see langword="null"/>) while <see cref="Timeout"/>
    /// is already set, instead of attempting - and waiting out - another doomed request.
    /// </param>
    /// <param name="skippedBecauseOfTimeout">Whether the read was skipped because of an active timeout.</param>
    /// <param name="isConnectivityProbe">
    /// Only the periodic port mode read in <see cref="PollingAsync"/> is a dedicated master connectivity check
    /// and is allowed to set this parameter to <see langword="true"/>. A single device's data (vendor/device id, process
    /// data, IODD fields, ...) can fail to be read for reasons specific to that one device, so those reads must not
    /// affect the master-wide <see cref="Timeout"/> flag - doing so would mark the whole master offline whenever a
    /// single sensor is unreachable.
    /// </param>
    /// <returns>The element's value, or <see langword="null"/> if skipped.</returns>
    internal VariantValue? GetDataValueAsValue(string elementAddress, bool skipIfInTimeout, out bool skippedBecauseOfTimeout, bool isConnectivityProbe = false)
    {
        // Inside a prefetched cycle every value was already read in one batch request (see PrefetchPortData),
        // which also resolved the master-wide Timeout - so serve from that batch instead of a live request.
        Dictionary<string, VariantValue>? prefetched = _prefetchedValues;
        if (prefetched is not null)
        {
            skippedBecauseOfTimeout = false;
            return prefetched.GetValueOrDefault(elementAddress);
        }

        Exception? timeoutException;
        bool timeoutBefore = Timeout;

        // If a timeout is already in effect, don't start another request (unless explicitly asked whether the
        // timeout is over: skipIfInTimeout == false).
        skippedBecauseOfTimeout = skipIfInTimeout && Timeout;
        if (skippedBecauseOfTimeout)
            return null;

        // HACK: If parallel requests are made to the device and both run into a timeout, different exceptions
        //       come back. To prevent that, all GetDataValueAsValue calls should run serially.
        lock (_getDataLock)
        {
            // Re-checked here inside the lock so no further request is started and runs into a timeout again,
            // if the current request already detected a timeout.
            skippedBecauseOfTimeout = skipIfInTimeout && Timeout;
            if (skippedBecauseOfTimeout)
                return null;

            try
            {
                return Core.GetDataValueAsValue(elementAddress);
            }
            catch (Exception ex) when (isConnectivityProbe && Timeout is false)
            {
                // The connectivity probe is the cycle's top-level reachability check: if it fails for ANY reason -
                // a transport timeout, a connection error such as "no route to host", anything else - the master
                // could not be reached and no values can be read this cycle, so it is unreachable.
                Timeout = true;
                timeoutException = ex;
            }
        }

        if (timeoutBefore != Timeout)
            TimeoutChanged?.Invoke();

        ExceptionDispatchInfo.Capture(timeoutException).Throw();
        // Satisfy the compiler...
        return null;
    }

    /// <summary>
    /// Reads every port's cyclic data (mode, device status, vendor/device id and process-data-in) in a single
    /// "getdatamulti" request and stores it in <see cref="_prefetchedValues"/> for the rest of the polling cycle,
    /// so <see cref="ProbePorts"/> and <see cref="ReadAndDispatchProcessData"/> serve their reads from it.
    /// <para>This one request is also the cycle's connectivity check: a transport timeout sets the master-wide
    /// <see cref="Timeout"/> (and skips probing this cycle), a success clears it. Non-timeout failures are logged
    /// and skip the cycle too.</para>
    /// </summary>
    /// <returns><see langword="true"/> if the batch succeeded and probing should proceed, <see langword="false"/> if the master was unreachable.</returns>
    private bool PrefetchPortData()
    {
        List<string> addresses = [];
        foreach (IoLinkMasterPortDeviceInfo portDeviceInfo in _portDeviceInfos!.Values)
        {
            addresses.Add(portDeviceInfo.PortModeAddress);
            addresses.Add(portDeviceInfo.DeviceStatusAddress);
            addresses.Add(portDeviceInfo.VendorIdAddress);
            addresses.Add(portDeviceInfo.DeviceIdAddress);
            addresses.Add($"{portDeviceInfo.Address}{PdInAddressSuffix}");
        }

        // With a diagnostics subscriber registered, fold the master's own diagnostic values into this same batch
        // (cyclic values on the same device) instead of reading them one by one afterwards. The units are read
        // only until cached, so they are included only while _masterDiagnosticUnits has not been read yet.
        if (_masterDiagnosticSubscribers.IsEmpty is false && _mirroredDeviceAddress is not null)
        {
            addresses.Add($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterTemperature}");
            addresses.Add($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterVoltage}");
            addresses.Add($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterCurrent}");
            addresses.Add($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterSupervisionStatus}");
            if (_masterDiagnosticUnits is null)
            {
                addresses.Add($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterTemperature}{IoLinkParser.AddressUnitSuffix}");
                addresses.Add($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterVoltage}{IoLinkParser.AddressUnitSuffix}");
                addresses.Add($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterCurrent}{IoLinkParser.AddressUnitSuffix}");
            }
        }

        try
        {
            _prefetchedValues = GetDataMulti(addresses);
        }
        catch (Exception ex)
        {
            _prefetchedValues = null;
            // This single getdatamulti is the cycle's top-level read: any failure means the master could not be
            // reached (transport timeout, connection error, ...), so mark it unreachable regardless of the type.
            if (Timeout is false)
            {
                Timeout = true;
                TimeoutChanged?.Invoke();
            }
            LogPollingError(ex);
            return false;
        }

        if (Timeout)
        {
            Timeout = false;
            TimeoutChanged?.Invoke();
            LogConnectionReEstablished();
        }
        return true;
    }

    /// <summary>
    /// Reads the given element addresses from the master in a single "getdatamulti" request, serialized against
    /// the master's other requests.
    /// </summary>
    /// <param name="elementAddresses">The element addresses to read, in this master's mirrored address space.</param>
    /// <returns>The read values keyed by their (full) element address; an address the master did not answer is absent.</returns>
    private Dictionary<string, VariantValue> GetDataMulti(IReadOnlyCollection<string> elementAddresses)
    {
        lock (_getDataLock)
            return PortDataBatchReader.Read(Core, _mirroredDeviceAddress!, elementAddresses);
    }

    /// <summary>
    /// Queues a BLOB-channel raw-data recording request for the given port on this master's <see cref="RecordingManager"/>.
    /// </summary>
    /// <param name="identifier">An identifier for correlating this request with its eventual result.</param>
    /// <param name="portIndex">The port to record from.</param>
    /// <param name="ignoreTimedValueArray">Whether to skip decoding the recorded raw data into timed values.</param>
    public void GetRecordingBlobData(string identifier, int portIndex, bool ignoreTimedValueArray)
        => _recordingManager.Value.GetRecordingBlobData(identifier, portIndex, ignoreTimedValueArray);

    /// <summary>
    /// Repeatedly attempts <see cref="MirroringAsync"/> until it succeeds or this master is disposed.
    /// </summary>
    private async Task MirroringTaskAsync()
    {
        while (Initialized == false && _cancellationTokenSource.IsCancellationRequested == false)
            await MirroringAsync();
    }

    /// <summary>
    /// Mirrors the master's device tree once and, on success, builds the per-port tracking state and starts the
    /// polling loop.
    /// <para>On failure, logs the error once (repeats are suppressed) and waits before the next attempt.</para>
    /// </summary>
    private async Task MirroringAsync()
    {
        try
        {
            // A master with security enabled expects the user name and password Base64-encoded in the request,
            // so they are encoded here before being sent.
            static string EncodeBase64(string? value)
                => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value ?? string.Empty));

            AuthenticationRequestServiceData? authentication = string.IsNullOrWhiteSpace(Username)
                ? null
                : new AuthenticationRequestServiceData(EncodeBase64(Username), EncodeBase64(Password));

            AddDeviceRequestServiceData addRequest = new(
                type: _deviceType,
                remoteUri: Uri.AbsoluteUri,
                callbackUrl: null,
                alias: null,
                persist: false,
                authentication: authentication,
                cacheTimeout: ValueCacheSeconds,
                // CRITICAL: with true the connection is verified successfully, but the device is not mounted
                // into the element tree (empty "remote" node)
                checkConnection: false);

            _deviceProfileBuilder.AddDevice(Core.Root, addRequest, acquireLock: true);

            // AddDeviceResponseServiceData.Address does not reliably report the mounted element's address
            // (empirically confirmed empty even on success with checkConnection: false) - look the element up
            // directly instead. Exactly one device is ever mirrored per IoLinkMaster instance, so it must be
            // the sole child under "remote" - Single() (rather than First()) turns a violation of that
            // invariant into a clear exception instead of silently operating on the wrong element.
            IBaseElement remoteContainer = Core.Root.Subs!.Single(x => x.Identifier == "remote");
            IBaseElement deviceElement = remoteContainer.Subs!.Single();
            _mirroredDeviceAddress = deviceElement.Address;
            _portDeviceInfos = CreatePortInfos(deviceElement);
            Security = QuerySecurity(deviceElement);

            LogMirroringCompleted();
            Initialized = true;
            _announceTreeAfterMirror = true;

            _pollingTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(PollingWaitTime));
            _pollingTask = PollingTaskAsync(_pollingTimer);
        }
        catch (Exception ex)
        {
            if (_mirroringErrorLogged == false)
            {
                _mirroringErrorLogged = true;
                LogMirroringError(ex);
            }
            await DelayAndIgnoreCancellation(ErrorWaitTime);
        }
    }

    /// <summary>
    /// Invokes <paramref name="deviceElement"/>'s "GetIdentity" service and extracts its <c>security</c> block, if any.
    /// <para>"GetIdentity" is answerable without authentication even on a master that otherwise rejects every
    /// other service call unauthenticated - so this works regardless of whether <see cref="Username"/>/
    /// <see cref="Password"/> were configured, or configured correctly.</para>
    /// </summary>
    /// <param name="deviceElement">The freshly mirrored device element.</param>
    /// <returns>The master's security info - every property keeps its default if no <c>security</c> block was present or the call failed.</returns>
    private DeviceTreeIoLinkMasterSecurity QuerySecurity(IBaseElement deviceElement)
    {
        if (deviceElement.GetElementByPredicate(e => e.Identifier == "GetIdentity", false, false) is not IServiceElement getIdentityElement)
            return new DeviceTreeIoLinkMasterSecurity();

        try
        {
            Variant response = getIdentityElement.Invoke(null!, new ServiceInfo());
            if (response.AsVariantObject().TryGetValue("security", out Variant securityVariant))
            {
                VariantObject security = securityVariant.AsVariantObject();
                return new DeviceTreeIoLinkMasterSecurity
                {
                    RequiresAuthentication = security.TryGetValue("passwordstatus", out Variant passwordStatus) &&
                        string.Equals(passwordStatus.ToString(), "required", StringComparison.OrdinalIgnoreCase),
                    AuthenticationScheme = security.TryGetValue("authscheme", out Variant authScheme) ? authScheme.ToString() : null,
                    SecurityMode = security.TryGetValue("securitymode", out Variant securityMode) ? securityMode.ToString() : null,
                };
            }
        }
        catch (Exception ex)
        {
            LogSecurityQueryFailed(ex);
        }

        return new DeviceTreeIoLinkMasterSecurity();
    }

    /// <summary>
    /// Runs <see cref="PollingAsync"/> on every tick of <paramref name="timer"/> until it is disposed.
    /// </summary>
    private async Task PollingTaskAsync(PeriodicTimer timer)
    {
        LogPollingStarted();
        while (await timer.WaitForNextTickAsync(CancellationToken.None))
            await PollingAsync();
        LogPollingStopped();
    }

    /// <summary>
    /// One polling cycle: probes every port's connectivity and identity, reports availability changes, then
    /// reads and dispatches process data for ports with a matching registration.
    /// <para>Runs under the exclusive request gate (see <see cref="AcquireExclusiveAccessAsync"/>) so a caller
    /// holding it can pause polling for the duration of its own request.</para>
    /// </summary>
    private async Task PollingAsync()
    {
        if (Initialized == false)
            return;

        await _requestGate.WaitAsync();
        try
        {
            InitializeRegistrations();
            await Task.Delay(50);

            // When reading in batches, the single getdatamulti request is both this cycle's connectivity check
            // and the source every port read below is served from - if it fails the master is unreachable and
            // there is nothing to probe this cycle.
            if (_useGetDataMulti && PrefetchPortData() is false)
                return;

            try
            {
                ProbePorts();
                ReportPendingChildDevicesChanged();

                // A device can be genuinely present (correct vendor/device id) but not yet answer process data
                // queries right after (re-)connecting, e.g. while IO-Link negotiation is still in progress -
                // reading it only once the master itself is confirmed reachable means neither a slow/failing
                // read for one device nor its own try/catch can undo the availability just established above.
                // The master's own diagnostics are dispatched here inside the prefetch window too: in
                // getdatamulti mode they were folded into that batch (see PrefetchPortData) and so serve from it;
                // without the batch (_prefetchedValues is null) they fall through to live reads as before.
                if (Timeout is false)
                {
                    ReadAndDispatchProcessData();
                    ReadAndDispatchMasterDiagnostics();
                }
            }
            finally
            {
                _prefetchedValues = null;
            }
        }
        finally
        {
            _requestGate.Release();
        }
    }

    /// <summary>
    /// Probes every port's connectivity (mode) and, for ports in IO-Link mode, its connected device's identity
    /// (vendor/device id), updating each <see cref="IoLinkMasterPortDeviceInfo"/> and flagging
    /// <see cref="_pendingChildDevicesChanged"/> for any port whose availability or identity changed.
    /// <para>Stops probing further ports once <see cref="Timeout"/> becomes set: once a port's processing
    /// leaves it set, the master itself is unreachable (or still is), and letting every remaining port
    /// independently wait out its own HttpClient timeout too could make a single cycle take up to
    /// (port count) x HttpClient.Timeout to finish - which, since it holds the exclusive request gate the
    /// whole time, would delay both offline detection and the next cycle's recovery check by that same
    /// amount.</para>
    /// </summary>
    private void ProbePorts()
    {
        bool stopProbingRemainingPorts = false;
        foreach (IoLinkMasterPortDeviceInfo portDeviceInfo in _portDeviceInfos!.Values)
        {
            if (stopProbingRemainingPorts)
                break;

            try
            {
                int? portMode = GetDataValueAsValue(portDeviceInfo.PortModeAddress, false, out _, isConnectivityProbe: true)?.GetUInt8();

                // Reset Timeout as soon as the connectivity probe succeeds, before reading this same
                // port's vendor/device id below - otherwise those reads (skipIfInTimeout: true) would
                // still see Timeout as true and get skipped, leaving this specific port's data stale
                // for one extra polling cycle compared to every other port processed this same cycle.
                if (Timeout)
                {
                    Timeout = false;
                    TimeoutChanged?.Invoke();
                    LogConnectionReEstablished();
                }

                ushort? vendorId = null;
                uint? deviceId = null;
                if (portMode == 3) // 3 = IO-Link
                {
                    // A port can be configured for IO-Link with nothing plugged into it, and the master answers
                    // the identity reads below with an error in that case - once per port per polling cycle.
                    // The status element answers either way, so it tells the two apart for the price of one read
                    // and saves two on an empty port. Only a status of "no device" skips the identity reads: any
                    // other status means a device is there, even if it is not in operate yet.
                    int? deviceStatus = GetDataValueAsValue(portDeviceInfo.DeviceStatusAddress, true, out _)?.GetUInt8();
                    if (deviceStatus != IoLinkMasterPortDeviceInfo.NoDeviceStatus)
                    {
                        vendorId = GetDataValueAsValue(portDeviceInfo.VendorIdAddress, true, out _)?.GetUInt16();
                        deviceId = GetDataValueAsValue(portDeviceInfo.DeviceIdAddress, true, out _)?.GetUInt32();
                    }
                }

                bool identityChanged = portDeviceInfo.Available.HasValue &&
                    (portDeviceInfo.VendorId != vendorId || portDeviceInfo.DeviceId != deviceId);
                _pendingChildDevicesChanged |= identityChanged;
                // A device that goes away and the exact same device coming back are both identity
                // changes here - either way, ParseDeviceAsync must not reuse a cached tree node for
                // this port on the next full parse, since something about it actually changed.
                portDeviceInfo.NeedsFullRefresh |= identityChanged;

                portDeviceInfo.Available = vendorId.HasValue && deviceId.HasValue;
                portDeviceInfo.VendorId = vendorId;
                portDeviceInfo.DeviceId = deviceId;
            }
            catch (Exception ex)
            {
                // Detecting the transition to unavailable must not depend on the current Timeout state:
                // a device that disappears while the master happens to be in a Timeout window would
                // otherwise never be detected (Available silently flips to false here, but by the time
                // Timeout clears there is no further change left to compare against). The detected
                // change is only actually reported once Timeout is false (see
                // ReportPendingChildDevicesChanged), so this does not cause a full re-parse to be
                // triggered while the whole master is still unreachable.
                bool wasAvailable = portDeviceInfo.Available.HasValue && portDeviceInfo.Available.Value;
                _pendingChildDevicesChanged |= wasAvailable;
                portDeviceInfo.NeedsFullRefresh |= wasAvailable;

                portDeviceInfo.VendorId = null;
                portDeviceInfo.DeviceId = null;
                portDeviceInfo.Available = false;

                LogPollingError(ex);
            }
            finally
            {
                stopProbingRemainingPorts = Timeout;
            }
        }
    }

    /// <summary>
    /// Raises <see cref="ChildDevicesChanged"/> if <see cref="ProbePorts"/> flagged a change - but only once
    /// the master itself is confirmed reachable.
    /// <para>While <see cref="Timeout"/> is true, every port fails at once (not because those devices are
    /// individually gone), and a full re-parse would just be a very slow, doomed attempt to reach an
    /// unreachable master. Reporting this before process data is read means a newly (re-)available device is
    /// announced immediately, instead of only after every port's process data has also been read - a slow or
    /// still-negotiating device's process data read must not delay this notification for everyone.</para>
    /// </summary>
    private void ReportPendingChildDevicesChanged()
    {
        // _announceTreeAfterMirror forces one announcement on the first reachable cycle after a mirror, so a master
        // that only became reachable after start publishes its now-online tree even though no port flagged a change.
        if ((_pendingChildDevicesChanged || _announceTreeAfterMirror) && Timeout is false)
        {
            _pendingChildDevicesChanged = false;
            _announceTreeAfterMirror = false;
            // Not needed to clear the condition cache here: it is keyed by vendor/device id in
            // addition to the port address, so a device change naturally produces a cache miss.
            ChildDevicesChanged?.Invoke();
        }
    }

    /// <summary>
    /// Reads and decodes process-data-in for every port with a matching, currently-available registration, and
    /// dispatches the decoded values to whichever FunctionBlocks registered for each sub-index.
    /// </summary>
    private void ReadAndDispatchProcessData()
    {
        foreach (IoLinkMasterPortDeviceInfo portDeviceInfo in _portDeviceInfos!.Values)
        {
            if (portDeviceInfo.Available is not true ||
                _portProcessDataRegistrations.TryGetValue(portDeviceInfo.PortIndex, out DeviceProcessDataRegistration? registration) is false ||
                registration.DeviceDefinition is null ||
                registration.DeviceId != portDeviceInfo.DeviceId ||
                registration.VendorId != portDeviceInfo.VendorId)
                continue;

            try
            {
                List<DisplayValue> decodedPdin = ReadAndDecodeProcessDataIn(portDeviceInfo.Address, registration.DeviceDefinition, registration.VendorId, registration.DeviceId);
                foreach (DisplayValue entry in decodedPdin)
                {
                    if (entry.SubIndex.HasValue is false)
                        continue;

                    if (registration.SubIndexRegistrations.TryGetValue(entry.SubIndex.Value, out ConcurrentDictionary<IIoLinkSubscriberFunctionBlock, byte>? blocks) is false)
                        continue;

                    foreach (KeyValuePair<IIoLinkSubscriberFunctionBlock, byte> block in blocks)
                    {
                        block.Key.SetValue(entry.Value, entry.DisplayFormat);
                        block.Key.SetUnit(entry.Unit);
                    }
                }
            }
            catch (Exception ex)
            {
                LogPollingError(ex);
            }
        }
    }

    /// <summary>
    /// Reads a device's raw process-data-in value and decodes it into its individual named values using the
    /// device's IODD definition.
    /// </summary>
    /// <param name="deviceAddress">The connected device's element address.</param>
    /// <param name="device">The device's IODD definition, used to decode the raw value.</param>
    /// <param name="vendorId">The connected device's vendor ID, used to evaluate IODD conditions.</param>
    /// <param name="deviceId">The connected device's device ID, used to evaluate IODD conditions.</param>
    /// <param name="skipIfInTimeout">Whether to skip the read while the master is already in a timeout.</param>
    /// <returns>The decoded process-data-in values, or an empty list if skipped or empty.</returns>
    internal List<DisplayValue> ReadAndDecodeProcessDataIn(string deviceAddress, IDevice device, ushort vendorId, uint deviceId, bool skipIfInTimeout = true)
    {
        string pdinAddress = $"{deviceAddress}{PdInAddressSuffix}";
        string? pdinValue = GetDataValueAsValue(pdinAddress, skipIfInTimeout, out bool skipped)?.GetString();
        if (skipped || string.IsNullOrEmpty(pdinValue))
            return [];

        return ProcessData.Decode(ProcessData.Direction.Input,
            Convert.FromHexString(pdinValue),
            device,
            null,
            condition => CheckCondition(condition, deviceAddress, vendorId, deviceId));
    }

    /// <summary>
    /// Registers a FunctionBlock to receive decoded process-data-in values for one port's ISDU index.
    /// </summary>
    /// <param name="portIndex">The port the expected device is connected to.</param>
    /// <param name="vendorId">The expected connected device's vendor ID.</param>
    /// <param name="deviceId">The expected connected device's device ID.</param>
    /// <param name="processDataInIndex">The ISDU index of the process data to subscribe to.</param>
    /// <param name="functionBlock">The FunctionBlock to dispatch decoded values to.</param>
    public void RegisterProcessDataIn(int portIndex, ushort vendorId, uint deviceId, int processDataInIndex, IIoLinkSubscriberFunctionBlock functionBlock)
    {
        DeviceProcessDataRegistration registration = _portProcessDataRegistrations.GetOrAdd(portIndex, _ => new DeviceProcessDataRegistration()
        {
            DeviceId = deviceId,
            VendorId = vendorId,
        });
        ConcurrentDictionary<IIoLinkSubscriberFunctionBlock, byte> blocks = registration.SubIndexRegistrations.GetOrAdd(processDataInIndex, _ => new());
        blocks.TryAdd(functionBlock, default);
    }

    /// <summary>
    /// Removes every process-data-in registration for the given FunctionBlock across all ports.
    /// </summary>
    /// <param name="functionBlock">The FunctionBlock to unregister.</param>
    public void UnregisterProcessDataIn(IIoLinkSubscriberFunctionBlock functionBlock)
    {
        var portRegistrationsToRemove = new List<int>();
        foreach (KeyValuePair<int, DeviceProcessDataRegistration> registration in _portProcessDataRegistrations)
            foreach (KeyValuePair<int, ConcurrentDictionary<IIoLinkSubscriberFunctionBlock, byte>> blocks in registration.Value.SubIndexRegistrations)
                if (blocks.Value.TryRemove(functionBlock, out _) && blocks.Value.IsEmpty)
                    portRegistrationsToRemove.Add(registration.Key);

        foreach (int port in portRegistrationsToRemove)
            _portProcessDataRegistrations.TryRemove(port, out _);
    }

    /// <summary>
    /// Registers a FunctionBlock to receive the master's own diagnostic values every polling cycle.
    /// </summary>
    /// <param name="functionBlock">The FunctionBlock to dispatch the read diagnostics to.</param>
    public void RegisterMasterDiagnostics(IIoLinkMasterDiagnosticSubscriber functionBlock)
        => _masterDiagnosticSubscribers.TryAdd(functionBlock, default);

    /// <summary>
    /// Removes a FunctionBlock's master-diagnostics registration.
    /// </summary>
    /// <param name="functionBlock">The FunctionBlock to unregister.</param>
    public void UnregisterMasterDiagnostics(IIoLinkMasterDiagnosticSubscriber functionBlock)
        => _masterDiagnosticSubscribers.TryRemove(functionBlock, out _);

    /// <summary>
    /// Reads the master's own diagnostic values (temperature, supply voltage, current and supervision status)
    /// and dispatches them to every registered <see cref="IIoLinkMasterDiagnosticSubscriber"/>.
    /// </summary>
    private void ReadAndDispatchMasterDiagnostics()
    {
        if (_masterDiagnosticSubscribers.IsEmpty || _mirroredDeviceAddress is null)
            return;

        try
        {
            _masterDiagnosticUnits ??= (
                ReadMasterString($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterTemperature}{IoLinkParser.AddressUnitSuffix}"),
                ReadMasterString($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterVoltage}{IoLinkParser.AddressUnitSuffix}"),
                ReadMasterString($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterCurrent}{IoLinkParser.AddressUnitSuffix}"));

            var diagnostics = new MasterDiagnostics(
                ReadMasterNumber($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterTemperature}"), _masterDiagnosticUnits.Value.Temperature,
                ReadMasterNumber($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterVoltage}"), _masterDiagnosticUnits.Value.Voltage,
                ReadMasterNumber($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterCurrent}"), _masterDiagnosticUnits.Value.Current,
                (long?)ReadMasterNumber($"{_mirroredDeviceAddress}{IoLinkParser.AddressMasterSupervisionStatus}"));

            foreach (IIoLinkMasterDiagnosticSubscriber subscriber in _masterDiagnosticSubscribers.Keys)
                subscriber.SetDiagnostics(diagnostics);
        }
        catch (Exception ex)
        {
            LogPollingError(ex);
        }
    }

    /// <summary>
    /// Reads a single master diagnostic value as a number, returning <see langword="null"/> if the master does
    /// not expose it or the read is skipped/fails.
    /// </summary>
    private double? ReadMasterNumber(string address)
    {
        try
        {
            VariantValue? value = GetDataValueAsValue(address, skipIfInTimeout: true, out bool skipped);
            return skipped || value is null ? null : value.GetInt32();
        }
        catch (IoTCoreException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads a single master diagnostic value's unit, returning an empty string if it has none or the read fails.
    /// </summary>
    private string ReadMasterString(string address)
    {
        try
        {
            VariantValue? value = GetDataValueAsValue(address, skipIfInTimeout: true, out bool skipped);
            return skipped || value is null ? string.Empty : value.GetString();
        }
        catch (IoTCoreException)
        {
            return string.Empty;
        }
    }
}
