using ifmIoTCore.Common.Variant;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.VSE;

public sealed partial class VariantsSubscriber
{
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (cycleInfo.IsFirstCycle == false && ProcessDataInputs.ActiveVariant.Changed)
        {
            IsWriting = true;
            long value = ProcessDataInputs.ActiveVariant.Value;
            SetConnectorValueIfNotEqual(ProcessDataOutputs.ActiveVariant, value);
            Device!.Write($"{Settings.NodePath.Value}/ActiveVariant", value);
        }
    }

    protected override void SetVariantToConnectors(VariantObject variant)
        => SetConnectorValue(ProcessDataOutputs.ActiveVariant, variant["ActiveVariant"].ToObject<long>());
}
