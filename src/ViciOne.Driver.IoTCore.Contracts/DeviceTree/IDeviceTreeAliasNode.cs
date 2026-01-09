namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public interface IDeviceTreeAliasNode
{
    string Name { get; set; }
    string? NameAlias { get; set; }
}
