using System;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.IoTCore;

namespace ifm.IOLink;

public sealed partial class BlobSubscriber
{
    private const string Unknown_ProductName = "unknown";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Released by the DeviceManager.")]
    private IoLinkMaster? _device;
    private string? _recordingIdentifier;
    private bool _waitingForData;

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && _device is not null)
        {
            _device.BlobDataReceived -= OnBlobDataReceived;
            _device = null;
        }
    }

    private string GetRecordingIdentifier()
        => new IoLinkRawDataIdentifier(
            _device?.Address ?? string.Empty,
            Settings.PortIndex.Value,
            Settings.VendorId.Value,
            Settings.DeviceId.Value,
            Settings.ProductName.Value ?? Unknown_ProductName,
            "Recording",
            DeviceTreeBlobData.ReadBlobDuration,
            DeviceTreeBlobData.ReadBlobSampleRate).ToString();

    /// <inheritdoc/>
    public override void InitializeII()
    {
        base.InitializeII();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (string.IsNullOrWhiteSpace(Settings.Identifier.Value))
            throw new ArgumentException("Invalid identifier configuration.");

        // This is in InitializeII so the IoTCoreConfiguration for this identifier has run its InitializeI first,
        // creating (and authenticating) the master this subscriber now reads through.
        _device = driver.GetIoLinkMaster(Settings.Identifier.Value);
        _device.BlobDataReceived += OnBlobDataReceived;

        _recordingIdentifier = GetRecordingIdentifier();
    }

    private void OnBlobDataReceived(Measurement measurement)
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
            RecordReasons = [new ErrorState() { TriggerType = "Interval" }],
            Start = measurement.Start,
            Values = measurement.Values,
        });
    }

    /// <inheritdoc/>
    protected override void OnSettingChanged(IRuntimeSetting setting)
        => _recordingIdentifier = GetRecordingIdentifier();

    /// <inheritdoc/>
    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled is false)
            return;

        if (_waitingForData is false &&
            (ProcessDataInputs.Trigger.Changed || cycleInfo.IsFirstCycle) &&
            ProcessDataInputs.Trigger.Value is true)
        {
            _waitingForData = true;
            _device!.GetRecordingBlobData(_recordingIdentifier!, Settings.PortIndex.Value, Settings.IgnoreTimedValueArray.Value);
        }
    }
}
