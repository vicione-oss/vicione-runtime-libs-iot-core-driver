namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public interface IDeviceTreeDataNode : IDeviceTreeBase
{
    DataType DataType { get; }
    bool HasUnit { get; }
    bool IsWriteable { get; }
    bool ShowDetailButton { get; set; }
}
