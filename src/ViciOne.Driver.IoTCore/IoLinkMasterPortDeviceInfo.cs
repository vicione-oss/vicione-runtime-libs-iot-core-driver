namespace ViciOne.Driver.IoTCore;

internal class IoLinkMasterPortDeviceInfo
{
    public required string Address { get; init; }
    public bool? Available { get; set; }
    public uint? DeviceId { get; set; }
    public required string DeviceIdAddress { get; init; }
    public required string PortModeAddress { get; init; }
    public required int PortIndex { get; init; }
    public ushort? VendorId { get; set; }
    public required string VendorIdAddress { get; init; }
}
