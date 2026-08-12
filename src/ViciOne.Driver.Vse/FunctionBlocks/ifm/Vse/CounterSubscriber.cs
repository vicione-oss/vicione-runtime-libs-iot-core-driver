using ViciOne.Communication.Vse.Data;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.Vse;

public sealed partial class CounterSubscriber
{
    /// <inheritdoc/>
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    /// <inheritdoc/>
    protected override void SetItemToConnectors(Counter counter)
    {
        SetConnectorValue(ProcessDataOutputs.Value, counter.Value);
        SetConnectorValue(ProcessDataOutputs.Unit, counter.Unit);
        SetConnectorValue(ProcessDataOutputs.Limit, counter.Limit);
        SetConnectorValue(ProcessDataOutputs.State, counter.State);
    }
}
