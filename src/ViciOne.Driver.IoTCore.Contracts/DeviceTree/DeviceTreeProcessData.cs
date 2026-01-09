namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public class DeviceTreeProcessData : IDeviceTreeBase, IDeviceTreeCompressableDataNode, IDeviceTreeLiveDataNode
{
    public List<IDeviceTreeBase> Children { get; set; } = [];
    public List<CompressorConfiguration> CompressorConfigurations { get; set; } = [];
    public DataType DataType { get; set; }
    public DeviceTreeNodeDescription? Description { get; set; }
    public bool HasUnit { get; set; }
    public string? StructureUnit { get; set; }
    public required string Id { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public bool IsWriteable { get; set; }
    public required string Name { get; set; }
    public bool ShowDetailButton { get; set; }
    public int SubIndex { get; set; }
    public bool Visible { get; set; } = true;
}
