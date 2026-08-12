using ViciOne.Communication.Vse.Data;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.Vse;

public sealed partial class VariantsSubscriber
{
    /// <inheritdoc/>
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    /// <summary>
    /// Writes a changed active-variant selection to the device.
    /// </summary>
    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (ProcessDataInputs.ActiveVariant.Changed)
        {
            IsWriting = true;
            long value = ProcessDataInputs.ActiveVariant.Value;
            SetConnectorValueIfNotEqual(ProcessDataOutputs.ActiveVariant, value);
            Device!.Write($"{Settings.NodePath.Value}/ActiveVariant", value);
        }
    }

    /// <inheritdoc/>
    protected override void SetItemToConnectors(Variants variants)
        => SetConnectorValue(ProcessDataOutputs.ActiveVariant, variants.ActiveVariantId);
}
