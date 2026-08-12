namespace ViciOne.DeviceTree.Contracts.Scanning;

/// <summary>
/// A <see cref="DcpDevice"/>'s network configuration, as reported in its DCP response.
/// </summary>
public sealed class DcpDeviceNetwork
{
    /// <summary>
    /// The device's HTTP address.
    /// </summary>
    public Uri Address { get; set; } = default!;

    /// <summary>
    /// The device's IP address.
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// The device's configured subnet mask.
    /// </summary>
    public string Netmask { get; set; } = string.Empty;

    /// <summary>
    /// The device's configured gateway address.
    /// </summary>
    public string Gateway { get; set; } = string.Empty;

    /// <summary>
    /// The device's MAC address.
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;
}
