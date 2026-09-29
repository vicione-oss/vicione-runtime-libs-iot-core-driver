using System;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.DeviceTree.Contracts;

namespace ifm.Vse;

public sealed partial class ErrorStateGuard
{
    internal const string DamageReason = "Damage";
    internal const string WarningReason = "Warning";

    private DateTime _lastTriggerTime = DateTime.MinValue;
    private string _lastTriggerReason = string.Empty;

    /// <summary>
    /// Forwards <c>ErrorState</c> if the configured delay has passed since the last forwarded state of the same
    /// kind, or immediately if it's a damage-level state following a non-damage one (a damage condition is never
    /// held back by an unrelated warning's rate limit).
    /// </summary>
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
