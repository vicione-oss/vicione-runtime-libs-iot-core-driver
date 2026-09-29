namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a VSE device node group, addressable by its VSE data model path.
/// </summary>
public interface IDeviceTreeVseDataParent : IDeviceTreeBase
{
    /// <summary>
    /// The node's path in the VSE device's data model.
    /// </summary>
    string Path { get; set; }
}
