using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ACX.ViciOne.Data.Driver.CloudDataAdapter.Contracts;
using ifmIoTCore;
using ifmIoTCore.Common.Variant;
using ifmIoTCore.Elements;
using ifmIoTCore.MessageConverter.Json.Microsoft;
using ifmIoTCore.NetAdapter.Http;
using Microsoft.Extensions.Logging;
using ViciOne.Driver.IoTCore.Contracts;
using ViciOne.Driver.IoTCore.Extensions;
using VSEIoTCoreAdapter;

namespace ViciOne.Driver.IoTCore;

public sealed class VseDevice : IManageableDevice
{
    public static int DefaultPort { get; } = 3321;
    private const int ErrorWaitTime = 5000;
    private const int PollingWaitTime = 1000;
    private const int WritingWaitTime = 500;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed;
    private IServiceElement? _getConnectionStateService;
    private IServiceElement? _getSystemModeService;
    private readonly HttpClientNetAdapterFactory _httpClientNetAdapterFactory;
    private string _lastPollingErrorMessage = string.Empty;
    private string[]? _lastRelevantEntries;
    private string _lastWritingErrorMessage = string.Empty;
    private readonly LoggerFactory _loggerFactory;
    private readonly ForwardLoggerProvider _loggerProvider;
    private readonly ILogger _logger;
    private Task? _pollingTask;
    private PeriodicTimer? _pollingTimer;
    private readonly Lock _pollingTimerLock = new();
    private readonly ConcurrentDictionary<string, List<IVseSubscriberFunctionBlock>> _registeredAddresses = new();
    private IFM.SDK.VSE.SystemMode _systemMode;
    private readonly VSEIoTCoreAdapter.VSEIoTCore _vseCore;
    private readonly ConcurrentDictionary<string, object?> _writingAddresses = new();
    private Task? _writingTask;
    private PeriodicTimer? _writingTimer;
    private readonly Lock _writingTimerLock = new();
    private int _treeRebuildCount;

    public bool Connected => GetValueFromService(GetConnectionStateService, out string? value) && value == "connected";
    public IIoTCore Core { get; }
    public string DeviceAddress { get; }
    string IManageableDevice.Address => DeviceAddress;
    private IServiceElement GetConnectionStateService => _getConnectionStateService ??= (IServiceElement)Core.GetElementByAddress("/Device/Status/ConnectionState/getdata");
    private IServiceElement GetSystemModeService => _getSystemModeService ??= (IServiceElement)Core.GetElementByAddress("/Device/Status/SystemMode/getdata");
    bool IManageableDevice.IgnoreReleaseDelay => true;
    public bool Initialized => _treeRebuildCount >= 2;
    public bool Measuring => _systemMode == IFM.SDK.VSE.SystemMode.Measure;
    DateTime? IManageableDevice.ReleaseDate { get; set; }
    public bool Supervising => _systemMode == IFM.SDK.VSE.SystemMode.Supervise;
    public bool Timeout
    {
        get;
        private set
        {
            if (field == value)
                return;
            field = value;
            TimeoutChanged?.Invoke();
        }
    }
    public Uri Uri { get; }

    public event Action? ParametersChanged;
    public event Action<Measurement>? RawDataReceived;
    public event Action? SystemModeChanged;
    public event Action? TimeoutChanged;

    public VseDevice(string address, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException($"Device address cannot be null or empty.", nameof(address));

        DeviceAddress = address;
        Uri = new UriBuilder(DeviceAddress).Uri;
        _logger = logger;

        Core = IoTCoreFactory.Create($"IoTCore-{DeviceAddress}");
        _httpClientNetAdapterFactory = new HttpClientNetAdapterFactory(new MessageConverter(),
            TimeSpan.FromSeconds(10), true);

        Core.RegisterClientNetAdapterFactory(_httpClientNetAdapterFactory);

        _loggerProvider = new ForwardLoggerProvider(logger);
        _loggerFactory = new LoggerFactory([_loggerProvider]);
        var configuration = new VSEIoTCoreConfiguration(Uri.DnsSafeHost, Uri.Port);
        _vseCore = new(configuration, _loggerFactory, Core);
        _vseCore.TreeRebuildCompleted += VseCore_TreeRebuildCompleted;
        _vseCore.Run();

        VseRawDataManager.RawDataReceived += OnRawDataReceived;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource.Cancel();

        _pollingTimer?.Dispose();
        _pollingTimer = null;
        _pollingTask?.Wait();

        _writingTimer?.Dispose();
        _writingTimer = null;
        _writingTask?.Wait();

        _httpClientNetAdapterFactory.Dispose();

        _vseCore.TreeRebuildCompleted -= VseCore_TreeRebuildCompleted;
        _vseCore.Dispose();

        Core.Dispose();

        _registeredAddresses.Clear();
        _writingAddresses.Clear();

        VseRawDataManager.RawDataReceived -= OnRawDataReceived;

        _cancellationTokenSource.Dispose();
        _loggerFactory.Dispose();
        _loggerProvider.Dispose();

        _disposed = true;
    }

