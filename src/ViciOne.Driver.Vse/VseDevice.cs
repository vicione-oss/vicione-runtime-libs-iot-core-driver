using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IFM.SDK.VSE;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.Vse;
using ViciOne.Communication.Vse.Data;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.Vse.Interfaces;

namespace ViciOne.Driver.Vse;

/// <summary>
/// Owns the connection to one VSE device: dispatches newly received counter/object/input/alarm/variant values to
/// whichever <see cref="IVseSubscriberFunctionBlock"/>s are registered for them, and batches outgoing writes.
/// <para>Shared across multiple driver instances via <see cref="DeviceManager"/> rather than owned by any one
/// of them.</para>
/// </summary>
public sealed class VseDevice : IDisposable
{
    /// <summary>
    /// The default TCP port for a device address with none specified.
    /// </summary>
    public static int DefaultPort { get; } = 3321;

    private const int WritingWaitTime = 500;

    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, List<IVseSubscriberFunctionBlock>> _registeredAddresses = new();
    private readonly ConcurrentDictionary<string, object?> _writingAddresses = new();
    private readonly Dictionary<string, Action> _itemUnsubscribersByAddress = [];
    private readonly Lock _itemUnsubscribersLock = new();
    private readonly Lock _writingAddressesLock = new();
    private readonly Lock _writingTimerLock = new();
    private IReadOnlyDictionary<string, object> _itemsById = new Dictionary<string, object>();
    private SystemMode _lastSystemMode = SystemMode.None;
    private bool _hasRebuiltBefore;
    private string _lastWritingErrorMessage = string.Empty;
    private PeriodicTimer? _writingTimer;
    private Task? _writingTask;
    private bool _disposed;

    /// <summary>
    /// The underlying VSE-SDK connection.
    /// </summary>
    internal VseConnection Connection { get; }

    /// <summary>
    /// Whether the connection to the device is currently established.
    /// </summary>
    public bool Connected => Connection.Device.ConnectionStatus == IFM.SDK.VSE.ConnectionStatus.Connected;

    /// <summary>
    /// The device's address, as passed to the constructor.
    /// </summary>
    public string DeviceAddress { get; }

    /// <summary>
    /// Whether the device tree has been built at least once (see <see cref="OnConnectionRebuilt"/>).
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    /// Whether the device is currently in its raw-data recording ("Measure") system mode.
    /// </summary>
    public bool Measuring => Connection.Status.SystemMode == SystemMode.Measure;

    /// <summary>
    /// Set by <see cref="DeviceManager"/> once no <see cref="VseDriver"/> instance still needs this device -
    /// null while at least one instance holds a reference to it.
    /// </summary>
    internal DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// Whether the device is currently in its normal monitoring ("Supervise") system mode.
    /// </summary>
    public bool Supervising => Connection.Status.SystemMode == SystemMode.Supervise;

    /// <summary>
    /// Whether the device is currently unreachable.
    /// </summary>
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

    /// <summary>
    /// The device's address, parsed as a <see cref="System.Uri"/>.
    /// </summary>
    public Uri Uri { get; }

    /// <summary>
    /// Raised when the device's parameters changed (see <see cref="OnConnectionRebuilt"/>).
    /// </summary>
    public event Action? ParametersChanged;

    /// <summary>
    /// Raised with a finished raw-data recording's result.
    /// </summary>
    public event Action<Measurement>? RawDataReceived;

    /// <summary>
    /// Raised when the device's system mode changed.
    /// </summary>
    public event Action? SystemModeChanged;

    /// <summary>
    /// Raised when <see cref="Timeout"/> changed.
    /// </summary>
    public event Action? TimeoutChanged;

    /// <summary>
    /// Creates a new device connection and starts connecting in the background.
    /// </summary>
    /// <param name="address">The VSE device's address.</param>
    /// <param name="logger">The logger to report connection issues to.</param>
    public VseDevice(string address, ILogger logger)
        : this(address, RequireConnection(address, logger), logger)
        => Connection.Connect();

