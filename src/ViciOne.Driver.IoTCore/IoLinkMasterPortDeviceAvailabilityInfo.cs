namespace ViciOne.Driver.IoTCore;

internal record IoLinkMasterPortDeviceAvailabilityInfo
{
    public required bool Available { get; init; }
    public required uint DeviceId { get; init; }
    public required ushort VendorId { get; init; }
}
