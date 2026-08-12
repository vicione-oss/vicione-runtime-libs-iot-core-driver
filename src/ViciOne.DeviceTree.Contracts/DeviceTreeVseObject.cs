using System.Diagnostics;

namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a VSE device's object.
/// </summary>
[DebuggerDisplay("{nameof(Name),nq}={Name} {nameof(Topic),nq}={Topic}")]
public sealed class DeviceTreeVseObject : IDeviceTreeVseDataParent, IDeviceTreeAliasNode
{
    /// <inheritdoc/>
    public string? Alias { get; set; }

    /// <inheritdoc/>
    public List<IDeviceTreeBase> Children { get; set; } = [];

    /// <inheritdoc/>
    public DeviceTreeNodeDescription? Description { get; set; }

    /// <inheritdoc/>
    public required string Id { get; set; }

    /// <summary>
    /// The id of the input this object belongs to.
    /// </summary>
    public required string InputId { get; set; }

    /// <summary>
    /// The type of the input this object belongs to.
    /// </summary>
    public required string InputType { get; set; }

    /// <inheritdoc/>
    public bool IsNew { get; set; }

    /// <inheritdoc/>
    public ConnectionStatus Status { get; set; }

    /// <inheritdoc/>
    public required string Name { get; set; }

    /// <summary>
    /// The object's id.
    /// </summary>
    public required string ObjectId { get; set; }

    /// <inheritdoc/>
    public required string Path { get; set; }

    /// <summary>
    /// The object's type.
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// The object's unit.
    /// </summary>
    public required string Unit { get; set; }
}
