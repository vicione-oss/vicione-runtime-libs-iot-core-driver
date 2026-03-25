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
            if (tree.IsOffline || (_device is not null && _device.Timeout))
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

        if (_runningTask.Status == TaskStatus.WaitingForActivation)
        {
            Interlocked.Exchange(ref _deviceTreeUpdateRequested, 1);
            return;
        }

        _runningTask = Task.Run(async () =>
            {
                try { PublishDeviceTree(await IoLinkParser.ParseDeviceAsync(_device, Logger)); }
                catch (Exception ex) { Logger.IoLinkParseDeviceErrorOccurred(ex); }
            }, _cancellationTokenSource.Token);
        _runningTask.ContinueWith(_ =>
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
