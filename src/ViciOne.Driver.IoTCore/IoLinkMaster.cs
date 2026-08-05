using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ACX.ViciOne.Data.Driver.CloudDataAdapter.Contracts;
using ifm.IoddParser.Contracts;
using ifm.IoddParser.Helpers;
using ifmIoTCore;
using ifmIoTCore.Common.Variant;
using ifmIoTCore.Elements;
using ifmIoTCore.Exceptions;
using ifmIoTCore.IoddExtender.Utilities;
using ifmIoTCore.NetAdapter.Http;
using ifmIoTCore.Profiles.Base;
using ifmIoTCore.Profiles.DeviceManagement;
using ifmIoTCore.Profiles.DeviceManagement.ServiceData.Requests;
using Microsoft.Extensions.Logging;
using ViciOne.Driver.IoTCore.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Extensions;
using ViciOne.Driver.IoTCore.IoddManagement;

namespace ViciOne.Driver.IoTCore;

public sealed class IoLinkMaster : IManageableDevice
{
    public static string DefaultDeviceName { get; } = "IO-Link Device";
    public static string DefaultName { get; } = "IO-Link Master";
    public static int DefaultPort { get; } = 80;
    private const int ErrorWaitTime = 5000;
    private const int PollingWaitTime = 1000;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
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
    // Detected but not-yet-reported change in child device availability, see PollingAsync.
    private bool _pendingChildDevicesChanged;
    private Task? _pollingTask;
    private PeriodicTimer? _pollingTimer;
    private Dictionary<int, IoLinkMasterPortDeviceInfo>? _portDeviceInfos;
    private readonly Lazy<RecordingManager> _recordingManager;

    public IIoTCore Core { get; }
    internal ConditionCache ConditionValueCache { get; } = new();
    // Last successfully parsed device tree node per port, keyed by port index. Reused by
    // ParseDeviceAsync for ports whose IoLinkMasterPortDeviceInfo.NeedsFullRefresh is false, instead
    // of re-reading that device's IODD data and product info again on every single full parse.
    internal Dictionary<int, DeviceTreeDevice> CachedPortDeviceNodes { get; } = [];
    bool IManageableDevice.IgnoreReleaseDelay => false;
    public bool Initialized { get; private set; }
    DateTime? IManageableDevice.ReleaseDate { get; set; }
    public string Address { get; }
    public bool Timeout { get; private set; }
    public Uri Uri { get; }

    public event Action<Measurement>? BlobDataReceived;
    public event Action? ChildDevicesChanged;
    public event Action? TimeoutChanged;

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

    public IoLinkMaster(string address, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException($"{nameof(Address)} cannot be null or empty.", nameof(address));

        Address = address;
        Uri = new UriBuilder(address).Uri;
        _logger = logger;

        string iotCoreName = $"IoTCore-{Uri.DnsSafeHost}-{Uri.Port}";
        Core = IoTCoreFactory.Create(iotCoreName);

        _httpClientNetAdapterFactory = new HttpClientNetAdapterFactory(
            new ifmIoTCore.MessageConverter.Json.Microsoft.MessageConverter(),
            TimeSpan.FromSeconds(10), true);

        Core.RegisterClientNetAdapterFactory(_httpClientNetAdapterFactory);

        ProfileBuilderConfiguration configuration = new() { Address = iotCoreName, IoTCore = Core };
        _deviceProfileBuilder = new DeviceManagementProfileBuilder(configuration);
        _deviceProfileBuilder.Build();

        _mirroringTask = Task.Run(MirroringTaskAsync);

        _recordingManager = new Lazy<RecordingManager>(CreateRecordingManager);
    }

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

    private static Dictionary<int, IoLinkMasterPortDeviceInfo> CreatePortInfos(DeviceElement deviceElement)
        => IoddParserManager.GetIoLinkMasterDevices(deviceElement).Select(
            portDeviceElement =>
            {
                int portIndex = IoddParserManager.GetPortIndex(portDeviceElement.Parent);
                return new IoLinkMasterPortDeviceInfo
                {
                    Address = portDeviceElement.Address,
                    DeviceIdAddress = $"{portDeviceElement.Address}{IoddConstants.DeviceIdDataElementInIoLinkDevice}",
                    PortModeAddress = $"{portDeviceElement.Parent.Address}/mode",
                    PortIndex = portIndex,
                    VendorIdAddress = $"{portDeviceElement.Address}{IoddConstants.VendorIdDataElementInIoLinkDevice}",
                };
            }).ToDictionary(x => x.PortIndex, x => x);

