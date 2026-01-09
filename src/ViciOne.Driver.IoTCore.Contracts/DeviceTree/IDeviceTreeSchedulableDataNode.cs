namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public interface IDeviceTreeSchedulableDataNode : IDeviceTreeDataNode
{
    List<SchedulerConfiguration> SchedulerConfigurations { get; }
    string SensorType { get; }
}
