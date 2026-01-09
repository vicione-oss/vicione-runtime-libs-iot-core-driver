namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public sealed class TopicData
{
    public required string BrokerId { get; set; }
    public DataType DataType { get; set; }
    public required string Topic { get; set; }
}
