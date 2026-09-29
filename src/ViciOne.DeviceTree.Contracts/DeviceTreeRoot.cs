using System.Diagnostics;

namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// The single root node of a device tree, grouping every top-level master/device node.
/// </summary>
[DebuggerDisplay("{nameof(Id),nq}={Id} {nameof(Name),nq}={Name}")]
public sealed class DeviceTreeRoot : IDeviceTreeBase
{
    /// <inheritdoc/>
    public List<IDeviceTreeBase> Children { get; set; } = [];

    /// <inheritdoc/>
    public DeviceTreeNodeDescription? Description { get; set; }

    /// <inheritdoc/>
    public string Id { get; set; }

    /// <inheritdoc/>
    public bool IsNew { get; set; }

    /// <inheritdoc/>
    public ConnectionStatus Status { get; set; }

    /// <inheritdoc/>
    public string Name { get; set; }

    /// <summary>
    /// Creates a root node with its fixed <see cref="Id"/> ("root") and <see cref="Name"/> ("Devices").
    /// </summary>
    public DeviceTreeRoot()
    {
        Id = "root";
        Name = "Devices";
    }
}
