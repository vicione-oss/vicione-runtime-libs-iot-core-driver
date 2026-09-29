namespace ViciOne.DeviceTree.Contracts.Scanning;

/// <summary>
/// A VSE device found by a local-network scan.
/// </summary>
public sealed class VseScanDevice
{
    /// <summary>
    /// The device's type.
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>
    /// The device's firmware version.
    /// </summary>
    public string FirmwareVersion { get; set; } = string.Empty;

    /// <summary>
    /// The device's hardware revision.
    /// </summary>
    public string Hardware { get; set; } = string.Empty;

    /// <summary>
    /// The device's host name.
    /// </summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>
    /// The device's IP address.
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The device's MAC address.
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// The device's port.
    /// </summary>
    public int Port { get; set; }
}
