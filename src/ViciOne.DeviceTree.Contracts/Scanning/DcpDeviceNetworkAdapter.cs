namespace ViciOne.DeviceTree.Contracts.Scanning;

/// <summary>
/// The local network adapter a <see cref="DcpDevice"/>'s DCP response was received on.
/// </summary>
public sealed class DcpDeviceNetworkAdapter
{
    /// <summary>
    /// The adapter's id.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The adapter's display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}
