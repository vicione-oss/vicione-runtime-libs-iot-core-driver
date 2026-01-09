using ViciOne.Core.Contracts.Runtime;

namespace ifm.VSE;

public sealed partial class RpmAtMinMaxTracker
{
    private double _currentMaxValue = double.MinValue;
    private double _currentMinValue = double.MaxValue;
    private bool _firstAssignment = true;
    private double _refValueAtMax = double.MinValue;
    private double _refValueAtMin = double.MaxValue;
    private double _rotSpeedAtMax = double.MinValue;
    private double _rotSpeedAtMin = double.MaxValue;

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        double value = ProcessDataInputs.Value.Value;
        double refValue = ProcessDataInputs.RefValue.Value;
        double rotSpeed = ProcessDataInputs.RotSpeed.Value;
        bool writeOutputs =
            ProcessDataInputs.Average.Changed ||
            ProcessDataInputs.Difference.Changed ||
            ProcessDataInputs.Maximum.Changed ||
            ProcessDataInputs.Minimum.Changed ||
            ProcessDataInputs.Sum.Changed;

        if (writeOutputs)
        {
            SetOutputValue(ProcessDataOutputs.Average, ProcessDataInputs.Average.Value);
            SetOutputValue(ProcessDataOutputs.Difference, ProcessDataInputs.Difference.Value);
            SetOutputValue(ProcessDataOutputs.Maximum, ProcessDataInputs.Maximum.Value);
            SetOutputValue(ProcessDataOutputs.Minimum, ProcessDataInputs.Minimum.Value);
            SetOutputValue(ProcessDataOutputs.Sum, ProcessDataInputs.Sum.Value);
            SetOutputValue(ProcessDataOutputs.RotSpeedAtMaximum, _rotSpeedAtMax);
            SetOutputValue(ProcessDataOutputs.RotSpeedAtMinimum, _rotSpeedAtMin);
            SetOutputValue(ProcessDataOutputs.RefValueAtMaximum, _refValueAtMax);
            SetOutputValue(ProcessDataOutputs.RefValueAtMinimum, _refValueAtMin);
            SetInitialValues(value, rotSpeed, refValue);
        }
        else if (ProcessDataInputs.Value.Changed)
        {
            if (_firstAssignment)
            {
                SetInitialValues(value, rotSpeed, refValue);
                _firstAssignment = false;
            }
            else if (value < _currentMinValue)
            {
                _currentMinValue = value;
                _rotSpeedAtMin = rotSpeed;
                _refValueAtMin = refValue;
            }
            else if (value > _currentMaxValue)
            {
                _currentMaxValue = value;
                _rotSpeedAtMax = rotSpeed;
                _refValueAtMax = refValue;
            }
        }
    }

    private void SetInitialValues(double value, double rotSpeed, double refValue)
    {
        _currentMaxValue = _currentMinValue = value;
        _rotSpeedAtMax = _rotSpeedAtMin = rotSpeed;
        _refValueAtMax = _refValueAtMin = refValue;
    }

    private static void SetOutputValue<T>(IRuntimeConnector<T> output, T value)
    {
        SetConnectorValueIfNotEqual(output, value);
        output.MarkAsChanged();
    }
}
