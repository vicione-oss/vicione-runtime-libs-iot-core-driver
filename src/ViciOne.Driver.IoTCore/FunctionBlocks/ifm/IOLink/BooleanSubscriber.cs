using ViciOne.Core.Contracts.Runtime;

namespace ifm.IOLink;

public sealed partial class BooleanSubscriber
{
    /// <inheritdoc/>
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    /// <inheritdoc/>
    protected override IRuntimeConnector<string> UnitRuntimeConnector => ProcessDataOutputs.Unit;

    /// <inheritdoc/>
    protected override void SetValueToConnectors(object value, string displayFormat)
    {
        if (TryConvertToBoolean(value, out bool boolValue))
        {
            SetConnectorValue(ProcessDataOutputs.Value, boolValue);
            SetConnectorValue(ProcessDataOutputs.NumericValue, boolValue ? 1 : 0);
        }
    }
}
