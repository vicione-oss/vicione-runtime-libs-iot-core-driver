using System.Collections.Concurrent;
using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Subscribes to all events the Vse-SDK <see cref="IVSEDevice"/> raises and re-dispatches them, one at a time,
/// on a dedicated background task. This decouples the (potentially expensive) event handlers from the SDK's own
/// callback thread and gives every event a single, consistent processing order.
/// </summary>
public sealed partial class DeviceEventManager : IDisposable
{
    private readonly IVSEDevice _device;
    private readonly ConcurrentQueue<IDeviceEvent> _evtQueue = new();
    private readonly AutoResetEventAsync _evtTrigger = new();
    private readonly CancellationTokenSource _cancelationTokenSource = new();
    private bool _disposedValue;

    private IDeviceEvent? _currentEvent;
    private IDeviceEvent? _lastEvent;
    private readonly Lock _lock = new();
    private readonly ILogger? _logger;

    // Read live rather than captured at construction: this manager is built before the device first connects, and
    // the network settings are only populated once a connection has been established.
    private string DeviceAddress => _device.NetworkSettings?.IPAddress ?? string.Empty;

    public IDeviceEvent? CurrentEvent { get { lock (_lock) { return _currentEvent; } } }
    public IDeviceEvent? LastEvent { get { lock (_lock) { return _lastEvent; } } }

    /// <summary>
    /// True once the event queue has been idle for a while, i.e. no device event has changed the tree recently.
    /// </summary>
    public bool TreeStable { get; private set; }

    /// <summary>
    /// The device connection event. Sent when the connection status has changed.
    /// </summary>
    public event Action<ConnectionEvent>? ConnectionStatusChanged;

    /// <summary>
    /// The device connected event. Sent when the connection status has changed to connected.
    /// </summary>
    public event Action<IVSEDevice>? Connected;

    /// <summary>
    /// The device event triggered by counters reset.
    /// </summary>
    public event Action<ResetCountersEvent>? CountersResetEvent;

    /// <summary>
    /// The device event triggered by the device when the device parameters have been changed.
    /// </summary>
    public event Action<ParameterChangedEvent>? ParametersChanged;

    /// <summary>
    /// The device event triggered by reboot.
    /// </summary>
    public event Action<RebootEvent>? RebootEvent;

    /// <summary>
    /// The device event triggered by resetting its date/time.
    /// </summary>
    public event Action<ResetDateTimeEvent>? ResetDateTimeEvent;

    /// <summary>
    /// The device event triggered by resetting history.
    /// </summary>
    public event Action<ResetHistoryEvent>? ResetHistoryEvent;

    /// <summary>
    /// The device event triggered by device self-test complete.
    /// </summary>
    public event Action<SelfTestCompletedEvent>? SelfTestCompleted;

    /// <summary>
    /// The device system mode event. Sent when the device mode has changed (e.g. Supervise &lt;-&gt; Measure).
    /// </summary>
    public event Action<ModeChangedEvent>? SystemModeChanged;

    /// <summary>
    /// The device event triggered by teach in.
    /// </summary>
    public event Action<TeachEvent>? TeachInEvent;

    /// <summary>
    /// The device event triggered by changing the active variant.
    /// </summary>
    public event Action<VariantChangedEvent>? VariantChanged;

    public DeviceEventManager(IVSEDevice device, ILoggerFactory? loggerFactory)
    {
        _device = device;
        _device.ConnectionStatusChanged += HandleConnectionStatusChanged;
        _device.SystemModeChanged += HandleModeChanged;
        _device.RebootEvent += HandleRebootEvent;
        _device.VariantChanged += HandleVariantChangedEvent;
        _device.TeachInEvent += HandleTeachInEvent;
        _device.CountersResetEvent += HandleCountersResetEvent;
        _device.SelfTestCompleted += HandleSelfTestCompleted;
        _device.ParametersChanged += HandleParametersChanged;
        _device.ResetDateTimeEvent += HandleResetDateTimeEvent;
        _device.ResetHistoryEvent += HandleResetHistoryEvent;
        _logger = loggerFactory?.CreateLogger("vse-iot-core" + GetType());

        TreeStable = false;
        // Fire-and-forget: the loop runs for the lifetime of this instance and is stopped via _cancelationTokenSource in Dispose.
        _ = Task.Run(ProcessEvtQueue);
    }

