using ViciOne.Core.Contracts.Runtime;

namespace ifm.IOLink;

public sealed partial class StringSubscriber
{
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;
    protected override IRuntimeConnector<string> UnitRuntimeConnector => ProcessDataOutputs.Unit;

    protected override void SetValueToConnectors(object value, string displayFormat)
    {
        if (value is not string stringValue)
            return;

        SetConnectorValue(ProcessDataOutputs.Value, stringValue);
    }
}