    private RecordingManager CreateRecordingManager()
    {
        RecordingManager manager = new(Address, _logger);
        manager.BlobDataReceived += BlobDataReceived;
        return manager;
    }

    private async Task DelayAndIgnoreCancellation(int millisecondsDelay)
    {
        try { await Task.Delay(millisecondsDelay, _cancellationTokenSource.Token); }
        catch (OperationCanceledException) { }
    }

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
            _deviceProfileBuilder.Unmirror(new UnmirrorRequestServiceData(Uri.AbsoluteUri, null));
            Initialized = false;
        }

        ClearConditionValueCache();
        _httpClientNetAdapterFactory.Dispose();
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

    internal void ClearConditionValueCache()
        => ConditionValueCache.Clear();

    internal IEnumerable<IoLinkMasterPortDeviceInfo> PortDeviceInfos
        => _portDeviceInfos?.Values ?? Enumerable.Empty<IoLinkMasterPortDeviceInfo>();

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

    private void InitializeRegistrations()
    {
        foreach (KeyValuePair<int, DeviceProcessDataRegistration> registration in _portProcessDataRegistrations)
        {
            if (registration.Value.DeviceDefinition is not null)
                continue;

            IDevice? device = null;
            try { device = IoddParserManager.GetIoddDevice(registration.Value.VendorId, registration.Value.DeviceId); }
            catch (Exception ex) { _logger.ErrorWhileLoadingIodd(registration.Value.VendorId, registration.Value.DeviceId, ex.Message, ex); }
            if (device is null)
            {
                _logger.UnknownDeviceFound(registration.Value.VendorId, registration.Value.DeviceId, Uri);
                continue;
            }

            registration.Value.DeviceDefinition = device;
        }
    }

    /// <param name="isConnectivityProbe">
    /// Only the periodic port mode read in <see cref="PollingAsync"/> is a dedicated master connectivity check
    /// and is allowed to set this parameter to <see langword="true"/>. A single device's data (vendor/device id, process
    /// data, IODD fields, ...) can fail to be read for reasons specific to that one device, so those reads must not
    /// affect the master-wide <see cref="Timeout"/> flag - doing so would mark the whole master offline whenever a
    /// single sensor is unreachable.
    /// </param>
    internal VariantValue? GetDataValueAsValue(string elementAddress, bool skipIfInTimeout, out bool skippedBecauseOfTimeout, bool isConnectivityProbe = false)
    {
        IoTCoreException? timeoutException;
        bool timeoutBefore = Timeout;

        // Wenn bereits ein Timeout vorliegt, keine weitere Anfrage starten (es sei denn es soll explizit angefragt werden, ob das Timeout vorbei ist: skipIfInTimeout == false).
        skippedBecauseOfTimeout = skipIfInTimeout && Timeout;
        if (skippedBecauseOfTimeout)
            return null;

        // HACK: Wenn parallele Anfragen an das Gerät gestellt werden und beide ins Timeout laufen, dann kommen unterschiedliche Exceptions zurück.
        //       Um das zu verhindern, sollen alle GetDataValueAsValue-Aufrufe seriell ausgeführt werden.
        lock (_getDataLock)
        {
            // Wird hier im Lock erneut geprüft, damit keine weitere Anfrage gestartet wird und erneut ins Timeout läuft, wenn die aktuelle Anfrage schon ein Timeout feststellt.
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

    public void GetRecordingBlobData(string identifier, int portIndex, bool ignoreTimedValueArray)
        => _recordingManager.Value.GetRecordingBlobData(identifier, portIndex, ignoreTimedValueArray);

    private void LogConnectionReEstablished()
    {
        if (string.IsNullOrWhiteSpace(_lastLoggedMessage))
            return;
        _lastLoggedMessage = string.Empty;
        _logger.ConnectionReEstablished(Uri);
    }

    private void LogPollingError(Exception ex)
    {
        string error = ex.Message;
        if (string.Equals(_lastLoggedMessage, error, StringComparison.OrdinalIgnoreCase))
            return;
        _lastLoggedMessage = error;
        _logger.PollingErrorOccurred(ex, Uri);
    }

    private async Task MirroringTaskAsync()
    {
        while (Initialized == false && _cancellationTokenSource.IsCancellationRequested == false)
            await MirroringAsync();
    }

    private async Task MirroringAsync()
    {
        try
        {
            var deviceElement = _deviceProfileBuilder.Mirror(new MirrorRequestServiceData(Uri.AbsoluteUri, null)) as DeviceElement;
            _portDeviceInfos = CreatePortInfos(deviceElement!);
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

    private async Task PollingTaskAsync(PeriodicTimer timer)
    {
        _logger.PollingStarted(Uri);
        while (await timer.WaitForNextTickAsync(CancellationToken.None))
            await PollingAsync();
        _logger.PollingStopped(Uri);
    }

    private async Task PollingAsync()
    {
        if (Initialized == false)
            return;

        await _requestGate.WaitAsync();
        try
        {
            InitializeRegistrations();

            await Task.Delay(50);

            // Once a port's processing leaves Timeout set, the master itself is unreachable
            // (or still is) - stop probing further ports this cycle instead of letting each of
            // them independently wait out its own HttpClient timeout too. Otherwise a single
            // PollingAsync cycle could take up to (port count) x HttpClient.Timeout to finish,
            // which - since it holds the exclusive request gate the whole time - would delay
            // both offline detection and the next cycle's recovery check by that same amount.
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
                        vendorId = GetDataValueAsValue(portDeviceInfo.VendorIdAddress, true, out _)?.GetUInt16();
                        deviceId = GetDataValueAsValue(portDeviceInfo.DeviceIdAddress, true, out _)?.GetUInt32();
                    }

                    // TODO: Eigentlich müsste man hier auch noch den Status des Sensors (2 = operate) prüfen, das würde aber mehr Zeit kosten.

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
                    // change is only actually reported once Timeout is false (see below), so this does not
                    // cause a full re-parse to be triggered while the whole master is still unreachable.
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

            // Only report pending changes once the master itself is confirmed reachable: while Timeout is
            // true, every port fails at once (not because those devices are individually gone), and a full
            // re-parse would just be a very slow, doomed attempt to reach an unreachable master. Reporting
            // this now, before reading process data below, means a newly (re-)available device is announced
            // immediately instead of only after every port's process data has also been read - a slow or
            // still-negotiating device's process data read must not delay this notification for everyone.
            if (_pendingChildDevicesChanged && Timeout is false)
            {
                _pendingChildDevicesChanged = false;
                // Not needed to clear the condition cache here: it is keyed by vendor/device id in
                // addition to the port address, so a device change naturally produces a cache miss.
                ChildDevicesChanged?.Invoke();
            }

            // Process data is read and decoded in its own pass, after availability has already been
            // determined and reported for every port above: a device can be genuinely present (correct
            // vendor/device id) but not yet answer process data queries right after (re-)connecting, e.g.
            // while IO-Link negotiation is still in progress. Reading it here, separately, means neither a
            // slow/failing read for one device nor its own try/catch can undo the availability that was
            // just established, or delay reporting it for every other port.
            if (Timeout is false)
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
        }
        finally
        {
            _requestGate.Release();
        }
    }

    internal List<DisplayValue> ReadAndDecodeProcessDataIn(string deviceAddress, IDevice device, ushort vendorId, uint deviceId, bool skipIfInTimeout = true)
    {
        string pdinAddress = $"{deviceAddress}{IoddConstants.PdInDataElementInIoLinkDevice}";
        string? pdinValue = GetDataValueAsValue(pdinAddress, skipIfInTimeout, out bool skipped)?.GetString();
        if (skipped || string.IsNullOrEmpty(pdinValue))
            return [];

        return Helpers.DecodeProcessData(Helpers.ProcessDataDirection.Input,
            Convert.FromHexString(pdinValue),
            device,
            null,
            condition => CheckCondition(condition, deviceAddress, vendorId, deviceId));
    }

    public DeviceProcessDataRegistration RegisterProcessDataIn(int portIndex, ushort vendorId, uint deviceId, int processDataInIndex, IIoLinkSubscriberFunctionBlock functionBlock)
    {
        DeviceProcessDataRegistration registration = _portProcessDataRegistrations.GetOrAdd(portIndex, _ => new DeviceProcessDataRegistration()
        {
            DeviceId = deviceId,
            VendorId = vendorId,
        });
        ConcurrentDictionary<IIoLinkSubscriberFunctionBlock, byte> blocks = registration.SubIndexRegistrations.GetOrAdd(processDataInIndex, _ => new());
        blocks.TryAdd(functionBlock, default);

        return registration;
    }

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