    internal bool GetIsServiceElementExisting(string address)
        => Core.GetServiceElement(address) is not null;

    public void GetRecordingRawData(string identifier, int sensorIndex, int samplingRate, int duration, bool ignoreTimedValueArray)
        => VseRawDataManager.CollectRawData(this, identifier, sensorIndex, samplingRate, duration, ignoreTimedValueArray,
            (ex) => _logger.RawDataPullingErrorOccurred(ex, DeviceAddress));

    private static bool GetValueFromService<T>(IServiceElement serviceElement, out T? value)
    {
        value = default;
        if (serviceElement.Invoke() is not VariantObject variant)
            return false;
        value = variant["value"].ToObject<T>();
        return true;
    }

    private void LogConnectionReEstablished(ref string lastLoggedMessage)
    {
        if (string.IsNullOrWhiteSpace(lastLoggedMessage))
            return;
        lastLoggedMessage = string.Empty;
        _logger.ConnectionReEstablished(DeviceAddress);
    }

    private void LogError(ref string lastLoggedMessage, Exception ex, string action)
    {
        string error = ex.Message;
        if (string.Equals(lastLoggedMessage, error, StringComparison.OrdinalIgnoreCase))
            return;
        lastLoggedMessage = error;
        _logger.ExecutionErrorOccurred(ex, action, DeviceAddress);
    }

    private void OnRawDataReceived(Measurement measurement)
        => RawDataReceived?.Invoke(measurement);

    private async Task PollingTaskAsync(PeriodicTimer timer)
    {
        _logger.PollingStarted(DeviceAddress);
        while (await timer.WaitForNextTickAsync(CancellationToken.None))
            await PollingAsync();
        _logger.PollingStopped(DeviceAddress);
    }

    private async Task PollingAsync()
    {
        try
        {
            if (Initialized is false)
                return;

            ReadSystemMode();

            if (Connected && (Supervising || Measuring))
                Timeout = false;
            else if (Timeout is false)
            {
                _logger.ConnectionLost(DeviceAddress);
                Timeout = true;
            }

            if (Timeout || Measuring)
                return;

            IEnumerable<(string Address, IVseSubscriberFunctionBlock[] Blocks)> registeredAddresses = _registeredAddresses
                .ToDictionary(x => x.Key, x => x.Value.ToArray())
                .Select(x => (Address: x.Key, Blocks: x.Value));

            foreach ((string address, IVseSubscriberFunctionBlock[] blocks) in registeredAddresses)
            {
                VariantObject? value = Core.GetDataValueAsObject(address);
                if (value is not null)
                    foreach (IVseSubscriberFunctionBlock functionBlock in blocks)
                        functionBlock.SetValue(value);
            }

            LogConnectionReEstablished(ref _lastPollingErrorMessage);
        }
        catch (Exception ex)
        {
            LogError(ref _lastPollingErrorMessage, ex, nameof(PollingAsync));
            try { await Task.Delay(ErrorWaitTime, _cancellationTokenSource.Token); }
            catch (OperationCanceledException) { }
        }
    }

    private void ReadSystemMode()
    {
        if (GetValueFromService(GetSystemModeService, out string? systemModeText) is false)
            return;
        if (Enum.TryParse(systemModeText, true, out IFM.SDK.VSE.SystemMode systemMode) is false)
            return;

        bool changed = _systemMode != systemMode;
        _systemMode = systemMode;
        if (changed)
            SystemModeChanged?.Invoke();
    }

