using System;
using ifmIoTCore.Common.Variant;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.VSE;

public abstract class SubscriberBase<TBlock> : RuntimeFunctionBlock<TBlock>, IVseSubscriberFunctionBlock
    where TBlock : RuntimeFunctionBlock<TBlock>, new()
{
    private VariantObject? _pendingVariant;

    protected abstract IRuntimeConnector<bool> AvailableRuntimeConnector { get; }
    protected bool IsFirstValueFromDevice { get; private set; } = true;
    protected bool IsWriting { get; set; }
    public new abstract IVseSubscriberSettings Settings { get; }
    protected VseDevice? Device { get; private set; }

    private bool DetermineAvailability()
        => Device is not null &&
            Device.Timeout is false &&
            Device.Supervising &&
            Device.GetIsServiceElementExisting(Settings.NodePath.Value!);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing == false)
            return;

        if (Device is not null)
        {
            Device.UnregisterAddress(this);
            Device.TimeoutChanged -= OnDeviceTimeoutChanged;
            Device.SystemModeChanged -= OnDeviceSystemModeChanged;
            Device.ParametersChanged -= OnDeviceParametersChanged;
            Device = null;
        }
    }

    public override void InitializeI()
    {
        base.InitializeI();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (Settings.Address.Value is null)
            throw new ArgumentException("Invalid address configuration.");

        if (Settings.NodePath.Value is null)
            throw new ArgumentException("Invalid node path configuration.");

        Device = driver.GetOrAddVseDevice(Settings.Address.Value!);
        Device.TimeoutChanged += OnDeviceTimeoutChanged;
        Device.SystemModeChanged += OnDeviceSystemModeChanged;
        Device.ParametersChanged += OnDeviceParametersChanged;
        Device.RegisterAddress(Settings.NodePath.Value, this);
    }

    private void OnDeviceParametersChanged()
        => SetAvailable(DetermineAvailability());

    private void OnDeviceSystemModeChanged()
        => SetAvailable(DetermineAvailability());

    private void OnDeviceTimeoutChanged()
        => SetAvailable(DetermineAvailability());

    private void SetAvailable(bool available)
        => SetConnectorValue(AvailableRuntimeConnector, available);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Schnittstellenmethoden müssen von untergeordneten Typen aufgerufen werden können", Justification = "Die abgeleiteten Blöcke müssen die Methode nicht aufrufen.")]
    void IVseSubscriberFunctionBlock.SetValue(VariantObject variant)
    {
        if (IsWriting)
        {
            IsWriting = false;
            return;
        }

        if (FunctionBlockEnabled)
        {
            SetAvailable(true);

            if (Equals(_pendingVariant, variant))
                return;

            _pendingVariant = variant;
            SetVariantToConnectors(_pendingVariant);
            IsFirstValueFromDevice = false;
        }
    }

    protected bool SetConnectorValue<T>(IRuntimeConnector<T> connector, T newValue)
    {
        bool result = SetConnectorValueIfNotEqual(connector, newValue);
        if (IsFirstValueFromDevice)
            connector.MarkAsChanged();

        return result;
    }

    protected abstract void SetVariantToConnectors(VariantObject variant);
}
