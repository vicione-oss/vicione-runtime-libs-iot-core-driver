namespace ViciOne.Driver.IoTCore;

internal class IoLinkMasterPortDeviceInfo
{
    public required string Address { get; init; }
    public bool? Available { get; set; }
    public uint? DeviceId { get; set; }
    public required string DeviceIdAddress { get; init; }
    // True whenever this port's availability or identity changed since the last successful full
    // device-tree parse (starts true so the very first parse always reads every port in full). Lets
    // ParseDeviceAsync skip the expensive per-device detail reads (IODD data, product info, ...) for
    // ports that stayed continuously connected, reusing the cached result from last time instead.
    public bool NeedsFullRefresh { get; set; } = true;
    public required string PortModeAddress { get; init; }
    public required int PortIndex { get; init; }
    public ushort? VendorId { get; set; }
    public required string VendorIdAddress { get; init; }
}
