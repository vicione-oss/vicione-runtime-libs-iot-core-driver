namespace ViciOne.Driver.IoTCore.Contracts.Dcp;

public sealed class DcpDevice
{
    public string AdapterId { get; set; } = string.Empty;
    public string AdapterName { get; set; } = string.Empty;
    public Uri Address { get; set; } = default!;
    public ushort DeviceId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string Gateway { get; set; } = string.Empty;
    public bool IsUnreachable { get; set; }
    public string Ip { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Netmask { get; set; } = string.Empty;
    public ushort VendorId { get; set; }
}
