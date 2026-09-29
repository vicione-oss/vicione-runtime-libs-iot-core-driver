using System.Diagnostics;

namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing an IO-Link master.
/// </summary>
[DebuggerDisplay("{nameof(Alias),nq}={Alias}")]
public sealed class DeviceTreeIoLinkMaster : IDeviceTreeMasterNode, IDeviceTreeUserAliasNode
{
    /// <inheritdoc/>
    public string? Alias { get; set; }

    /// <summary>
    /// The master's MAC address.
    /// </summary>
    public required string MacAddress { get; set; }

    /// <summary>
    /// The master's bootloader revision, if known.
    /// </summary>
    public string? BootLoaderRevision { get; set; }

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

    /// <inheritdoc/>
    public string? Manufacturer { get; set; }

    /// <inheritdoc/>
    public required string Name { get; set; }

    /// <inheritdoc/>
    public string? ProductName { get; set; }

    /// <inheritdoc/>
    public string? SerialNumber { get; set; }

    /// <inheritdoc/>
    public string? SoftwareRevision { get; set; }

    /// <inheritdoc/>
    public required Uri Url { get; set; }

    /// <summary>
    /// The username this master is configured to authenticate with, or <see langword="null"/> for no authentication.
    /// <para>Carried along so the engineering UI - which uses this tree to configure, and later regenerate, this
    /// master's FunctionBlocks - can preserve it.</para>
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// The password this master is configured to authenticate with, ignored if <see cref="Username"/> is <see langword="null"/>.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Security-related information about the master, as reported by the master itself.
    /// </summary>
    public DeviceSecurity Security { get; set; } = new();
}
