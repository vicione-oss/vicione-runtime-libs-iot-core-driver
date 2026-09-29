namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A named group of <see cref="EventTrigger"/>s configured for a data node.
/// </summary>
public sealed class EventTriggerConfiguration
{
    /// <summary>
    /// Whether the referenced sensor is configured and ready to trigger.
    /// </summary>
    public bool IsSensorConfigured { get; set; }

    /// <summary>
    /// The configuration's display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The id of the node this configuration's triggers reference.
    /// </summary>
    public required string ReferenceNodeId { get; set; }

    /// <summary>
    /// The individual triggers in this configuration.
    /// </summary>
    public List<EventTrigger> Triggers { get; set; } = [];
}
