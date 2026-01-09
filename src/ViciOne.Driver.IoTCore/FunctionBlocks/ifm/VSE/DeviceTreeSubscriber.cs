using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

namespace ifm.VSE;

public sealed partial class DeviceTreeSubscriber
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Verwerfbare Felder verwerfen", Justification = "Wird vom DeviceManager freigegeben.")]
    private VseDevice? _device;
    private Task _runningTask = Task.CompletedTask;

    public override void InitializeII()
    {
        base.InitializeII();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (Settings.Url.Value is null)
            throw new ArgumentException("Invalid address configuration.");

        _device = driver.GetOrAddVseDevice(Settings.Url.Value);
        _device.ParametersChanged += OnDeviceParametersChanged;
        _device.SystemModeChanged += OnDeviceSystemModeChanged;
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
                _device.SystemModeChanged -= OnDeviceSystemModeChanged;
                _device.ParametersChanged -= OnDeviceParametersChanged;
                _device = null;
            }

            // TODO: CancellationToken im Callback des Tasks beachten und korrekt auf Beendigung warten
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            try { _runningTask?.Dispose(); }
            catch (InvalidOperationException) { }
        }
    }

    private void OnDeviceParametersChanged()
        => StartDeviceTreeUpdate();

    private void OnDeviceSystemModeChanged()
    {
        if (_device!.Timeout ||
            GetIsInRelevantSystemMode(_device) is false ||
            ProcessDataOutputs.DeviceTree.Value is null)
            return;

        // nur Kind-Knoten auf offline setzen, wenn Rohdaten-Download aktiv ist
        SetOnlyChildrenIsOffline(ProcessDataOutputs.DeviceTree.Value, _device.Measuring);

        ProcessDataOutputs.DeviceTree.MarkAsChanged();

        static bool GetIsInRelevantSystemMode(VseDevice device)
            => device.Supervising || device.Measuring;
    }

    private void OnDeviceTimeoutChanged()
    {
        if (ProcessDataOutputs.DeviceTree.Value is null ||
            ProcessDataOutputs.DeviceTree.Value.IsOffline == _device!.Timeout)
            return;

        ProcessDataOutputs.DeviceTree.Value.SetIsOffline(_device.Timeout);

        if (_device.Timeout is false && _device.Measuring)
            // Kind-Knoten auf offline lassen, wenn Rohdaten-Download aktiv ist
            SetOnlyChildrenIsOffline(ProcessDataOutputs.DeviceTree.Value, true);

        ProcessDataOutputs.DeviceTree.MarkAsChanged();
    }

    private static void SetOnlyChildrenIsOffline(DeviceTreeVseDevice tree, bool isOffline)
    {
        foreach (IDeviceTreeBase child in tree.Children)
            child.SetIsOffline(isOffline);
    }

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled is false)
            return;

        if (ProcessDataInputs.Trigger.Changed &&
            ProcessDataInputs.Trigger.Value)
            StartDeviceTreeUpdate();
    }

    private void StartDeviceTreeUpdate()
    {
        if (_runningTask.Status == TaskStatus.WaitingForActivation || _device is null)
            return;

        _runningTask = Task.Run(async () =>
            {
                try { SetConnectorValueIfNotEqual(ProcessDataOutputs.DeviceTree, await VseParser.ParseDeviceAsync(_device, Logger)); }
                catch (Exception ex) { Logger.ParsingErrorOccurred(ex); }
            }, _cancellationTokenSource.Token);
    }
}
