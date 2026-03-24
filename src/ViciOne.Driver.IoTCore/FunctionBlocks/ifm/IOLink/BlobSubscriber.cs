using ACX.ViciOne.Data.Driver.CloudDataAdapter.Contracts;
using System;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;
using ViciOne.Driver.IoTCore.Contracts.Constants;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace ifm.IOLink;

public sealed partial class BlobSubscriber
{
    private const string Unknown_ApplicationSpecificTag = "unknown";
    private const string Unknown_ProductName = "unknown";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Verwerfbare Felder verwerfen", Justification = "Wird vom DeviceManager freigegeben.")]
    private IoLinkMaster? _device;
    private string? _recordingIdentifier;
    private bool _waitingForData;

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
        => Identifiers.GetIoLinkRawDataIdentifier(Settings.Address.Value!,
            Settings.ApplicationSpecificTag.Value ?? Unknown_ApplicationSpecificTag, Settings.PortIndex.Value,
            Settings.VendorId.Value, Settings.DeviceId.Value, Settings.ProductName.Value ?? Unknown_ProductName, Identifiers.Recording,
            DeviceTreeBlobData.ReadBlobDuration, DeviceTreeBlobData.ReadBlobSampleRate);

    public override void InitializeII()
    {
        base.InitializeII();

        if (Driver is not IoTCoreDriver driver)
            throw new InvalidCastException($"Could not cast to {nameof(IoTCoreDriver)}.");

        if (Settings.Address.Value is null)
            throw new ArgumentException("Invalid address configuration.");

        if (Settings.ProductName.Value is null)
            throw new ArgumentException("Invalid product name configuration.");

        _recordingIdentifier = GetRecordingIdentifier();

        // Liegt im InitializeII damit der DeviceTreeSubscriber zuerst initialisiert wird und das Gerät mit dem konfigurierten IoddDirectory anlegt
        _device = driver.GetOrAddIoLinkMaster(Settings.Address.Value);
        _device.BlobDataReceived += OnBlobDataReceived;
    }

    private void OnBlobDataReceived(Measurement measurement)
    {
        if (_waitingForData == false || string.Equals(measurement.Identifier, _recordingIdentifier, StringComparison.Ordinal) == false)
            return;

        measurement.RecordReasons = [new ErrorState() { TriggerType = "Interval" }];

        _waitingForData = false;
        SetConnectorValueIfNotEqual(ProcessDataOutputs.Data, measurement);
    }

    protected override void OnSettingChanged(IRuntimeSetting setting)
        => _recordingIdentifier = GetRecordingIdentifier();

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
