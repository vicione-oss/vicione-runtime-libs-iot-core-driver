using System;
using System.Globalization;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Interfaces;

namespace ifm.IOLink;

/// <summary>
/// Base class for FunctionBlocks that subscribe to one IO-Link process-data-in value, handling device
/// registration, availability tracking, and dispatching decoded values to the derived block's connectors.
/// </summary>
public abstract class SubscriberBase<TBlock> : RuntimeFunctionBlock<TBlock>, IIoLinkSubscriberFunctionBlock
    where TBlock : RuntimeFunctionBlock<TBlock>, new()
{
    private object _lastValue = new();

    /// <summary>
    /// The connector to report the subscribed device's availability on.
    /// </summary>
    protected abstract IRuntimeConnector<bool> AvailableRuntimeConnector { get; }

    /// <summary>
    /// The IO-Link master this block is subscribed through, once initialized.
    /// </summary>
    protected IoLinkMaster? Device { get; private set; }

    /// <summary>
    /// Whether no value has been received from the device yet since (re-)initialization.
    /// </summary>
    protected bool IsFirstValueFromDevice { get; private set; } = true;

    /// <summary>
    /// This block's IO-Link subscription settings.
    /// </summary>
    public new abstract IIoLinkSubscriberSettings Settings { get; }

    /// <summary>
    /// The connector to report the subscribed value's unit on.
    /// </summary>
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

    /// <summary>
    /// Acquires the configured IO-Link master and registers this block's process-data-in subscription.
    /// </summary>
    public override void InitializeII()
    {
        base.InitializeII();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (string.IsNullOrWhiteSpace(Settings.Identifier.Value))
            throw new ArgumentException("Invalid identifier configuration.");

        // This is in InitializeII so the IoTCoreConfiguration for this identifier has run its InitializeI first,
        // creating (and authenticating) the master this subscriber now reads through.
        Device = driver.GetIoLinkMaster(Settings.Identifier.Value);
        Device.ChildDevicesChanged += OnDeviceChildDevicesChanged;
        Device.TimeoutChanged += OnDeviceTimeoutChanged;
        Device.RegisterProcessDataIn(Settings.PortIndex.Value, Settings.VendorId.Value, Settings.DeviceId.Value, Settings.ProcessDataInIndex.Value, this);
    }

    private void OnDeviceChildDevicesChanged()
        => SetAvailable(DetermineAvailability());

    private void OnDeviceTimeoutChanged()
        => SetAvailable(DetermineAvailability());

    private void SetAvailable(bool available)
        => SetConnectorValue(AvailableRuntimeConnector, available);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "The derived blocks do not need to call the method.")]
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1033:Interface methods should be callable by child types", Justification = "The derived blocks do not need to call the method.")]
    void IIoLinkSubscriberFunctionBlock.SetUnit(string unit)
        => SetConnectorValue(UnitRuntimeConnector, unit);

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
    /// Dispatches a newly received, decoded process-data-in value to the derived block's connectors.
    /// </summary>
    protected abstract void SetValueToConnectors(object value, string displayFormat);

    /// <summary>
    /// Converts a decoded process-data value (<see cref="IoddParser.Helpers.DisplayValue.Value"/>, which is
    /// untyped) to <see cref="double"/>.
    /// <para>Accepts every numeric CLR type IODD decoding can produce, converting straight to <see cref="decimal"/>
    /// and back rather than via <see cref="double"/> - a <c>float</c> source (IODD <c>Float32T</c>) converted
    /// directly to <see cref="double"/> keeps that value's binary rounding noise (e.g. 0.1f becomes
    /// 0.100000001490116119384765625), which round-tripping through <see cref="decimal"/> first avoids. Also
    /// accepts a numeric string, in case a value ever arrives already formatted as text.</para>
    /// </summary>
    protected static bool TryConvertToDouble(object value, out double result)
    {
        switch (value)
        {
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                result = (double)Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return true;
            case string stringValue:
                return double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            default:
                result = default;
                return false;
        }
    }

    /// <summary>
    /// Converts a decoded process-data value (<see cref="IoddParser.Helpers.DisplayValue.Value"/>, which is
    /// untyped) to <see cref="long"/>.
    /// <para>Accepts every integral CLR type IODD decoding can produce, plus a numeric string in case a value
    /// ever arrives already formatted as text.</para>
    /// </summary>
    protected static bool TryConvertToInt64(object value, out long result)
    {
        switch (value)
        {
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                result = Convert.ToInt64(value, CultureInfo.InvariantCulture);
                return true;
            case string stringValue:
                return long.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
            default:
                result = default;
                return false;
        }
    }

    /// <summary>
    /// Converts a decoded process-data value (<see cref="IoddParser.Helpers.DisplayValue.Value"/>, which is
    /// untyped) to <see cref="bool"/>, also accepting "True"/"False" in case a value ever arrives already
    /// formatted as text.
    /// </summary>
    protected static bool TryConvertToBoolean(object value, out bool result)
    {
        switch (value)
        {
            case bool boolValue:
                result = boolValue;
                return true;
            case string stringValue:
                return bool.TryParse(stringValue, out result);
            default:
                result = default;
                return false;
        }
    }
}
