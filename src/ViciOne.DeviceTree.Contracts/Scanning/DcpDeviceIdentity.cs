namespace ViciOne.DeviceTree.Contracts.Scanning;

/// <summary>
/// A <see cref="DcpDevice"/>'s PROFINET identity, as reported in its DCP response.
/// </summary>
public sealed class DcpDeviceIdentity
{
    /// <summary>
    /// The device's PROFINET station/device name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The device's PROFINET vendor id.
    /// </summary>
    public ushort VendorId { get; set; }

    /// <summary>
    /// The device's PROFINET device id.
    /// </summary>
    public ushort DeviceId { get; set; }
}
