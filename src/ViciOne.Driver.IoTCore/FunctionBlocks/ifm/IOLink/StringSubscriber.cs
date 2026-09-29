using ViciOne.Core.Contracts.Runtime;

namespace ifm.IOLink;

public sealed partial class StringSubscriber
{
    /// <inheritdoc/>
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    /// <inheritdoc/>
    protected override IRuntimeConnector<string> UnitRuntimeConnector => ProcessDataOutputs.Unit;

    /// <inheritdoc/>
    protected override void SetValueToConnectors(object value, string displayFormat)
    {
        if (value is not string stringValue)
            return;

        SetConnectorValue(ProcessDataOutputs.Value, stringValue);
    }
}
