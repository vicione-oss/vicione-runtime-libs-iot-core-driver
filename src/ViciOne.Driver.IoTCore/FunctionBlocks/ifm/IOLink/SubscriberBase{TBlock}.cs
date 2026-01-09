using System;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.IOLink;

public abstract class SubscriberBase<TBlock> : RuntimeFunctionBlock<TBlock>, IIoLinkSubscriberFunctionBlock
    where TBlock : RuntimeFunctionBlock<TBlock>, new()
{
    private object _lastValue = new();

    protected abstract IRuntimeConnector<bool> AvailableRuntimeConnector { get; }
    protected IoLinkMaster? Device { get; private set; }
    protected bool IsFirstValueFromDevice { get; private set; } = true;
    public new abstract IIoLinkSubscriberSettings Settings { get; }
    protected abstract IRuntimeConnector<string> UnitRuntimeConnector { get; }

    private bool DetermineAvailability()
    {
        if (Device is null || Device.Timeout)
            return false;
        IoLinkMasterPortDeviceAvailabilityInfo? info = Device.GetPortDeviceAvailabilityInfo(Settings.PortIndex.Value);
        return
            info is not null &&
            info.Available &&
            info.VendorId == Settings.VendorId.Value &&
            info.DeviceId == Settings.DeviceId.Value;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing == false)
            return;

        if (Device is not null)
        {
            Device.UnregisterProcessDataIn(this);
            Device.TimeoutChanged -= OnDeviceTimeoutChanged;
            Device.ChildDevicesChanged -= OnDeviceChildDevicesChanged;
            Device = null;
        }
    }

    public override void InitializeII()
    {
        base.InitializeII();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (Settings.Address.Value is null)
            throw new ArgumentException("Invalid address configuration.");

        if (Settings.ProductName.Value is null)
            throw new ArgumentException("Invalid product name configuration.");

        // Liegt im InitializeII damit der DeviceTreeSubscriber zuerst initialisiert wird und das Gerät mit dem konfigurierten IoddDirectory anlegt
        Device = driver.GetOrAddIoLinkMaster(Settings.Address.Value);
        Device.ChildDevicesChanged += OnDeviceTimeoutChanged;
        Device.TimeoutChanged += OnDeviceChildDevicesChanged;
        Device.RegisterProcessDataIn(Settings.PortIndex.Value, Settings.VendorId.Value, Settings.DeviceId.Value, Settings.ProcessDataInIndex.Value, this);
    }

    private void OnDeviceChildDevicesChanged()
        => SetAvailable(DetermineAvailability());

    private void OnDeviceTimeoutChanged()
        => SetAvailable(DetermineAvailability());

    private void SetAvailable(bool available)
        => SetConnectorValue(AvailableRuntimeConnector, available);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Schnittstellenmethoden müssen von untergeordneten Typen aufgerufen werden können", Justification = "Die abgeleiteten Blöcke müssen die Methode nicht aufrufen.")]
    void IIoLinkSubscriberFunctionBlock.SetValue(object value, string displayFormat)
    {
        if (FunctionBlockEnabled)
        {
            SetAvailable(true);

            if (Equals(_lastValue, value))
                return;

            _lastValue = value;
            SetValueToConnectors(_lastValue, displayFormat);
            IsFirstValueFromDevice = false;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Schnittstellenmethoden müssen von untergeordneten Typen aufgerufen werden können", Justification = "Die abgeleiteten Blöcke müssen die Methode nicht aufrufen.")]
    void IIoLinkSubscriberFunctionBlock.SetUnit(string unit)
        => SetConnectorValue(UnitRuntimeConnector, unit);

    protected bool SetConnectorValue<T>(IRuntimeConnector<T> connector, T newValue)
    {
        bool result = SetConnectorValueIfNotEqual(connector, newValue);
        if (IsFirstValueFromDevice)
            connector.MarkAsChanged();

        return result;
    }

    protected abstract void SetValueToConnectors(object value, string displayFormat);
}
