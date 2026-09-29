namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a single decoded process data value of an IO-Link device.
/// </summary>
public class DeviceTreeProcessData : IDeviceTreeCompressableDataNode, IDeviceTreeLiveDataNode, IDeviceTreeUnitDataNode
{
    /// <inheritdoc/>
    public List<IDeviceTreeBase> Children { get; set; } = [];

    /// <inheritdoc/>
    public List<CompressorConfiguration> CompressorConfigurations { get; set; } = [];

    /// <inheritdoc/>
    public DataType DataType { get; set; }

    /// <inheritdoc/>
    public DeviceTreeNodeDescription? Description { get; set; }

    /// <inheritdoc/>
    public bool HasUnit { get; set; }

    /// <summary>
    /// This value's unit, if <see cref="HasUnit"/> is set.
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <inheritdoc/>
    public required string Id { get; set; }

    /// <inheritdoc/>
    public bool IsNew { get; set; }

    /// <inheritdoc/>
    public ConnectionStatus Status { get; set; }

    /// <inheritdoc/>
    public bool IsWriteable { get; set; }

    /// <inheritdoc/>
    public required string Name { get; set; }

    /// <summary>
    /// The ISDU sub-index this value was decoded from.
    /// </summary>
    public int SubIndex { get; set; }
}