    /// <summary>
    /// Wraps an already-built <see cref="VseConnection"/> directly, skipping the host/port-based construction -
    /// lets tests drive the address registration/dispatch/write logic below against a mocked device instead of a
    /// real hardware connection. Deliberately does not call <see cref="VseConnection.Connect"/> - tests raise
    /// the mocked device's own events to simulate connecting.
    /// </summary>
    internal VseDevice(string address, VseConnection connection, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException($"Device address cannot be null or empty.", nameof(address));

        DeviceAddress = address;
        Uri = new UriBuilder(DeviceAddress).Uri;
        _logger = logger;

        Connection = connection;
        Connection.Rebuilt += OnConnectionRebuilt;
        Connection.Status.Changed += OnStatusChanged;

        VseRawDataCollector.RawDataReceived += OnRawDataReceived;
    }

    private static VseConnection RequireConnection(string address, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException($"Device address cannot be null or empty.", nameof(address));

        Uri uri = new UriBuilder(address).Uri;
        return new VseConnection(uri.DnsSafeHost, uri.Port, logger);
    }

    /// <summary>
    /// Stops the connection, unsubscribes from every item, and releases the underlying VSE-SDK connection.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _writingTimer?.Dispose();
        _writingTimer = null;
        _writingTask?.Wait();

        UnsubscribeFromItems();
        Connection.Rebuilt -= OnConnectionRebuilt;
        Connection.Status.Changed -= OnStatusChanged;
        Connection.Dispose();

        _registeredAddresses.Clear();
        _writingAddresses.Clear();

        VseRawDataCollector.RawDataReceived -= OnRawDataReceived;

