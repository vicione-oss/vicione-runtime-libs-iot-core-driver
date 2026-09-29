namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// Marks a device tree node as not meant to be shown to the user (e.g. internal bookkeeping rather than an
/// actual device value).
/// </summary>
public interface IDeviceTreeHiddenNode : IDeviceTreeBase
{ }
