using System;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Interfaces;

namespace ifm.IOLink;

public sealed partial class IoLinkMasterDiagnosticSubscriber : IIoLinkMasterDiagnosticSubscriber
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Released by the DeviceManager.")]
    private IoLinkMaster? _device;
    private bool _isFirstDispatch = true;

    /// <summary>
    /// Acquires the configured IO-Link master and registers this block for its diagnostic values.
    /// </summary>
    public override void InitializeII()
    {
        base.InitializeII();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (string.IsNullOrWhiteSpace(Settings.Identifier.Value))
            throw new ArgumentException("Invalid identifier configuration.");

        // This is in InitializeII so the IoTCoreConfiguration for this identifier has run its InitializeI first,
        // creating (and authenticating) the master this block now reads through.
        _device = driver.GetIoLinkMaster(Settings.Identifier.Value);
        _device.TimeoutChanged += OnDeviceTimeoutChanged;
        _device.RegisterMasterDiagnostics(this);
        SetConnectorValueIfNotEqual(ProcessDataOutputs.Available, _device.Timeout is false);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing is false)
            return;

        if (_device is not null)
        {
            _device.UnregisterMasterDiagnostics(this);
            _device.TimeoutChanged -= OnDeviceTimeoutChanged;
            _device = null;
        }
    }

    private void OnDeviceTimeoutChanged()
        => SetConnectorValueIfNotEqual(ProcessDataOutputs.Available, _device is { Timeout: false });

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "The block does not need to call the method itself.")]
    void IIoLinkMasterDiagnosticSubscriber.SetDiagnostics(MasterDiagnostics diagnostics)
    {
        if (FunctionBlockEnabled is false)
            return;

        SetValue(ProcessDataOutputs.Available, true);

        if (diagnostics.Temperature is { } temperature)
            SetValue(ProcessDataOutputs.Temperature, temperature);
        SetValue(ProcessDataOutputs.TemperatureUnit, diagnostics.TemperatureUnit);

        if (diagnostics.SupplyVoltage is { } supplyVoltage)
            SetValue(ProcessDataOutputs.SupplyVoltage, supplyVoltage);
        SetValue(ProcessDataOutputs.SupplyVoltageUnit, diagnostics.SupplyVoltageUnit);

        if (diagnostics.Current is { } current)
            SetValue(ProcessDataOutputs.Current, current);
        SetValue(ProcessDataOutputs.CurrentUnit, diagnostics.CurrentUnit);

        if (diagnostics.SupervisionStatus is { } supervisionStatus)
            SetValue(ProcessDataOutputs.SupervisionStatus, supervisionStatus);

        _isFirstDispatch = false;
    }

    /// <summary>
    /// Publishes a connector's value, forcing it to be reported as changed on the very first dispatch even when
    /// it already equals the connector's default (e.g. a supervision status of 0) - otherwise that value would
    /// never raise an event and so would show up in the live view without a timestamp.
    /// </summary>
    private void SetValue<T>(IRuntimeConnector<T> connector, T value)
    {
        SetConnectorValueIfNotEqual(connector, value);
        if (_isFirstDispatch)
            connector.MarkAsChanged();
    }
}
