namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// Marks a data node as reflecting a live, continuously-updated value rather than a one-off or recorded reading.
/// </summary>
public interface IDeviceTreeLiveDataNode : IDeviceTreeDataNode
{ }
