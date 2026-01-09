namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public interface IAliasStructureNode : IDeviceTreeBase
{
    string? Alias { get; set; }
}
