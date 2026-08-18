using ViciOne.Communication.Vse.Data;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.Vse;

public sealed partial class InputSubscriber
{
    /// <inheritdoc/>
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    /// <summary>
    /// Writes a changed input value to the device (only meaningful for an <c>External</c> input, which is the
    /// only kind that accepts writes).
    /// </summary>
    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (ProcessDataInputs.Value.Changed)
        {
            double value = ProcessDataInputs.Value.Value;
            // Only arm the write-echo suppression when the value actually changes: an unchanged write produces no
            // push-back, so arming IsWriting would leave it set and swallow the next genuine external change.
            if (SetConnectorValueIfNotEqual(ProcessDataOutputs.Value, value))
                IsWriting = true;
            Device!.Write($"{Settings.NodePath.Value}/Value", value);
        }
    }

    /// <inheritdoc/>
    protected override void SetItemToConnectors(Input input)
    {
        SetConnectorValue(ProcessDataOutputs.Value, input.Value);
        SetConnectorValue(ProcessDataOutputs.Unit, input.Unit);
    }
}
