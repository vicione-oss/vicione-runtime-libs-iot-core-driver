namespace ViciOne.DeviceTree.Contracts.Scanning;

/// <summary>
/// The result of a local-network scan for VSE devices.
/// </summary>
public sealed class VseScanningResult
{
    /// <summary>
    /// The devices found by the scan.
    /// </summary>
    public List<VseScanDevice> Devices { get; set; } = [];

    /// <summary>
    /// Any warning or error messages produced while scanning.
    /// </summary>
    public List<string> Messages { get; set; } = [];
}
