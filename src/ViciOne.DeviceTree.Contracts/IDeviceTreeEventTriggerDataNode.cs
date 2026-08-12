namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A data node whose recording can be triggered by one or more configured events.
/// </summary>
public interface IDeviceTreeEventTriggerDataNode : IDeviceTreeDataNode
{
    /// <summary>
    /// The configured event triggers for this node.
    /// </summary>
    List<EventTriggerConfiguration> EventTriggerConfigurations { get; }
}
