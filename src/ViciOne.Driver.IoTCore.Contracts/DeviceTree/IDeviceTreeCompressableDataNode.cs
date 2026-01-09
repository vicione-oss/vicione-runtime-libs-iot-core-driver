namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public interface IDeviceTreeCompressableDataNode : IDeviceTreeDataNode
{
    List<CompressorConfiguration> CompressorConfigurations { get; }
}
