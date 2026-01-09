using System.Diagnostics;

namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

[DebuggerDisplay("{nameof(Name),nq}={Name} {nameof(Topic),nq}={Topic}")]
public sealed class DeviceTreeVseInput : IDeviceTreeVseDataParent, IAliasStructureNode
{
    public string? Alias { get; set; }
    public List<IDeviceTreeBase> Children { get; set; } = [];
    public DeviceTreeNodeDescription? Description { get; set; }
    public required string Id { get; set; }
    public int InputId { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required string Unit { get; set; }
    public bool Visible => true;
}
