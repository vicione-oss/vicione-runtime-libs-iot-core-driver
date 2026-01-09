namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public interface IDeviceTreeEventTriggerDataNode : IDeviceTreeDataNode
{
    List<EventTriggerConfiguration> EventTriggerConfigurations { get; }
}
