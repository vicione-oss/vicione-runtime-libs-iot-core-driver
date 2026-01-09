using ifmIoTCore.Common.Variant;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.VSE;

public sealed partial class ObjectSubscriber
{
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    private static bool IsValid(string state, string error)
    {
        bool validState = string.Equals(state, "ok", StringComparison.OrdinalIgnoreCase);
        if (validState == false)
            return false;

        if (string.IsNullOrWhiteSpace(error))
            return true;

        List<string> errorValues = [.. error.Split('|')];
        errorValues.RemoveAll(x => x.Equals(ErrorStateGuard.WarningReason, StringComparison.OrdinalIgnoreCase));
        errorValues.RemoveAll(x => x.Equals(ErrorStateGuard.DamageReason, StringComparison.OrdinalIgnoreCase));

        return errorValues.Count == 0;
    }

    protected override void SetVariantToConnectors(VariantObject variant)
    {
        double average = variant["Average"].ToObject<double>();
        double maximum = variant["Maximum"].ToObject<double>();
        double minimum = variant["Minimum"].ToObject<double>();
        double refValue = variant["RefValue"].ToObject<double>();
        double rotSpeed = variant["RotSpeed"].ToObject<double>();

        if (IsFirstValueFromDevice || (double.IsNaN(rotSpeed) == false && ProcessDataOutputs.RotSpeed.Value != rotSpeed))
        {
            SetConnectorValueIfNotEqual(ProcessDataOutputs.RotationalFrequencyTuple,
                new RotationalFrequencyTuple() { Object = Settings.NodePath.Value!, RotationalFrequency = (float)rotSpeed });
        }

        SetConnectorValue(ProcessDataOutputs.Average, average);
        SetConnectorValue(ProcessDataOutputs.Maximum, maximum);
        SetConnectorValue(ProcessDataOutputs.Minimum, minimum);
        SetConnectorValue(ProcessDataOutputs.Unit, variant["Unit"].ToObject<string>());
        SetConnectorValue(ProcessDataOutputs.RotSpeed, rotSpeed);
        SetConnectorValue(ProcessDataOutputs.RefValue, refValue);
        SetConnectorValue(ProcessDataOutputs.Warning, variant["Warning"].ToObject<double>());
        SetConnectorValue(ProcessDataOutputs.BaseLine, variant["BaseLine"].ToObject<double>());
        SetConnectorValue(ProcessDataOutputs.Damage, variant["Damage"].ToObject<double>());
        SetConnectorValue(ProcessDataOutputs.AveragingPeriod, variant["AveragingPeriod"].ToObject<double>());

        string error = variant["Error"].ToObject<string>();
        if (SetConnectorValue(ProcessDataOutputs.Error, error) &&
            string.IsNullOrWhiteSpace(error) == false)
        {
            SetConnectorValue(ProcessDataOutputs.ErrorState, new ErrorState()
            {
                Average = average,
                Maximum = maximum,
                Minimum = minimum,
                ObjectId = Settings.NodePath.Value!.Split('/').Last(),
                ObjectName = variant["Name"].ToObject<string>(),
                RefValue = refValue,
                RotSpeed = rotSpeed,
                TriggerType = error,
            });
        }

        SetConnectorValue(ProcessDataOutputs.Valid, IsValid(variant["State"].ToObject<string>(), error));
    }
}
