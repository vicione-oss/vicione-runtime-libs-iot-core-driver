using ifmIoTCore.Common.Variant;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.VSE;

public sealed partial class CounterSubscriber
{
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    protected override void SetVariantToConnectors(VariantObject variant)
    {
        SetConnectorValue(ProcessDataOutputs.Value, variant["Value"].ToObject<double>());
        SetConnectorValue(ProcessDataOutputs.Unit, variant["Unit"].ToObject<string>());
        SetConnectorValue(ProcessDataOutputs.Limit, variant["Limit"].ToObject<double>());
        SetConnectorValue(ProcessDataOutputs.State, variant["State"].ToObject<string>());
    }
}
