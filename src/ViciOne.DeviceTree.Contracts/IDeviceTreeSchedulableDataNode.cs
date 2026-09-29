namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A data node whose recording can be scheduled according to one or more configured time schedules.
/// </summary>
public interface IDeviceTreeSchedulableDataNode : IDeviceTreeDataNode
{
    /// <summary>
    /// The configured recording schedules for this node.
    /// </summary>
    List<SchedulerConfiguration> SchedulerConfigurations { get; }

    /// <summary>
    /// The sensor type this node's value originates from.
    /// </summary>
    string SensorType { get; }
}
