namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node representing a BLOB-channel raw-data recording process on a VVB device.
/// </summary>
public sealed class DeviceTreeBlobData : IDeviceTreeSchedulableDataNode
{
    /// <summary>
    /// The fixed duration, in milliseconds, of a BLOB recording.
    /// </summary>
    public const int ReadBlobDuration = 4_000; //it's always 4 seconds

    /// <summary>
    /// The fixed sample rate, in Hz, of a BLOB recording.
    /// </summary>
    public const int ReadBlobSampleRate = 25_000; //hardcoded for VVB

    //Originally from ACX.ViciOne.IoTCoreManager.Constants.Identifiers, but that's only included as a file in the PoC
    //we should ideally pull that in properly once we reference the drivers
    /// <summary>
    /// The default process name segment used for this node's recording identifier.
    /// </summary>
    public const string DefaultProcessName = "Recording";

    /// <inheritdoc/>
    public List<IDeviceTreeBase> Children { get; set; } = [];

    /// <inheritdoc/>
    public DataType DataType { get; } = DataType.Blob;

    /// <inheritdoc/>
    public DeviceTreeNodeDescription? Description { get; set; }

    /// <inheritdoc/>
    public required string Id { get; set; }

    /// <inheritdoc/>
    public bool IsNew { get; set; }

    /// <inheritdoc/>
    public ConnectionStatus Status { get; set; }

    /// <inheritdoc/>
    public bool IsWriteable => false;

    /// <inheritdoc/>
    public required string Name { get; set; }

    /// <inheritdoc/>
    public List<SchedulerConfiguration> SchedulerConfigurations { get; set; } = [];

    /// <inheritdoc/>
    public string SensorType { get; } = "VVB";
}
