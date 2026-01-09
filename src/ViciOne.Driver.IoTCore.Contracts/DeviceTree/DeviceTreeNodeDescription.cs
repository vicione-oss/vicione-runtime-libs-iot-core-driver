namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public sealed class DeviceTreeNodeDescription
{
    public Dictionary<string, string> AdditionalInfo { get; set; } = [];
    public string? Icon { get; set; }
    public required string Text { get; set; }
}
