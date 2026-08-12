using System.Diagnostics;

namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a VSE device's input channel.
/// </summary>
[DebuggerDisplay("{nameof(Name),nq}={Name} {nameof(Topic),nq}={Topic}")]
public sealed class DeviceTreeVseInput : IDeviceTreeVseDataParent, IDeviceTreeAliasNode
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
    /// The input's id on the device.
    /// </summary>
    public int InputId { get; set; }

    /// <inheritdoc/>
    public bool IsNew { get; set; }

    /// <inheritdoc/>
    public ConnectionStatus Status { get; set; }

    /// <inheritdoc/>
    public required string Name { get; set; }

    /// <inheritdoc/>
    public required string Path { get; set; }

    /// <summary>
    /// The input's unit.
    /// </summary>
    public required string Unit { get; set; }
}
