namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A data node whose value can have an associated unit.
/// </summary>
public interface IDeviceTreeUnitDataNode : IDeviceTreeDataNode
{
    /// <summary>
    /// Whether this value has an associated unit.
    /// </summary>
    bool HasUnit { get; }

    /// <summary>
    /// This value's unit, if <see cref="HasUnit"/> is set.
    /// </summary>
    string Unit { get; }
}
