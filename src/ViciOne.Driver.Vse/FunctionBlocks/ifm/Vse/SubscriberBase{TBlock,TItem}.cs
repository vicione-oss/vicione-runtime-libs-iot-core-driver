using System;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Driver.Vse;
using ViciOne.Driver.Vse.Interfaces;

namespace ifm.Vse;

/// <summary>
/// Base class for FunctionBlocks that subscribe to one VSE node path, handling device registration, availability
/// tracking, and dispatching newly received values to the derived block's connectors.
/// </summary>
public abstract class SubscriberBase<TBlock, TItem> : RuntimeFunctionBlock<TBlock>, IVseSubscriberFunctionBlock
    where TBlock : RuntimeFunctionBlock<TBlock>, new()
{
    /// <summary>
    /// The connector to report the subscribed device's availability on.
    /// </summary>
    protected abstract IRuntimeConnector<bool> AvailableRuntimeConnector { get; }

    /// <summary>
    /// Whether no value has been received from the device yet since (re-)initialization.
    /// </summary>
    protected bool IsFirstValueFromDevice { get; private set; } = true;

    /// <summary>
    /// Set before writing a value to the device, so the resulting SDK push-back is recognized as an echo of this
    /// block's own write and does not get processed as a new externally received value.
    /// </summary>
    protected bool IsWriting { get; set; }

    /// <summary>
    /// This block's VSE subscription settings.
    /// </summary>
    public new abstract IVseSubscriberSettings Settings { get; }

    /// <summary>
    /// The VSE device this block is subscribed through, once initialized.
    /// </summary>
    protected VseDevice? Device { get; private set; }

    private bool DetermineAvailability()
        => Device is not null &&
            Device.Timeout is false &&
            Device.Supervising &&
            Device.GetIsServiceElementExisting(Settings.NodePath.Value!);

    /// <inheritdoc/>
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

    /// <summary>
    /// Acquires the configured VSE device and registers this block's node path subscription.
    /// </summary>
    public override void InitializeI()
    {
        base.InitializeI();

        if (Driver is not VseDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(VseDriver)}.");

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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Schnittstellenmethoden müssen von untergeordneten Typen aufgerufen werden können", Justification = "The derived blocks do not need to call the method.")]
    void IVseSubscriberFunctionBlock.SetValue(object item)
    {
        if (IsWriting)
        {
            IsWriting = false;
            return;
        }

        if (FunctionBlockEnabled)
        {
            SetAvailable(true);

            // No need to dedupe against the previous push here (unlike the old polling model, which re-sent the
            // current value every tick regardless of change): the VSE-SDK-facing side only fires a push when a
            // watched value has actually changed.
            SetItemToConnectors((TItem)item);
            IsFirstValueFromDevice = false;
        }
    }

    /// <summary>
    /// Sets a connector's value, forcing it to be reported as changed on the very first value received from the
    /// device even if it happens to match the connector's existing value.
    /// </summary>
    protected bool SetConnectorValue<T>(IRuntimeConnector<T> connector, T newValue)
    {
        bool result = SetConnectorValueIfNotEqual(connector, newValue);
        if (IsFirstValueFromDevice)
            connector.MarkAsChanged();

        return result;
    }

    /// <summary>
    /// Dispatches a newly received value to the derived block's connectors.
    /// </summary>
    protected abstract void SetItemToConnectors(TItem item);
}
