using ifmIoTCore.Common.Variant;
using ViciOne.Core.Contracts.Runtime;

namespace ifm.VSE;

public sealed partial class InputSubscriber
{
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (cycleInfo.IsFirstCycle == false && ProcessDataInputs.Value.Changed)
        {
            IsWriting = true;
            double value = ProcessDataInputs.Value.Value;
            SetConnectorValueIfNotEqual(ProcessDataOutputs.Value, value);
            Device!.Write($"{Settings.NodePath.Value}/Value", value);
        }
    }

    protected override void SetVariantToConnectors(VariantObject variant)
    {
        SetConnectorValue(ProcessDataOutputs.Value, variant["Value"].ToObject<double>());
        SetConnectorValue(ProcessDataOutputs.Unit, variant["Unit"].ToObject<string>());
    }
}
