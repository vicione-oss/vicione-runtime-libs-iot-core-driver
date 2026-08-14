using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ifm.Common.Variant;
using ifm.DataStore;
using ifm.IoddParser.Contracts;
using ifm.IoddParser.Helpers;
using ifm.IoTCore;
using ifm.IoTCore.Common;
using ifm.IoTCore.Common.Exceptions;
using ifm.IoTCore.Contracts;
using ifm.IoTCore.Device.IoTCore;
using ifm.IoTCore.ElementManager.Contracts.Elements;
using ifm.IoTCore.MessageConverter.Json;
using ifm.IoTCore.NetAdapter.Http.Client;
using ifm.IoTCore.Profile.DeviceManagement;
using ifm.IoTCore.Profile.DeviceManagement.ServiceData.Requests;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.IoTCore;
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
public sealed class IoLinkMaster : IDisposable
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
    // Not exposed as constants by the new SDK's own IoddParser packages (which only cover generic IODD
    // decoding, not IO-Link master wire addresses) - these three address suffixes are unchanged from the old
    // SDK's IoddConstants, confirmed against the live-tested mirrored device tree (see migration plan).
    private const string DeviceIdAddressSuffix = "/deviceid";
    private const string DeviceStatusAddressSuffix = "/status";
    private const string VendorIdAddressSuffix = "/vendorid";

    /// <summary>
    /// The cache lifetime, in seconds, the SDK applies to every value read from a mirrored device. Zero keeps
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
    private readonly HttpClientNetAdapterFactory _httpClientNetAdapterFactory;
    private string _lastLoggedMessage = string.Empty;
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<int, DeviceProcessDataRegistration> _portProcessDataRegistrations = [];
    private bool _mirroringErrorLogged;
    private readonly Task _mirroringTask;
    // The "type" string the new SDK's DeviceManagementProfileBuilder expects in AddDeviceRequestServiceData -
    // read off the registered IoTCoreDeviceFactory rather than hardcoded, since it's not otherwise documented.
    private readonly string _deviceType;
    // Set once mirroring succeeds (see MirroringAsync) - the actual mounted element's address, needed for
    // RemoveDevice in Dispose since the new SDK's AddDevice does not reliably report it back (see MirroringAsync).
    private string? _mirroredDeviceAddress;
    // Detected but not-yet-reported change in child device availability, see PollingAsync.
    private bool _pendingChildDevicesChanged;
    private Task? _pollingTask;
    private PeriodicTimer? _pollingTimer;
    private Dictionary<int, IoLinkMasterPortDeviceInfo>? _portDeviceInfos;
    private readonly Lazy<RecordingManager> _recordingManager;

    /// <summary>
    /// The underlying ifm.IoTCore SDK connection.
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
    public IoLinkMaster(string address, ILogger logger, string? username = null, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException($"{nameof(Address)} cannot be null or empty.", nameof(address));

        Address = address;
        Uri = new UriBuilder(address).Uri;
        _logger = logger;
        Username = username;
        Password = password;

        ifm.Logger.Contracts.ILogger ifmLogger = new MicrosoftLoggerAdapter(logger);
        ConnectionComponents connection = BuildConnection(Uri, ifmLogger);
        Core = connection.Core;
        _httpClientNetAdapterFactory = connection.HttpClientNetAdapterFactory;
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
    /// The new SDK types that make up one master connection, bundled together so <see cref="BuildConnection"/>
    /// can be a single, self-contained step in the constructor instead of a dozen loose local variables.
    /// </summary>
    private readonly record struct ConnectionComponents(
        IIoTCore Core,
        HttpClientNetAdapterFactory HttpClientNetAdapterFactory,
        ifm.IoTCore.Device.Contracts.DeviceManager DeviceManager,
        string DeviceType,
        DeviceManagementProfileBuilder ProfileBuilder);

    /// <summary>
    /// Wires up a new, unmirrored <see cref="IIoTCore"/> connection: data store, HTTP net adapter, device
    /// factory registration, and device-management profile builder - everything needed before
    /// <see cref="MirroringAsync"/> can call <c>AddDevice</c>.
    /// </summary>
    /// <param name="uri">The master's address, used to derive the connection's data store file and SDK identifier.</param>
    /// <param name="ifmLogger">The new SDK's own logger type (see <see cref="MicrosoftLoggerAdapter"/>), passed to every SDK component that needs one.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Justification = "Ownership of the created disposables transfers to the returned ConnectionComponents; the constructor stores them in fields and disposes them in Dispose().")]
    private static ConnectionComponents BuildConnection(Uri uri, ifm.Logger.Contracts.ILogger ifmLogger)
    {
        string iotCoreName = $"IoTCore-{uri.DnsSafeHost}-{uri.Port}";
        // The new SDK's element identifiers reject characters like the dots in an IP-address host (throws
        // ArgumentException from BaseElement's ctor) - sanitize before use as the IoTCore's own identifier.
        // The unsanitized name is still used below for the (unrelated) data store file name.
        string sanitizedIotCoreName = AddressParser.ReplaceInvalidCharacters(iotCoreName, "_");

        string dataStorePath = Path.Combine(Path.GetTempPath(), $"{iotCoreName}-datastore.json");
        DataStore dataStore = new(dataStorePath);

        IIoTCore core = IoTCoreFactory.Create(sanitizedIotCoreName, dataStore, ifmLogger);

        HttpClientNetAdapterFactory httpClientNetAdapterFactory = new(
            new MessageConverter(), TimeSpan.FromSeconds(10), true, ifmLogger);
        core.ClientNetAdapterManager.RegisterClientNetAdapterFactory(httpClientNetAdapterFactory);

        ifm.IoTCore.Device.Contracts.DeviceManager deviceManager = new();
        // Without a registered IoTCoreDeviceFactory, AddDevice below fails with "device type not supported" -
        // this is the new SDK's replacement for what the old SDK handled internally.
        // The TimeSpan is this factory's fallback value-cache lifetime, used only for devices added without one
        // of their own. MirroringAsync always passes ValueCacheSeconds explicitly, which overrides it.
        IoTCoreDeviceFactory ioTCoreDeviceFactory = new(
            core.ElementManager, core.PersistenceManager, core.EventSender, ifmLogger,
            core.ClientNetAdapterManager, TimeSpan.FromSeconds(30), new Config());
        deviceManager.RegisterDeviceFactory(ioTCoreDeviceFactory);

        ifm.IoTCore.Profile.DeviceManagement.Config.DeviceManagement settings = new(
            // The 3-parameter DeviceMonitor ctor (heartbeat, treeChanged, interval) throws
            // "Invalid value '0' for parameter 'interval'" internally - use the parameterless ctor instead.
            new ifm.IoTCore.Profile.DeviceManagement.Config.DeviceMonitor(), []);

        DeviceManagementProfileBuilder profileBuilder = new(
            core.ElementManager, core.ClientNetAdapterManager, core.DataStore, deviceManager, ifmLogger, settings);
        profileBuilder.Build();

        return new ConnectionComponents(core, httpClientNetAdapterFactory, deviceManager, ioTCoreDeviceFactory.Type, profileBuilder);
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
                _logger.LogWarning(ex, "Acyclic read failed for condition variable index {Index} subindex {SubIndex} on '{DeviceAddress}'. Using default value from IODD.",
                    condition.Variable.Index, condition.SubIndex, deviceAddress);
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
    /// Stops the mirroring/polling loops, unmirrors the device tree if mirrored, and releases the underlying SDK
    /// and recording resources.
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
        _httpClientNetAdapterFactory.Dispose();
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
        //TODO: verriegeln?
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
            catch (Exception ex) { _logger.ErrorWhileLoadingIodd(registration.Value.VendorId, registration.Value.DeviceId, ex.Message, ex); }
            if (device is null)
            {
                _logger.UnknownDeviceFound(registration.Value.VendorId, registration.Value.DeviceId, Uri);
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
        IoTCoreException? timeoutException;
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
            catch (IoTCoreException ex) when (isConnectivityProbe && Timeout is false && ex.IsTimeout)
            {
                Timeout = true;
                timeoutException = ex;
            }
        }

        if (timeoutBefore != Timeout)
            TimeoutChanged?.Invoke();

        ExceptionDispatchInfo.Capture(timeoutException).Throw();
        // Compiler zufriedenstellen...
        return null;
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
    /// Logs that the connection was re-established, if a connection error was previously logged.
    /// </summary>
    private void LogConnectionReEstablished()
    {
        if (string.IsNullOrWhiteSpace(_lastLoggedMessage))
            return;
        _lastLoggedMessage = string.Empty;
        _logger.ConnectionReEstablished(Uri);
    }

    /// <summary>
    /// Logs a polling-cycle exception, suppressing repeats of the same message to avoid flooding the log while
    /// a condition persists across many polling cycles.
    /// </summary>
    private void LogPollingError(Exception ex)
    {
        string error = ex.Message;
        if (string.Equals(_lastLoggedMessage, error, StringComparison.OrdinalIgnoreCase))
            return;
        _lastLoggedMessage = error;
        _logger.PollingErrorOccurred(ex, Uri);
    }

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
            AuthenticationRequestServiceData? authentication = Username is null
                ? null
                : new AuthenticationRequestServiceData(Username, Password);

            AddDeviceRequestServiceData addRequest = new(
                type: _deviceType,
                remoteUri: Uri.AbsoluteUri,
                callbackUrl: null,
                alias: null,
                persist: false,
                authentication: authentication,
                cacheTimeout: ValueCacheSeconds,
                // KRITISCH: bei true wird die Verbindung zwar erfolgreich geprueft, aber das Geraet nicht in
                // den Elementbaum eingehaengt (leerer "remote"-Knoten)
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

            _logger.MirroringCompleted(Uri);
            Initialized = true;

            _pollingTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(PollingWaitTime));
            _pollingTask = PollingTaskAsync(_pollingTimer);
        }
        catch (Exception ex)
        {
            if (_mirroringErrorLogged == false)
            {
                _mirroringErrorLogged = true;
                _logger.MirroringErrorOccurred(ex, Uri);
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
            _logger.LogWarning(ex, "Could not query security information for '{Uri}'.", Uri);
        }

        return new DeviceTreeIoLinkMasterSecurity();
    }

    /// <summary>
    /// Runs <see cref="PollingAsync"/> on every tick of <paramref name="timer"/> until it is disposed.
    /// </summary>
    private async Task PollingTaskAsync(PeriodicTimer timer)
    {
        _logger.PollingStarted(Uri);
        while (await timer.WaitForNextTickAsync(CancellationToken.None))
            await PollingAsync();
        _logger.PollingStopped(Uri);
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

            ProbePorts();
            ReportPendingChildDevicesChanged();

            // A device can be genuinely present (correct vendor/device id) but not yet answer process data
            // queries right after (re-)connecting, e.g. while IO-Link negotiation is still in progress -
            // reading it only once the master itself is confirmed reachable means neither a slow/failing
            // read for one device nor its own try/catch can undo the availability just established above.
            if (Timeout is false)
                ReadAndDispatchProcessData();
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
        if (_pendingChildDevicesChanged && Timeout is false)
        {
            _pendingChildDevicesChanged = false;
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
}
