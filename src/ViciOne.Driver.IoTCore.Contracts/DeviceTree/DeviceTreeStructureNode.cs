namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public sealed class DeviceTreeStructureNode : IDeviceTreeBase
{
    public List<IDeviceTreeBase> Children { get; set; } = [];
    public DeviceTreeNodeDescription? Description { get; set; }
    public required string Id { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public required string Name { get; set; }
    public bool Visible => true;
}
