namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a VSE device's raw-data recording sensor channel.
/// </summary>
public sealed class DeviceTreeVseRawData : IDeviceTreeEventTriggerDataNode, IDeviceTreeConfigurableRawDataNode, IDeviceTreeSchedulableDataNode
{
    /// <inheritdoc/>
    public List<IDeviceTreeBase> Children { get; set; } = [];

    /// <inheritdoc/>
    public DataType DataType => DataType.Blob;

    /// <inheritdoc/>
    public DeviceTreeNodeDescription? Description { get; set; }

    /// <inheritdoc/>
    public List<EventTriggerConfiguration> EventTriggerConfigurations { get; set; } = [];

    /// <inheritdoc/>
    public required string Id { get; set; }

    /// <summary>
    /// The sensor's index on the device.
    /// </summary>
    public int Index { get; set; }

    /// <inheritdoc/>
    public bool IsNew { get; set; }

    /// <inheritdoc/>
    public ConnectionStatus Status { get; set; }

    /// <inheritdoc/>
    public bool IsWriteable => false;

    /// <inheritdoc/>
    public required string Name { get; set; }

    /// <inheritdoc/>
    public Dictionary<Guid, RawDataSettings> RawDataConfigurations { get; set; } = [];

    /// <inheritdoc/>
    public List<SchedulerConfiguration> SchedulerConfigurations { get; set; } = [];

    /// <inheritdoc/>
    public string SensorType => "VSE";
}
