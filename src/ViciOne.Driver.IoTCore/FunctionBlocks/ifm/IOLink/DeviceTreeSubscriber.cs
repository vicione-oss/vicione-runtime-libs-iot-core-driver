using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Communication.IoTCore.IoddStore;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;
using ViciOne.Driver.IoTCore;

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

    /// <inheritdoc/>
    public override void InitializeI()
    {
        base.InitializeI();

        IoLinkParser.Initialize(Settings.IoddDirectory.Value ?? string.Empty, Settings.IoddAutoDownload.Value, Logger);

        // Sets the value even if the manager was already initialized
        IoddCatalog.IoddAutoDownload = Settings.IoddAutoDownload.Value;
    }

    /// <inheritdoc/>
    public override void InitializeII()
    {
        base.InitializeII();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (Settings.Url.Value is null)
            throw new ArgumentException("Invalid address configuration.");

        _device = driver.GetOrAddIoLinkMaster(Settings.Url.Value, Settings.Username.Value, Settings.Password.Value);
        _device.ChildDevicesChanged += OnDeviceChildDevicesChanged;
        _device.TimeoutChanged += OnDeviceTimeoutChanged;
    }

    /// <inheritdoc/>
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

            // TODO: honor the CancellationToken in the task's callback and wait correctly for completion
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            try { _runningTask?.Dispose(); }
            catch (InvalidOperationException) { }
        }
    }

    private void OnDeviceChildDevicesChanged()
        => StartDeviceTreeUpdate();

    private void OnDeviceTimeoutChanged()
        => UpdateStatusInCurrentTree(IoLinkParser.GetConnectionStatus(_device!));

    /// <inheritdoc/>
    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled is false)
            return;

        if ((ProcessDataInputs.Trigger.Changed || cycleInfo.IsFirstCycle) &&
            ProcessDataInputs.Trigger.Value)
            StartDeviceTreeUpdate();

        SetConnectorValueIfNotEqual(ProcessDataOutputs.IoddDirectory, IoddCatalog.IoddDirectory);
    }

    private void PublishDeviceTree(DeviceTreeIoLinkMaster tree)
    {
        lock (_deviceTreeLock)
        {
            // If a tree is requested while the device is in timeout (or requires authentication we don't
            // have), a tree gets built that doesn't represent the device the way it actually is. So this tree
            // is discarded and its status is written into whatever tree already exists instead.
            // Note: deliberately only tree.Status, no additional re-check of _device here - that reflects the
            // status exactly as it was at the moment ParseDeviceAsync completed (see
            // IoLinkParser.ParseDeviceAsync). A separate re-check at this point could find the status has
            // already changed again between the end of parsing and here (e.g. through a brief renewed timeout
            // flicker), and would then discard an already fully and correctly parsed tree without any further
            // ChildDevicesChanged ever following that could correct it - the published tree would stay stuck
            // off Online forever, even though the device state has long since become correct again.
            if (tree.Status != ConnectionStatus.Online)
            {
                UpdateStatusInCurrentTree(tree.Status, tree);
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

    private void UpdateStatusInCurrentTree(ConnectionStatus status, DeviceTreeIoLinkMaster? fallbackTree = null)
    {
        lock (_deviceTreeLock)
        {
            if (ProcessDataOutputs.DeviceTree.Value is null)
            {
                SetConnectorValueIfNotEqual(ProcessDataOutputs.DeviceTree, fallbackTree);
                if (ProcessDataOutputs.DeviceTree.Value is null)
                    return;
            }
            ProcessDataOutputs.DeviceTree.Value.SetStatus(status);
            ProcessDataOutputs.DeviceTree.MarkAsChanged();
        }
    }
}
