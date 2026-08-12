using System.Diagnostics;

namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a single port on an IO-Link master.
/// </summary>
[DebuggerDisplay("{nameof(SubIndex),nq}={SubIndex}")]
public sealed class DeviceTreeIoLinkMasterPort : IDeviceTreeBase
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

    /// <summary>
    /// The port's index on the master.
    /// </summary>
    public int SubIndex { get; set; }
}
