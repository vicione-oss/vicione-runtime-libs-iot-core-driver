using ifmIoTCore.Common.Variant;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.VSE;

public sealed partial class AlarmSubscriber
{
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    protected override void SetVariantToConnectors(VariantObject variant)
    {
        bool value = variant["Value"].ToObject<bool>();
        SetConnectorValue(ProcessDataOutputs.Value, value);
        SetConnectorValue(ProcessDataOutputs.NumericValue, value ? 1 : 0);
    }
}
