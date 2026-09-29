namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a single process data value.
/// </summary>
public interface IDeviceTreeDataNode : IDeviceTreeBase
{
    /// <summary>
    /// This value's data type.
    /// </summary>
    DataType DataType { get; }

    /// <summary>
    /// Whether this value can be written to.
    /// </summary>
    bool IsWriteable { get; }
}
