namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public interface IDeviceTreeConfigurableRawDataNode : IDeviceTreeDataNode
{
    Dictionary<Guid, RawDataSettings> RawDataConfigurations { get; }
}