    private async void ProcessEvtQueue()
    {
        while (_cancelationTokenSource.IsCancellationRequested == false)
        {
            try
            {
                while (_evtQueue.IsEmpty == false)
                {
                    if (_evtQueue.TryDequeue(out IDeviceEvent? evt))
                    {
                        SetCurrentEvent(evt);
                        LogEventDequeued(evt.GetType());
                        switch (evt)
                        {
                            case ConnectionEvent connectionEvent:
                                TreeStable = false;
                                LogEventDataConnectionState(connectionEvent.Status);
                                ProcessConnectionStatusChange(connectionEvent);
                                break;
                            case ModeChangedEvent modeChangedEventEvent:
                                TreeStable = false;
                                LogEventDataModeChanged(modeChangedEventEvent.Mode, modeChangedEventEvent.Origin);
                                ProcessModeChange(modeChangedEventEvent);
                                break;
                            case RebootEvent rebootEvent:
                                TreeStable = false;
                                ProcessReboot(rebootEvent);
                                break;
                            case VariantChangedEvent variantChangedEvent:
                                ProcessVariantChanged(variantChangedEvent);
                                break;
                            case TeachEvent teachEvent:
                                ProcessTeachIn(teachEvent);
                                break;
                            case ResetCountersEvent resetCountersEvent:
                                ProcessCounterReset(resetCountersEvent);
                                break;
                            case SelfTestCompletedEvent selfTestCompletedEvent:
                                TreeStable = false;
                                ProcessSelfTestCompleted(selfTestCompletedEvent);
                                break;
                            case ParameterChangedEvent parameterChangedEvent:
                                TreeStable = false;
                                ProcessParameterChange(parameterChangedEvent);
                                break;
                            case ResetDateTimeEvent resetDateTimeEvent:
                                ProcessResetDateTime(resetDateTimeEvent);
                                break;
                            case ResetHistoryEvent resetHistoryEvent:
                                ProcessResetHistory(resetHistoryEvent);
                                break;
                        }
                        SetCurrentEvent(null);
                    }
                }

                // No event arrived within 2s: consider the tree settled until the next Enqueue/device callback wakes us up again.
                while (await _evtTrigger.WaitAsync(2000).ConfigureAwait(false) == false)
                {
                    TreeStable = true;
                }
            }
            catch (Exception ex)
            {
                LogProcessEventQueueFailed(ex);
            }
        }
    }

    private void ProcessResetHistory(ResetHistoryEvent evt)
        => ProcessEvent(() => ResetHistoryEvent?.Invoke(evt), evt);

    private void ProcessResetDateTime(ResetDateTimeEvent evt)
        => ProcessEvent(() => ResetDateTimeEvent?.Invoke(evt), evt);

    private void ProcessParameterChange(ParameterChangedEvent evt)
        => ProcessEvent(() => ParametersChanged?.Invoke(evt), evt);

    private void ProcessSelfTestCompleted(SelfTestCompletedEvent evt)
        => ProcessEvent(() => SelfTestCompleted?.Invoke(evt), evt);

    private void ProcessCounterReset(ResetCountersEvent evt)
        => ProcessEvent(() => CountersResetEvent?.Invoke(evt), evt);

    private void ProcessTeachIn(TeachEvent evt)
        => ProcessEvent(() => TeachInEvent?.Invoke(evt), evt);

    private void ProcessVariantChanged(VariantChangedEvent evt)
        => ProcessEvent(() => VariantChanged?.Invoke(evt), evt);

    private void ProcessReboot(RebootEvent evt)
        => ProcessEvent(() => RebootEvent?.Invoke(evt), evt);

    private void ProcessModeChange(ModeChangedEvent evt)
        => ProcessEvent(() => SystemModeChanged?.Invoke(evt), evt);

    private void ProcessConnectionStatusChange(ConnectionEvent evt)
    {
        ProcessEvent(() => ConnectionStatusChanged?.Invoke(evt), evt);

        if (evt.Status == ConnectionStatus.Connected)
            ProcessEvent(() => Connected?.Invoke(evt.Device!), evt);
    }