    public void RegisterAddress(string nodePath, IVseSubscriberFunctionBlock functionBlock)
    {
        if (_registeredAddresses.TryGetValue(nodePath, out List<IVseSubscriberFunctionBlock>? functionblocks) == false)
            _registeredAddresses[nodePath] = functionblocks = [];

        if (functionblocks.Contains(functionBlock) == false)
            functionblocks.Add(functionBlock);
    }

    private void StartPolling()
    {
        lock (_pollingTimerLock)
            if (_pollingTimer is null)
            {
                _pollingTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(PollingWaitTime));
                _pollingTask = PollingTaskAsync(_pollingTimer);
            }
    }

    public void UnregisterAddress(IVseSubscriberFunctionBlock functionBlock)
    {
        var nodePathsToRemove = new List<string>();

        foreach (KeyValuePair<string, List<IVseSubscriberFunctionBlock>> current in _registeredAddresses)
            lock (current.Value)
            {
                current.Value.Remove(functionBlock);
                if (current.Value.Count == 0)
                    nodePathsToRemove.Add(current.Key);
            }

        foreach (string path in nodePathsToRemove)
            _registeredAddresses.TryRemove(path, out _);
    }

    private void VseCore_TreeRebuildCompleted()
    {
        // HACK: bei der ersten Initialisierung des VSEIoTCoreAdapter werden zwei TreeRebuilds durchgeführt
        // TODO: besseren Anhaltspunkt für das Handling finden
        _treeRebuildCount++;

        if (Initialized)
        {
            ReadSystemMode();

            if (Connected && Supervising)
            {
                string[] relevantEntries = VseParser.GetRelevantEntries(Core);
                if (_lastRelevantEntries is not null && Enumerable.SequenceEqual(_lastRelevantEntries, relevantEntries, StringComparer.OrdinalIgnoreCase) is false)
                    ParametersChanged?.Invoke();
                _lastRelevantEntries = relevantEntries;
            }

            StartPolling();
        }
    }

    public void Write<T>(string address, T value)
    {
        lock (_writingAddresses)
            _writingAddresses.AddOrUpdate(address, value, (key, oldValue) => oldValue = value);

        lock (_writingTimerLock)
            if (_writingTimer is null)
            {
                _writingTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(WritingWaitTime));
                _writingTask = WritingTaskAsync(_writingTimer);
            }
    }

    private async Task WritingTaskAsync(PeriodicTimer timer)
    {
        _logger.WritingStarted(DeviceAddress);
        while (await timer.WaitForNextTickAsync(CancellationToken.None))
            Writing();
        _logger.WritingStopped(DeviceAddress);
    }

    private void Writing()
    {
        List<string> addressesNotFound = [];
        Dictionary<string, object?> addresses = [];

        try
        {
            ReadSystemMode();

            if (_writingAddresses.IsEmpty == false && Initialized && Connected && Supervising)
            {
                lock (_writingAddresses)
                {
                    addresses = _writingAddresses.ToDictionary(x => x.Key, x => x.Value);
                    _writingAddresses.Clear();
                }

                foreach (KeyValuePair<string, object?> pair in addresses)
                {
                    // TODO: schreiben in IoTCoreExtensions auslagern
                    string address = $"{pair.Key}/setdata";

                    if (Core.GetElementByAddress($"{address}") is not IServiceElement setDataService)
                    {
                        if (Connected == false)
                        {
                            addressesNotFound.AddRange(addresses.Keys);
                            _logger.DeviceToWriteNotConnected(DeviceAddress);
                            break;
                        }

                        addressesNotFound.Add(address);
                        _logger.AddressToWriteNotFound(address, DeviceAddress);
                        continue;
                    }

                    VariantObject newValue = new()
                    {
                        { "value", Variant.FromObject(pair.Value) },
                    };

                    setDataService.Invoke(newValue);
                }

                LogConnectionReEstablished(ref _lastWritingErrorMessage);
            }
        }
        catch (Exception ex)
        {
            LogError(ref _lastWritingErrorMessage, ex, nameof(Writing));
            lock (_writingAddresses)
                // wenn etwas schief läuft, werden die Adressen wieder hinzugefügt, damit sie im nächsten Schritt nochmal verschickt werden
                foreach (KeyValuePair<string, object?> pair in addresses)
                    if (addressesNotFound.Contains(pair.Key, StringComparer.Ordinal) == false)
                        _writingAddresses.TryAdd(pair.Key, pair.Value);
        }
    }
}
