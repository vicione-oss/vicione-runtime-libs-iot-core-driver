namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public sealed class DeviceTreeVseRawData : IDeviceTreeBase, IDeviceTreeEventTriggerDataNode, IDeviceTreeConfigurableRawDataNode, IDeviceTreeSchedulableDataNode
{
    public List<IDeviceTreeBase> Children { get; set; } = [];
    public DataType DataType { get; } = DataType.BlobT;
    public DeviceTreeNodeDescription? Description { get; set; }
    public List<EventTriggerConfiguration> EventTriggerConfigurations { get; set; } = [];
    public bool HasUnit => false;
    public required string Id { get; set; }
    public int Index { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public bool IsWriteable => false;
    public required string Name { get; set; }
    public Dictionary<Guid, RawDataSettings> RawDataConfigurations { get; set; } = [];
    public List<SchedulerConfiguration> SchedulerConfigurations { get; set; } = [];
    public string SensorType { get; } = "VSE";
    public bool ShowDetailButton { get; set; }
    public required string Unit { get; set; }
    public bool Visible => true;
}
