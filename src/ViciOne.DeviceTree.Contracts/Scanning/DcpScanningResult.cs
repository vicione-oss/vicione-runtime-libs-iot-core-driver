namespace ViciOne.DeviceTree.Contracts.Scanning;

/// <summary>
/// The result of a DCP network scan for PROFINET devices.
/// </summary>
public sealed class DcpScanningResult
{
    /// <summary>
    /// The devices found by the scan.
    /// </summary>
    public List<DcpDevice> Devices { get; set; } = [];

    /// <summary>
    /// Any warning or error messages produced while scanning.
    /// </summary>
    public List<string> Messages { get; set; } = [];
}
