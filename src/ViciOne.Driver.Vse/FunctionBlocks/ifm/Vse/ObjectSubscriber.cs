using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Communication.Vse.Data;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.DeviceTree.Contracts;

namespace ifm.Vse;

public sealed partial class ObjectSubscriber
{
    /// <inheritdoc/>
    protected override IRuntimeConnector<bool> AvailableRuntimeConnector => ProcessDataOutputs.Available;

    /// <summary>
    /// Determines whether the object's current state and flags represent a fault-free reading, ignoring the
    /// warning/damage flags (which are reported separately via <see cref="ErrorState"/>/the dedicated connectors
    /// rather than making the object itself invalid).
    /// </summary>
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

    /// <inheritdoc/>
    protected override void SetItemToConnectors(DeviceObject deviceObject)
    {
        double average = deviceObject.Average;
        double maximum = deviceObject.Maximum;
        double minimum = deviceObject.Minimum;
        double refValue = deviceObject.RefValue;
        double rotSpeed = deviceObject.RotSpeed;

        if (IsFirstValueFromDevice || (double.IsNaN(rotSpeed) == false && ProcessDataOutputs.RotSpeed.Value != rotSpeed))
        {
            SetConnectorValueIfNotEqual(ProcessDataOutputs.RotationalFrequencyTuple,
                new RotationalFrequencyTuple() { Object = Settings.NodePath.Value!, RotationalFrequency = (float)rotSpeed });
        }

        SetConnectorValue(ProcessDataOutputs.Average, average);
        SetConnectorValue(ProcessDataOutputs.Maximum, maximum);
        SetConnectorValue(ProcessDataOutputs.Minimum, minimum);
        SetConnectorValue(ProcessDataOutputs.Unit, deviceObject.Unit);
        SetConnectorValue(ProcessDataOutputs.RotSpeed, rotSpeed);
        SetConnectorValue(ProcessDataOutputs.RefValue, refValue);
        SetConnectorValue(ProcessDataOutputs.Warning, deviceObject.Warning);
        SetConnectorValue(ProcessDataOutputs.BaseLine, deviceObject.BaseLine);
        SetConnectorValue(ProcessDataOutputs.Damage, deviceObject.Damage);
        SetConnectorValue(ProcessDataOutputs.AveragingPeriod, deviceObject.AveragingPeriod);

        string error = deviceObject.Flags;
        if (SetConnectorValue(ProcessDataOutputs.Error, error) &&
            string.IsNullOrWhiteSpace(error) == false)
        {
            SetConnectorValue(ProcessDataOutputs.ErrorState, new ErrorState()
            {
                Average = average,
                Maximum = maximum,
                Minimum = minimum,
                ObjectId = Settings.NodePath.Value!.Split('/').Last(),
                ObjectName = deviceObject.Name,
                RefValue = refValue,
                RotSpeed = rotSpeed,
                TriggerType = error,
            });
        }

        SetConnectorValue(ProcessDataOutputs.Valid, IsValid(deviceObject.State, error));
    }
}
