using ViciOne.Core.Contracts.Runtime;

namespace ifm.IOLink;

public sealed partial class BooleanSubscriber
{
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;
    protected override IRuntimeConnector<string> UnitRuntimeConnector => ProcessDataOutputs.Unit;

    protected override void SetValueToConnectors(object value, string displayFormat)
    {
        if (value is bool boolValue)
        {
            SetConnectorValue(ProcessDataOutputs.Value, boolValue);
            SetConnectorValue(ProcessDataOutputs.NumericValue, boolValue ? 1 : 0);
        }
        else if (value is string stringValue)
        {
            if (bool.TryParse(stringValue, out bool parsedBoolValue))
            {
                SetConnectorValue(ProcessDataOutputs.Value, parsedBoolValue);
                SetConnectorValue(ProcessDataOutputs.NumericValue, parsedBoolValue ? 1 : 0);
            }
        }
    }
}
