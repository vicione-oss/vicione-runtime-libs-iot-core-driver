namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public interface IDeviceTreeMasterNode : IDeviceTreeBase
{
    string? DeviceFamily { get; }
    Uri Url { get; }
    string? SerialNumber { get; }
    string? SoftwareRevision { get; }
    string? HardwareRevision { get; }
    string? Manufacturer { get; }
    string? ManufacturerId { get; }
    string? ProductCode { get; }
    string? ProductName { get; }
}
