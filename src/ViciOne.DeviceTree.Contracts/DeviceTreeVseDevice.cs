using System.Diagnostics;

namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a VSE device.
/// </summary>
[DebuggerDisplay("{nameof(Alias),nq}={Alias}")]
public sealed class DeviceTreeVseDevice : IDeviceTreeMasterNode, IDeviceTreeUserAliasNode
{
    /// <inheritdoc/>
    public string? Alias { get; set; }

    /// <inheritdoc/>
    public List<IDeviceTreeBase> Children { get; set; } = [];

    /// <inheritdoc/>
    public DeviceTreeNodeDescription? Description { get; set; }

    /// <inheritdoc/>
    public string? DeviceFamily { get; set; }

    /// <inheritdoc/>
    public string? HardwareRevision { get; set; }

    /// <inheritdoc/>
    public required string Id { get; set; }

    /// <inheritdoc/>
    public bool IsNew { get; set; }

    /// <inheritdoc/>
    public ConnectionStatus Status { get; set; }

    /// <summary>
    /// The device's MAC address.
    /// </summary>
    public required string MacAddress { get; set; }

    /// <inheritdoc/>
    public required string Name { get; set; }

    /// <inheritdoc/>
    public string? Manufacturer { get; set; }

    /// <inheritdoc/>
    public string? ProductName { get; set; }

    /// <inheritdoc/>
    public string? SerialNumber { get; set; }

    /// <inheritdoc/>
    public string? SoftwareRevision { get; set; }

    /// <inheritdoc/>
    public required Uri Url { get; set; }
}
