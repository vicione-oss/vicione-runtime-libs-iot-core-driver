namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a master device's own identity and connection details.
/// </summary>
public interface IDeviceTreeMasterNode : IDeviceTreeBase
{
    /// <summary>
    /// The device family this master belongs to, if known.
    /// </summary>
    string? DeviceFamily { get; }

    /// <summary>
    /// The master's address.
    /// </summary>
    Uri Url { get; }

    /// <summary>
    /// The master's serial number, if known.
    /// </summary>
    string? SerialNumber { get; }

    /// <summary>
    /// The master's firmware/software revision, if known.
    /// </summary>
    string? SoftwareRevision { get; }

    /// <summary>
    /// The master's hardware revision, if known.
    /// </summary>
    string? HardwareRevision { get; }

    /// <summary>
    /// The master's manufacturer name, if known.
    /// </summary>
    string? Manufacturer { get; }

    /// <summary>
    /// The master's product name, if known.
    /// </summary>
    string? ProductName { get; }
}