    private void ProcessEvent(Action invocation, IDeviceEvent evt)
    {
        try
        {
            LogProcessingEvent(evt.GetType());
            DateTime startTime = DateTime.Now;
            invocation();
            LogProcessingTook(evt.GetType(), (DateTime.Now - startTime).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            LogProcessingEventFailed(ex, evt.GetType());
        }
    }

    private void HandleVariantChangedEvent(IVSEDevice device, IVariant newVariant)
        => Enqueue(new VariantChangedEvent(device, newVariant));

    private void HandleConnectionStatusChanged(IVSEDevice device, ConnectionStatus newStatus)
        => Enqueue(new ConnectionEvent(device, newStatus));

    private void HandleModeChanged(IVSEDevice device, SystemMode newSysMode, Origin origin)
        => Enqueue(new ModeChangedEvent(device, newSysMode, origin));

    private void HandleRebootEvent()
        => Enqueue(new RebootEvent(_device));

    private void HandleResetHistoryEvent()
        => Enqueue(new ResetHistoryEvent(null));

    private void HandleResetDateTimeEvent(DateTime date)
        => Enqueue(new ResetDateTimeEvent(null, date));

    private void HandleParametersChanged(IVSEDevice device)
        => Enqueue(new ParameterChangedEvent(device));

    private void HandleSelfTestCompleted(IVSEDevice device, SelfTestResult selfTestResult, Origin origin)
        => Enqueue(new SelfTestCompletedEvent(device, selfTestResult));

    private void HandleCountersResetEvent(int counter)
        => Enqueue(new ResetCountersEvent(null, counter));

    private void HandleTeachInEvent(ITeachIn teachIn)
        => Enqueue(new TeachEvent(null, teachIn));

    private void Enqueue(IDeviceEvent evt)
    {
        LogEventEnqueued(evt.GetType());
        _evtQueue.Enqueue(evt);
        _evtTrigger.Set();
    }

    private void SetCurrentEvent(IDeviceEvent? evt)
    {
        lock (_lock)
        {
            _lastEvent = _currentEvent;
            _currentEvent = evt;
        }
    }

    /// <summary>
    /// Blocks until the event queue has been idle (no <see cref="CurrentEvent"/>) for a continuous
    /// <paramref name="settleWindowMs"/> window, or throws <see cref="TimeoutException"/> after
    /// <paramref name="timeoutMs"/>. Used before operations that switch device state (system mode, active
    /// variant, ...) to avoid racing an in-flight critical event (mode change, reboot, teach-in, ...).
    /// </summary>
    public void WaitUntilQuiet(int timeoutMs = 5000, int settleWindowMs = 50)
    {
        DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
        DateTime? quietSince = null;

        while (true)
        {
            DateTime now = DateTime.Now;
            if (now > deadline)
                throw new TimeoutException("Timed out waiting for pending device events to finish processing.");

            if (CurrentEvent is not null)
            {
                quietSince = null;
            }
            else
            {
                quietSince ??= now;
                if ((now - quietSince.Value).TotalMilliseconds >= settleWindowMs)
                    return;
            }

            Thread.Sleep(10);
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;

        if (disposing)
        {
            _device.ConnectionStatusChanged -= HandleConnectionStatusChanged;
            _device.SystemModeChanged -= HandleModeChanged;
            _device.RebootEvent -= HandleRebootEvent;
            _device.VariantChanged -= HandleVariantChangedEvent;
            _device.TeachInEvent -= HandleTeachInEvent;
            _device.CountersResetEvent -= HandleCountersResetEvent;
            _device.SelfTestCompleted -= HandleSelfTestCompleted;
            _device.ParametersChanged -= HandleParametersChanged;
            _device.ResetDateTimeEvent -= HandleResetDateTimeEvent;
            _device.ResetHistoryEvent -= HandleResetHistoryEvent;

            TeachInEvent = null;
            CountersResetEvent = null;
            RebootEvent = null;
            SelfTestCompleted = null;
            ParametersChanged = null;
            ResetDateTimeEvent = null;
            SystemModeChanged = null;
            ConnectionStatusChanged = null;
            Connected = null;
            ResetHistoryEvent = null;
            VariantChanged = null;

            _cancelationTokenSource.Cancel();
            _evtTrigger.Set();
            _evtTrigger.Dispose();
            _cancelationTokenSource.Dispose();
            _evtQueue.Clear();
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
