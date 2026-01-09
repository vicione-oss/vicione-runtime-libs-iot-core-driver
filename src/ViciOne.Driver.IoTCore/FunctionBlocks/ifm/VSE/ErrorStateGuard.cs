using System;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.VSE;

public sealed partial class ErrorStateGuard
{
    internal const string DamageReason = "Damage";
    internal const string WarningReason = "Warning";

    private DateTime _lastTriggerTime = DateTime.MinValue;
    private string _lastTriggerReason = string.Empty;

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        double passedHours = (DateTime.UtcNow - _lastTriggerTime).TotalHours;

        ErrorState? state = ProcessDataInputs.ErrorState.Value;
        if (state is null)
            return;

        if (((state.TriggerType.Contains(DamageReason, StringComparison.Ordinal) && Settings.OnDamage.Value) ||
             (state.TriggerType.Contains(WarningReason, StringComparison.Ordinal) && Settings.OnWarning.Value))
            && passedHours > Settings.Delay.Value)
        {
            SetConnectorValueIfNotEqual(ProcessDataOutputs.ErrorState, state);
            _lastTriggerReason = state.TriggerType;
            _lastTriggerTime = DateTime.UtcNow;
        }
        else if (state.TriggerType.Contains(DamageReason, StringComparison.Ordinal) && Settings.OnDamage.Value && !_lastTriggerReason.Contains(DamageReason, StringComparison.Ordinal))
        {
            SetConnectorValueIfNotEqual(ProcessDataOutputs.ErrorState, state);
            _lastTriggerReason = state.TriggerType;
            _lastTriggerTime = DateTime.UtcNow;
        }
    }
}
