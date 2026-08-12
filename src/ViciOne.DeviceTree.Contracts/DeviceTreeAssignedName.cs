namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node holding a stable slot's current semantic name as a plain text value, so a consumer diffing
/// two trees can detect that the slot was reassigned even though its own <see cref="Id"/>/<see cref="Name"/>
/// (the slot's fixed technical address, e.g. "Object01") stayed the same.
/// </summary>
public class DeviceTreeAssignedName : IDeviceTreeHiddenNode
{
    /// <inheritdoc/>
    public List<IDeviceTreeBase> Children { get; } = [];

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

    /// <summary>
    /// The slot's current semantic name.
    /// </summary>
    public required string Value { get; set; }
}
