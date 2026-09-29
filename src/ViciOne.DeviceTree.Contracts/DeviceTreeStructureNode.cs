namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A plain grouping node with no data or identity of its own, used purely to organize other nodes in the tree.
/// </summary>
public sealed class DeviceTreeStructureNode : IDeviceTreeBase
{
    /// <inheritdoc/>
    public List<IDeviceTreeBase> Children { get; set; } = [];

    /// <inheritdoc/>
    public DeviceTreeNodeDescription? Description { get; set; }

    /// <inheritdoc/>
    public required string Id { get; set; }

    /// <inheritdoc/>
    public bool IsNew { get; set; }

    /// <inheritdoc/>
    public ConnectionStatus Status { get; set; }

    /// <inheritdoc/>
    public required string Name { get; set; }
}
