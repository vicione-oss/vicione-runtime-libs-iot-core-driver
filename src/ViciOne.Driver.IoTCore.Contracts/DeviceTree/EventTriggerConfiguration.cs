namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public sealed class EventTriggerConfiguration
{
    public bool IsSensorConfigured { get; set; }
    public required string Name { get; set; }
    public required string ReferenceNodeId { get; set; }
    public List<EventTrigger> Triggers { get; set; } = [];
}
