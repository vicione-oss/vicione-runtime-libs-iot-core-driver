using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;
using ViciOne.Driver.IoTCore.IoddManagement;

namespace ifm.IOLink;

public sealed partial class DeviceTreeSubscriber
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Verwerfbare Felder verwerfen", Justification = "Wird vom DeviceManager freigegeben.")]
    private IoLinkMaster? _device;
    private byte _deviceTreeUpdateRequested;
    private readonly Lock _deviceTreeLock = new();
    private readonly Lock _startUpdateLock = new();
    private Task _runningTask = Task.CompletedTask;

    public override void InitializeI()
    {
        base.InitializeI();

        IoLinkParser.Initialize(Settings.IoddDirectory.Value ?? string.Empty, Settings.IoddAutoDownload.Value, Logger);

        // Setzt den Wert auch wenn der Manager schon initialisiert war
        IoddParserManager.IoddAutoDownload = Settings.IoddAutoDownload.Value;
    }

    public override void InitializeII()
    {
        base.InitializeII();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (Settings.Url.Value is null)
            throw new ArgumentException("Invalid address configuration.");

        _device = driver.GetOrAddIoLinkMaster(Settings.Url.Value);
        _device.ChildDevicesChanged += OnDeviceChildDevicesChanged;
        _device.TimeoutChanged += OnDeviceTimeoutChanged;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            if (_device is not null)
            {
                _device.TimeoutChanged -= OnDeviceTimeoutChanged;
                _device.ChildDevicesChanged -= OnDeviceChildDevicesChanged;
                _device = null;
            }

            // TODO: CancellationToken im Callback des Tasks beachten und korrekt auf Beendigung warten
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            try { _runningTask?.Dispose(); }
            catch (InvalidOperationException) { }
        }
    }

    private void OnDeviceChildDevicesChanged()
        => StartDeviceTreeUpdate();

    private void OnDeviceTimeoutChanged()
        => UpdateIsOfflineInCurrentTree(_device!.Timeout);

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled is false)
            return;

        if ((ProcessDataInputs.Trigger.Changed || cycleInfo.IsFirstCycle) &&
            ProcessDataInputs.Trigger.Value)
            StartDeviceTreeUpdate();

        SetConnectorValueIfNotEqual(ProcessDataOutputs.IoddDirectory, IoddParserManager.IoddDirectory);
    }

    private void PublishDeviceTree(DeviceTreeIoLinkMaster tree)
    {
        lock (_deviceTreeLock)
        {
            // Wenn ein Baum angefragt wird, während das Gerät im Timeout ist, dann wird ein Baum erzeugt,
            // der das Gerät nicht so repräsentiert, wie es eigentlich ist. Daher wird dieser Baum verworfen
            // und die Offline-Information in den ggf. bestehenden Baum geschrieben.
            // Hinweis: bewusst nur tree.IsOffline, kein zusätzlicher Re-Check von _device.Timeout hier -
            // das spiegelt den Timeout-Zustand exakt zum Zeitpunkt wider, an dem ParseDeviceAsync
            // abgeschlossen wurde (siehe IoLinkParser.ParseDeviceAsync). Ein separater erneuter Check an
            // dieser Stelle könnte den Timeout-Zustand zwischen Parse-Ende und hier erneut abweichend
            // vorfinden (z.B. durch ein kurzes erneutes Timeout-Flackern) und würde dann einen bereits
            // fertig und korrekt geparsten Baum verwerfen, ohne dass je ein neuer ChildDevicesChanged mehr
            // nachträfe, der das korrigieren könnte - der veröffentlichte Baum bliebe dauerhaft auf
            // IsOffline=true hängen, obwohl der Gerätezustand längst wieder korrekt ist.
            if (tree.IsOffline)
            {
                UpdateIsOfflineInCurrentTree(true, tree);
                return;
            }

            SetConnectorValueIfNotEqual(ProcessDataOutputs.DeviceTree, tree);
        }
    }

    private void StartDeviceTreeUpdate()
    {
        if (_device is null)
            return;

        // The check-then-act below (is a parse already running -> if not, start one) must be
        // atomic. Without this lock, two calls to StartDeviceTreeUpdate arriving close together
        // (e.g. two ChildDevicesChanged/TimeoutChanged events, or a new trigger racing the
        // ContinueWith below) could both observe _runningTask as completed and each start their
        // own ParseDeviceAsync, instead of the second one coalescing via
        // _deviceTreeUpdateRequested as intended. With two overlapping parses in flight, whichever
        // one happens to finish last wins the call to PublishDeviceTree - which is not necessarily
        // the one reflecting the more recent device state. Since nothing re-triggers a parse once
        // the device state has actually settled, a stale result winning that race would stay
        // published indefinitely.
        Task startedTask;
        lock (_startUpdateLock)
        {
            if (_runningTask.IsCompleted is false)
            {
                Interlocked.Exchange(ref _deviceTreeUpdateRequested, 1);
                return;
            }

            startedTask = _runningTask = Task.Run(async () =>
                {
                    try { PublishDeviceTree(await IoLinkParser.ParseDeviceAsync(_device, Logger)); }
                    catch (Exception ex) { Logger.IoLinkParseDeviceErrorOccurred(ex); }
                }, _cancellationTokenSource.Token);
        }

        startedTask.ContinueWith(_ =>
        {
            if (Interlocked.CompareExchange(ref _deviceTreeUpdateRequested, 0, 1) == 1)
                StartDeviceTreeUpdate();
        }, _cancellationTokenSource.Token, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    private void UpdateIsOfflineInCurrentTree(bool isOffline, DeviceTreeIoLinkMaster? fallbackTree = null)
    {
        lock (_deviceTreeLock)
        {
            if (ProcessDataOutputs.DeviceTree.Value is null)
            {
                SetConnectorValueIfNotEqual(ProcessDataOutputs.DeviceTree, fallbackTree);
                if (ProcessDataOutputs.DeviceTree.Value is null)
                    return;
            }
            ProcessDataOutputs.DeviceTree.Value.SetIsOffline(isOffline);
            ProcessDataOutputs.DeviceTree.MarkAsChanged();
        }
    }
}
