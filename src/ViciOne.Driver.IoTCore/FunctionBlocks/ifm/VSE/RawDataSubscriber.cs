using ACX.ViciOne.Data.Driver.CloudDataAdapter.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;
using ViciOne.Driver.IoTCore.Contracts.Constants;

namespace ifm.VSE;

public sealed partial class RawDataSubscriber
{
    private readonly List<ErrorState> _errorStates = [];
    private string? _recordingIdentifier;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Verwerfbare Felder verwerfen", Justification = "Wird vom DeviceManager freigegeben.")]
    private VseDevice? _vseDevice;
    private bool _waitingForData;

    private void AddErrorState(ErrorState errorState)
    {
        if (string.IsNullOrEmpty(errorState.ObjectId))
        {
            _errorStates.Add(errorState);
            return;
        }

        IEnumerable<ErrorState> existingErrorStates = [.. _errorStates.Where(e => e.ObjectId == errorState.ObjectId)];

        if (errorState.TriggerType.Contains(ErrorStateGuard.DamageReason, StringComparison.Ordinal))
        {
            foreach (ErrorState existingErrorState in existingErrorStates)
            {
                _errorStates.Remove(existingErrorState);
            }

            _errorStates.Add(errorState);
        }
        else if (errorState.TriggerType.Contains(ErrorStateGuard.WarningReason, StringComparison.Ordinal))
        {
            if (!existingErrorStates.Any())
            {
                _errorStates.Add(errorState);
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && _vseDevice is not null)
        {
            _vseDevice.RawDataReceived -= OnRawDataReceived;
            _vseDevice = null;
        }
    }

    private string GetRecordingIdentifier()
        => Identifiers.GetVseRawDataIdentifier(Settings.Address.Value!,
            Settings.SensorIndex.Value,
            Settings.Duration.Value,
            Settings.SamplingRate.Value);

    public override void InitializeI()
    {
        base.InitializeI();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (Settings.Address.Value is null)
            throw new ArgumentException("Invalid address configuration.");

        _vseDevice = driver.GetOrAddVseDevice(Settings.Address.Value!);
        _vseDevice.RawDataReceived += OnRawDataReceived;
    }

    private void OnRawDataReceived(Measurement measurement)
    {
        if (_waitingForData == false || string.Equals(measurement.Identifier, _recordingIdentifier, StringComparison.Ordinal) == false)
            return;

        _waitingForData = false;

        SetConnectorValueIfNotEqual(ProcessDataOutputs.Data, new Measurement()
        {
            Duration = measurement.Duration,
            Frequency = measurement.Frequency,
            Identifier = Identifiers.GetVseRawDataIdentifier(Settings.Address.Value!,
                            Settings.SensorIndex.Value,
                            (int)measurement.Duration,
                            (int)measurement.Frequency),
            IdentifierType = measurement.IdentifierType,
            RawData = measurement.RawData,
            RecordReasons = _errorStates,
            Start = measurement.Start,
            Values = measurement.Values,
        });

        _errorStates.Clear();
    }

    protected override void OnSettingChanged(IRuntimeSetting setting)
        => _recordingIdentifier = GetRecordingIdentifier();

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (_waitingForData is false && ProcessDataInputs.ObjectTrigger.Changed && ProcessDataInputs.ObjectTrigger.Value is not null)
        {
            foreach (ErrorState errorState in ProcessDataInputs.ObjectTrigger.Value)
                AddErrorState(errorState);

            if (ProcessDataInputs.ObjectTrigger.Value.Length > 0)
            {
                _waitingForData = true;
                _vseDevice!.GetRecordingRawData(_recordingIdentifier!, Settings.SensorIndex.Value, Settings.SamplingRate.Value,
                    Settings.Duration.Value, Settings.IgnoreTimedValueArray.Value);
            }
        }
        else if (_waitingForData is false && ProcessDataInputs.Trigger.Changed && ProcessDataInputs.Trigger.Value)
        {
            _waitingForData = true;
            AddErrorState(new ErrorState()
            {
                TriggerType = "Interval",
            });
            _vseDevice!.GetRecordingRawData(_recordingIdentifier!, Settings.SensorIndex.Value, Settings.SamplingRate.Value,
                Settings.Duration.Value, Settings.IgnoreTimedValueArray.Value);
        }

        // TODO: eventuell muss hier wieder die Rohdaten von den Konnektoren und Links aus Speichergründen gelöscht werden
        // conOutputData.ResetValueReference();
    }
}
