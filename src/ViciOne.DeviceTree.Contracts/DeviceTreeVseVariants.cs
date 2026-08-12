using System.Diagnostics;

namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a VSE device's variant selection.
/// </summary>
[DebuggerDisplay("{nameof(Name),nq}={Name} {nameof(Topic),nq}={Topic}")]
public sealed class DeviceTreeVseVariants : IDeviceTreeVseDataParent
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

    /// <inheritdoc/>
    public required string Path { get; set; }
}
