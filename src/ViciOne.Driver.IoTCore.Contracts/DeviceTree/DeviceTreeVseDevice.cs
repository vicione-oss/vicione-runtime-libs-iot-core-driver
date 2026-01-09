using System.Diagnostics;

namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

[DebuggerDisplay("{nameof(NameAlias),nq}={NameAlias}")]
public sealed class DeviceTreeVseDevice : IDeviceTreeMasterNode, IDeviceTreeAliasNode
{
    public List<IDeviceTreeBase> Children { get; set; } = [];
    public DeviceTreeNodeDescription? Description { get; set; }
    public string? DeviceFamily { get; set; }
    public string? HardwareRevision { get; set; }
    public required string Id { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public required string MacAddress { get; set; }
    public required string Name { get; set; }
    public string? NameAlias { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? SerialNumber { get; set; }
    public string? SoftwareRevision { get; set; }
    public required Uri Url { get; set; }
    public bool Visible => true;
}
