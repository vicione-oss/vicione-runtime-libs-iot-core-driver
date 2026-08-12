using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// Static identity information for a VSE device - type, serial number, firmware and hardware revision. Unlike
/// <see cref="Status"/>, none of this changes while the device is connected.
/// </summary>
public sealed class DeviceInfo(IVSEDevice device)
{
    public string Type => ReflectionUtils.DeviceType2Str(device.DeviceInfo.DeviceType);
    public string Name => $"{Type}-{device.DeviceInfo.SerialNumber}";
    public string DeviceName => device.DeviceInfo.DeviceName;
    public string SerialNumber => device.DeviceInfo.SerialNumber;
    public string MacAddress => device.DeviceInfo.MacAddress;
    public FirmwareType FirmwareType => device.DeviceInfo.FirmwareType;
    public IFM.SDK.VSE.Version FirmwareVersion => device.DeviceInfo.FirmwareVersion;
    public string BoardVersion => device.DeviceInfo.HardwareVersion.BoardVersion;
    public string ProductionCode => device.DeviceInfo.HardwareVersion.ProductionCode;
    public ProductionDate ProductionDate => device.DeviceInfo.HardwareVersion.ProductionDate;

    /// <summary>
    /// Reads the device's own current date/time (its internal clock, queried live - not cached).
    /// </summary>
    public DateTime GetDeviceDateTime() => device.Commands.GetDateTime();
}
