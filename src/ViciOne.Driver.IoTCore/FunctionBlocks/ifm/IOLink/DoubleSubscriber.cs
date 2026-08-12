using ViciOne.Core.Contracts.Runtime;

namespace ifm.IOLink;

public sealed partial class DoubleSubscriber
{
    /// <inheritdoc/>
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    /// <inheritdoc/>
    protected override IRuntimeConnector<string> UnitRuntimeConnector => ProcessDataOutputs.Unit;

    /// <inheritdoc/>
    protected override void SetValueToConnectors(object value, string displayFormat)
    {
        if (TryConvertToDouble(value, out double doubleValue))
            SetConnectorValue(ProcessDataOutputs.Value, doubleValue);
    }
}
