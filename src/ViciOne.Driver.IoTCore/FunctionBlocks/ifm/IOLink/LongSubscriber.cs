using ViciOne.Core.Contracts.Runtime;

namespace ifm.IOLink;

public sealed partial class LongSubscriber
{
    /// <inheritdoc/>
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    /// <inheritdoc/>
    protected override IRuntimeConnector<string> UnitRuntimeConnector => ProcessDataOutputs.Unit;

    /// <inheritdoc/>
    protected override void SetValueToConnectors(object value, string displayFormat)
    {
        if (TryConvertToInt64(value, out long longValue))
            SetConnectorValue(ProcessDataOutputs.Value, longValue);
    }
}