        _disposed = true;
    }

    /// <summary>
    /// Whether the device's data model currently has an item at the given node path.
    /// </summary>
    internal bool GetIsServiceElementExisting(string address)
        => _itemsById.ContainsKey(address);

    /// <summary>
    /// Queues a raw-data recording request for the given sensor on this device's <see cref="VseRawDataCollector"/>.
    /// </summary>
    /// <param name="identifier">An identifier for correlating this request with its eventual result.</param>
    /// <param name="sensorIndex">The sensor to record from.</param>
    /// <param name="sampleRate">The recording's sample rate, in Hz.</param>
    /// <param name="duration">The recording's duration, in milliseconds.</param>
    /// <param name="ignoreTimedValueArray">Whether to skip decoding the recorded raw data into timed values.</param>
    public void GetRecordingRawData(string identifier, int sensorIndex, int sampleRate, int duration, bool ignoreTimedValueArray)
        => VseRawDataCollector.CollectRawData(this, identifier, sensorIndex, sampleRate, duration, ignoreTimedValueArray,
            (ex) => _logger.RawDataPullingErrorOccurred(ex, DeviceAddress));

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

    /// <summary>
    /// Rebuilds the node-path-to-item lookup after the underlying connection rebuilds its device tree, and
    /// re-establishes live subscriptions for every already-registered address.
    /// </summary>
    private void OnConnectionRebuilt()
    {
        UnsubscribeFromItems();

        // Only the address-to-domain-object lookup is needed here, not the full DeviceTree (that's what
        // DeviceTreeSubscriber's own ParseDeviceAsync call is for) - BuildItemsById skips materializing a whole
        // DeviceTreeVse*/DeviceTreeProcessData node graph just to redirect this dispatch table.
        // Connection's categories are only null before its very first Rebuild() - this method only runs as a
        // result of a Rebuild() having just happened, so they're guaranteed non-null here.
        _itemsById = VseParser.BuildItemsById(Connection.DigitalAlarms!, Connection.Counters!, Connection.Inputs!,
            Connection.DeviceObjects!, Connection.Variants!);

        // Only the addresses some FunctionBlock actually registered for need a live subscription - most of a
        // device's counters/objects/inputs/alarms typically have nothing listening to them.
        foreach (string address in _registeredAddresses.Keys)
            if (_itemsById.TryGetValue(address, out object? item))
                SubscribeToItem(address, item);

        Initialized = true;

        // Matches the original's tree-diffing semantics: don't report "parameters changed" the very first time
        // there is anything to report at all, only on subsequent, genuine rebuilds.
        if (_hasRebuiltBefore)
            ParametersChanged?.Invoke();
        _hasRebuiltBefore = true;
    }

    private void OnRawDataReceived(Measurement measurement)
        => RawDataReceived?.Invoke(measurement);

    private void OnStatusChanged()
    {
        Timeout = Connection.Status.ConnectionStatus != IFM.SDK.VSE.ConnectionStatus.Connected;

        SystemMode currentMode = Connection.Status.SystemMode;
        if (currentMode == _lastSystemMode)
            return;

        _lastSystemMode = currentMode;
        SystemModeChanged?.Invoke();
    }

    /// <summary>
    /// Registers a FunctionBlock to receive newly received values for one node path.
    /// </summary>
    /// <param name="nodePath">The node path in the device's data model to subscribe to.</param>
    /// <param name="functionBlock">The FunctionBlock to dispatch received values to.</param>
    public void RegisterAddress(string nodePath, IVseSubscriberFunctionBlock functionBlock)
    {
        // GetOrAdd (rather than TryGetValue + indexer-set) keeps two concurrent first-registrations for the
        // same nodePath from each creating their own list and clobbering one another - both end up sharing
        // whichever candidate list actually won.
        List<IVseSubscriberFunctionBlock> candidate = [];
        List<IVseSubscriberFunctionBlock> functionblocks = _registeredAddresses.GetOrAdd(nodePath, candidate);
        bool isNewAddress = ReferenceEquals(functionblocks, candidate);

        // The list itself isn't thread-safe (unlike the ConcurrentDictionary holding it) - Dispatch() and
        // UnregisterAddress() both lock the same list instance before touching it.
        lock (functionblocks)
            if (functionblocks.Contains(functionBlock) == false)
                functionblocks.Add(functionBlock);

        // Registration can happen after the device has already (re)built once - pick up the live subscription
        // immediately in that case, instead of waiting for the next rebuild.
        if (isNewAddress && _itemsById.TryGetValue(nodePath, out object? item))
            SubscribeToItem(nodePath, item);
    }

    private void SubscribeToItem(string address, object item)
    {
        Action? unsubscribe = item switch
        {
            Counter counter => SubscribeToCounter(address, counter),
            DeviceObject deviceObject => SubscribeToDeviceObject(address, deviceObject),
            Input input => SubscribeToInput(address, input),
            DigitalAlarm alarm => SubscribeToDigitalAlarm(address, alarm),
            Variants variants => SubscribeToVariants(address, variants),
            _ => null,
        };

        if (unsubscribe is null)
            return;

        lock (_itemUnsubscribersLock)
            _itemUnsubscribersByAddress[address] = unsubscribe;

        // The SDK reports a value only once it changes, so a value that never moves is never reported at all:
        // an alarm that is false when the device comes up and stays false leaves its subscribers with nothing
        // but their own defaults, and nothing distinguishes "no alarm" from "never heard from". A measured
        // value hides this, because it starts moving right away. Report what the device currently holds once
        // here, so every subscriber begins with the device's state rather than a coincidence.
        Dispatch(address, item);
    }

    private Action SubscribeToCounter(string address, Counter counter)
    {
        void OnChanged() => Dispatch(address, counter);
        counter.Changed += OnChanged;
        return () => counter.Changed -= OnChanged;
    }

    private Action SubscribeToDeviceObject(string address, DeviceObject deviceObject)
    {
        void OnChanged() => Dispatch(address, deviceObject);
        deviceObject.Changed += OnChanged;
        deviceObject.FlagsChanged += OnChanged;
        return () =>
        {
            deviceObject.Changed -= OnChanged;
            deviceObject.FlagsChanged -= OnChanged;
        };
    }

    private Action SubscribeToInput(string address, Input input)
    {
        void OnChanged() => Dispatch(address, input);
        input.Changed += OnChanged;
        return () => input.Changed -= OnChanged;
    }

    private Action SubscribeToDigitalAlarm(string address, DigitalAlarm alarm)
    {
        void OnChanged() => Dispatch(address, alarm);
        alarm.Changed += OnChanged;
        return () => alarm.Changed -= OnChanged;
    }

    private Action SubscribeToVariants(string address, Variants variants)
    {
        void OnActiveVariantChanged(Variant _) => Dispatch(address, variants);
        variants.ActiveVariantChanged += OnActiveVariantChanged;
        return () => variants.ActiveVariantChanged -= OnActiveVariantChanged;
    }

    private void Dispatch(string address, object item)
    {
        if (_registeredAddresses.TryGetValue(address, out List<IVseSubscriberFunctionBlock>? functionBlocks) == false)
            return;

        IVseSubscriberFunctionBlock[] snapshot;
        lock (functionBlocks)
            snapshot = [.. functionBlocks];

        foreach (IVseSubscriberFunctionBlock functionBlock in snapshot)
            functionBlock.SetValue(item);
    }

    /// <summary>
    /// Removes every node-path registration for the given FunctionBlock.
    /// </summary>
    /// <param name="functionBlock">The FunctionBlock to unregister.</param>
    public void UnregisterAddress(IVseSubscriberFunctionBlock functionBlock)
    {
        List<string> nodePathsToRemove = [];

        foreach (KeyValuePair<string, List<IVseSubscriberFunctionBlock>> current in _registeredAddresses)
            lock (current.Value)
            {
                current.Value.Remove(functionBlock);
                if (current.Value.Count == 0)
                    nodePathsToRemove.Add(current.Key);
            }

        foreach (string path in nodePathsToRemove)
        {
            _registeredAddresses.TryRemove(path, out _);
            UnsubscribeFromItem(path);
        }
    }

    private void UnsubscribeFromItem(string address)
    {
        lock (_itemUnsubscribersLock)
        {
            if (_itemUnsubscribersByAddress.Remove(address, out Action? unsubscribe))
                unsubscribe();
        }
    }

    private void UnsubscribeFromItems()
    {
        lock (_itemUnsubscribersLock)
        {
            foreach (Action unsubscribe in _itemUnsubscribersByAddress.Values)
                unsubscribe();
            _itemUnsubscribersByAddress.Clear();
        }
    }

    /// <summary>
    /// Queues a value to be written to the given node path on the next writing cycle, replacing any not-yet-sent
    /// value already queued for that path.
    /// </summary>
    /// <param name="address">The node path in the device's data model to write to.</param>
    /// <param name="value">The value to write.</param>
    public void Write<T>(string address, T value)
    {
        lock (_writingAddressesLock)
            _writingAddresses.AddOrUpdate(address, value, (key, oldValue) => oldValue = value);

        lock (_writingTimerLock)
            if (_writingTimer is null)
            {
                _writingTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(WritingWaitTime));
                _writingTask = WritingTaskAsync(_writingTimer);
            }
    }

    /// <summary>
    /// Runs <see cref="Writing"/> on every tick of <paramref name="timer"/> until it is disposed.
    /// </summary>
    private async Task WritingTaskAsync(PeriodicTimer timer)
    {
        _logger.WritingStarted(DeviceAddress);
        while (await timer.WaitForNextTickAsync(CancellationToken.None))
            Writing();
        _logger.WritingStopped(DeviceAddress);
    }

    /// <summary>
    /// Sends every queued write to the device, but only while it is connected and in its "Supervise" system
    /// mode - writes are left queued (not dropped) otherwise, so they go out once the device becomes ready.
    /// <para>On failure, any address not already known to be missing from the data model is re-queued so it is
    /// retried on the next cycle.</para>
    /// </summary>
    private void Writing()
    {
        List<string> addressesNotFound = [];
        Dictionary<string, object?> addresses = [];

        try
        {
            if (_writingAddresses.IsEmpty == false && Initialized && Connected && Supervising)
            {
                lock (_writingAddressesLock)
                {
                    addresses = _writingAddresses.ToDictionary(x => x.Key, x => x.Value);
                    _writingAddresses.Clear();
                }

                foreach (KeyValuePair<string, object?> pair in addresses)
                {
                    if (_itemsById.TryGetValue(pair.Key, out object? item) == false)
                    {
                        addressesNotFound.Add(pair.Key);
                        _logger.AddressToWriteNotFound(pair.Key, DeviceAddress);
                        continue;
                    }

                    switch (item)
                    {
                        case Input input:
                            input.Value = Convert.ToSingle(pair.Value, CultureInfo.InvariantCulture);
                            break;
                        case Variants variants:
                            variants.ActiveVariantId = Convert.ToInt32(pair.Value, CultureInfo.InvariantCulture);
                            break;
                    }
                }

                LogConnectionReEstablished(ref _lastWritingErrorMessage);
            }
        }
        catch (Exception ex)
        {
            LogError(ref _lastWritingErrorMessage, ex, nameof(Writing));
            lock (_writingAddressesLock)
                // If something goes wrong, re-add the addresses so they get sent again on the next tick.
                foreach (KeyValuePair<string, object?> pair in addresses)
                    if (addressesNotFound.Contains(pair.Key, StringComparer.Ordinal) == false)
                        _writingAddresses.TryAdd(pair.Key, pair.Value);
        }
    }
}
