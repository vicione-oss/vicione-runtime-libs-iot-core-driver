namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node that can be given a user-facing alias distinct from its own <see cref="IDeviceTreeBase.Name"/>.
/// </summary>
public interface IDeviceTreeAliasNode : IDeviceTreeBase
{
    /// <summary>
    /// The user-facing alias for this node, if set.
    /// </summary>
    string? Alias { get; set; }
}
