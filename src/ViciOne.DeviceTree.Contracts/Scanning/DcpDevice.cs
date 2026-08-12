namespace ViciOne.DeviceTree.Contracts.Scanning;

/// <summary>
/// A PROFINET device found by a DCP network scan.
/// </summary>
public sealed class DcpDevice
{
    /// <summary>
    /// The device's PROFINET identity.
    /// </summary>
    public DcpDeviceIdentity Identity { get; set; } = new();

    /// <summary>
    /// Whether a follow-up HTTP reachability check for this device failed.
    /// </summary>
    public bool IsUnreachable { get; set; }

    /// <summary>
    /// The device's network configuration.
    /// </summary>
    public DcpDeviceNetwork Network { get; set; } = new();

    /// <summary>
    /// The local network adapter the device's DCP response was received on.
    /// </summary>
    public DcpDeviceNetworkAdapter NetworkAdapter { get; set; } = new();

    /// <summary>
    /// Security-related information about the device, derived from its <c>getidentity</c> response.
    /// </summary>
    public DcpDeviceSecurity Security { get; set; } = new();
}
