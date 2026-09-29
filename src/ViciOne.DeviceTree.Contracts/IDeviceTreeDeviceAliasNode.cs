namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node the device names itself. The alias is read from the device and refreshed every time
/// the tree is parsed, so it is not the user's to edit. It stands in for the node's technical
/// <see cref="IDeviceTreeBase.Name"/>, which stays available as a secondary identifier.
/// </summary>
public interface IDeviceTreeDeviceAliasNode : IDeviceTreeAliasNode;
