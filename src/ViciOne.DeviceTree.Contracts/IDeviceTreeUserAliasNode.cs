namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node the user can name. The alias is entered by the user, stored by the consumer and
/// left untouched when the tree is parsed again, so it must not be overwritten from a parsed node.
/// It complements the node's own <see cref="IDeviceTreeBase.Name"/> rather than replacing it, and a node
/// without an alias is identified by its name alone.
/// </summary>
public interface IDeviceTreeUserAliasNode : IDeviceTreeAliasNode;
