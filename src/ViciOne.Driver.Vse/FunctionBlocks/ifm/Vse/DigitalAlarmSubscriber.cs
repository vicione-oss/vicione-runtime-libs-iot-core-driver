using ViciOne.Communication.Vse.Data;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.Vse;

public sealed partial class DigitalAlarmSubscriber
{
    /// <inheritdoc/>
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    /// <inheritdoc/>
    protected override void SetItemToConnectors(DigitalAlarm alarm)
    {
        SetConnectorValue(ProcessDataOutputs.Value, alarm.Value);
        SetConnectorValue(ProcessDataOutputs.NumericValue, alarm.Value ? 1 : 0);
    }
}
