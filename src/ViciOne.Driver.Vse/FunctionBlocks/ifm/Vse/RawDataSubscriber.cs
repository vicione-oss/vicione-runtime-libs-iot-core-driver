using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.Vse;

namespace ifm.Vse;

public sealed partial class RawDataSubscriber
{
    private readonly List<ErrorState> _errorStates = new();
    private string _recordingIdentifier = string.Empty;
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Released by the DeviceManager.")]
    private VseDevice? _vseDevice;
    private bool _waitingForData;

    /// <summary>
    /// Adds an error state to the set reported as <see cref="Measurement.RecordReasons"/> on the next recording,
    /// keeping at most one damage-level state and one warning-level state per object: a new damage state
    /// replaces any existing state for the same object, while a warning state is only added if the object has
    /// no existing state yet (a warning must not downgrade an already-recorded damage state for that object).
    /// </summary>
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

    /// <inheritdoc/>
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
        => new VseRawDataIdentifier(
            Settings.Address.Value!,
            Settings.SensorIndex.Value,
            Settings.Duration.Value,
            Settings.SampleRate.Value).ToString();

    /// <inheritdoc/>
    public override void InitializeI()
    {
        base.InitializeI();

        if (Driver is not VseDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(VseDriver)}.");

        if (Settings.Address.Value is null)
            throw new ArgumentException("Invalid address configuration.");

        _recordingIdentifier = GetRecordingIdentifier();

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
            Identifier = measurement.Identifier,
            RawData = measurement.RawData,
            RecordReasons = _errorStates,
            Start = measurement.Start,
            Values = measurement.Values,
        });

        _errorStates.Clear();
    }

    /// <inheritdoc/>
    protected override void OnSettingChanged(IRuntimeSetting setting)
        => _recordingIdentifier = GetRecordingIdentifier();

    /// <inheritdoc/>
    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (_waitingForData is false &&
            (ProcessDataInputs.ObjectTrigger.Changed || cycleInfo.IsFirstCycle) &&
            ProcessDataInputs.ObjectTrigger.Value is not null)
        {
            foreach (ErrorState errorState in ProcessDataInputs.ObjectTrigger.Value)
                AddErrorState(errorState);

            if (ProcessDataInputs.ObjectTrigger.Value.Length > 0)
            {
                _waitingForData = true;
                _vseDevice!.GetRecordingRawData(_recordingIdentifier!, Settings.SensorIndex.Value, Settings.SampleRate.Value,
                    Settings.Duration.Value, Settings.IgnoreTimedValueArray.Value);
            }
        }
        else if (_waitingForData is false &&
            (ProcessDataInputs.Trigger.Changed || cycleInfo.IsFirstCycle) &&
            ProcessDataInputs.Trigger.Value)
        {
            _waitingForData = true;
            AddErrorState(new ErrorState()
            {
                TriggerType = "Interval",
            });
            _vseDevice!.GetRecordingRawData(_recordingIdentifier!, Settings.SensorIndex.Value, Settings.SampleRate.Value,
                Settings.Duration.Value, Settings.IgnoreTimedValueArray.Value);
        }

        // TODO: may need to clear the raw data from the connectors and links again here for memory reasons
        // conOutputData.ResetValueReference();
    }
}
