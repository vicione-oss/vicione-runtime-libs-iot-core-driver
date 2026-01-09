using System.Diagnostics;

namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

[DebuggerDisplay("{nameof(Name),nq}={Name} {nameof(Topic),nq}={Topic}")]
public sealed class DeviceTreeVseAlarm : IDeviceTreeVseDataParent, IAliasStructureNode
{
    public string? Alias { get; set; }
    public List<IDeviceTreeBase> Children { get; set; } = [];
    public DeviceTreeNodeDescription? Description { get; set; }
    public required string Id { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required string Type { get; set; }
    public bool Visible => true;
}
