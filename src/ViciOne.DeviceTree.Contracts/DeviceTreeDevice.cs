using System.Diagnostics;

namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing an IO-Link device connected to a master port.
/// </summary>
[DebuggerDisplay("{nameof(VendorId),nq}={VendorId} {nameof(ProductId),nq}={ProductId} {nameof(DeviceId),nq}={DeviceId}")]
public sealed class DeviceTreeDevice : IDeviceTreeBase
{
    /// <summary>
    /// The device's configured application-specific tag, if any.
    /// </summary>
    public string? ApplicationSpecificTag { get; set; }

    /// <inheritdoc/>
    public required string Name { get; set; }

    /// <summary>
    /// The device's vendor ID.
    /// </summary>
    public ushort VendorId { get; set; }

    /// <summary>
    /// The device's device ID.
    /// </summary>
    public uint DeviceId { get; set; }

    /// <summary>
    /// The device's IODD product id, used to disambiguate device variants that share the same product name.
    /// </summary>
    public string? ProductId { get; set; }

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

    /// <summary>
    /// Whether no matching IODD definition could be found for this device.
    /// </summary>
    public bool IsUnknown { get; set; }
}
