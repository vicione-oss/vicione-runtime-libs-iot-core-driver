namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public class DeviceTreeConstantData : IDeviceTreeBase, IDeviceTreeLiveDataNode
{
    public List<IDeviceTreeBase> Children { get; set; } = [];
    public DataType DataType { get; set; }
    public DeviceTreeNodeDescription? Description { get; set; }
    public bool HasUnit { get; set; }
    public required string Id { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public bool IsWriteable => false;
    public required string Name { get; set; }
    public bool ShowDetailButton { get; set; }
    public int SubIndex { get; set; }
    public string? Unit { get; set; }
    public required string Value { get; set; }
    public bool Visible { get; set; } = true;
}
