using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;
using ViciOne.Driver.Vse;

namespace ifm.Vse;

public sealed partial class DeviceTreeSubscriber
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Verwerfbare Felder verwerfen", Justification = "Wird vom DeviceManager freigegeben.")]
    private VseDevice? _device;
    private Task _runningTask = Task.CompletedTask;

    /// <inheritdoc/>
    public override void InitializeII()
    {
        base.InitializeII();

        if (Driver is not VseDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(VseDriver)}.");

        if (Settings.Url.Value is null)
            throw new ArgumentException("Invalid address configuration.");

        _device = driver.GetOrAddVseDevice(Settings.Url.Value);
        _device.ParametersChanged += OnDeviceParametersChanged;
        _device.SystemModeChanged += OnDeviceSystemModeChanged;
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
                _device.SystemModeChanged -= OnDeviceSystemModeChanged;
                _device.ParametersChanged -= OnDeviceParametersChanged;
                _device = null;
            }

            // TODO: honor the CancellationToken in the task's callback and wait correctly for completion
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            try { _runningTask?.Dispose(); }
            catch (InvalidOperationException) { }
        }
    }

    private void OnDeviceParametersChanged()
        => StartDeviceTreeUpdate();

    /// <summary>
    /// Marks only the current tree's child nodes offline/online while a raw-data recording is active on the
    /// device - the device itself is still reachable and its own node must stay online.
    /// </summary>
    private void OnDeviceSystemModeChanged()
    {
        if (_device!.Timeout ||
            GetIsInRelevantSystemMode(_device) is false ||
            ProcessDataOutputs.DeviceTree.Value is null)
            return;

        // only set child nodes to offline while a raw data recording is active
        SetOnlyChildrenStatus(ProcessDataOutputs.DeviceTree.Value, ToStatus(_device.Measuring));

        ProcessDataOutputs.DeviceTree.MarkAsChanged();

        static bool GetIsInRelevantSystemMode(VseDevice device)
            => device.Supervising || device.Measuring;
    }

    /// <summary>
    /// Updates the current tree's status to match the device's timeout, keeping only the child nodes offline
    /// (not the device's own node) while a raw-data recording is still active despite the timeout clearing.
    /// </summary>
    private void OnDeviceTimeoutChanged()
    {
        if (ProcessDataOutputs.DeviceTree.Value is null ||
            ProcessDataOutputs.DeviceTree.Value.Status == ToStatus(_device!.Timeout))
            return;

        ProcessDataOutputs.DeviceTree.Value.SetStatus(ToStatus(_device.Timeout));

        if (_device.Timeout is false && _device.Measuring)
            // leave child nodes marked offline while a raw data recording is active
            SetOnlyChildrenStatus(ProcessDataOutputs.DeviceTree.Value, ConnectionStatus.Offline);

        ProcessDataOutputs.DeviceTree.MarkAsChanged();
    }

    private static ConnectionStatus ToStatus(bool isOffline)
        => isOffline ? ConnectionStatus.Offline : ConnectionStatus.Online;

    private static void SetOnlyChildrenStatus(DeviceTreeVseDevice tree, ConnectionStatus status)
    {
        foreach (IDeviceTreeBase child in tree.Children)
            child.SetStatus(status);
    }

    /// <inheritdoc/>
    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled is false)
            return;

        if ((ProcessDataInputs.Trigger.Changed || cycleInfo.IsFirstCycle) &&
            ProcessDataInputs.Trigger.Value)
            StartDeviceTreeUpdate();
    }

    /// <summary>
    /// Starts a device tree parse if one isn't already running.
    /// </summary>
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
