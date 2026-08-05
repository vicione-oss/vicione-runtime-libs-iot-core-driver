using System.Diagnostics;

namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

[DebuggerDisplay("{nameof(VendorId),nq}={VendorId} {nameof(ProductId),nq}={ProductId} {nameof(DeviceId),nq}={DeviceId}")]
public sealed class DeviceTreeDevice : IDeviceTreeBase
{
    public string? ApplicationSpecificTag { get; set; }

    public required string Name { get; set; }

    public ushort VendorId { get; set; }
    public uint DeviceId { get; set; }
    public string? ProductId { get; set; }
    
    public List<IDeviceTreeBase> Children { get; set; } = [];
    public DeviceTreeNodeDescription? Description { get; set; }
    public required string Id { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public bool IsUnknown { get; set; }
    public bool Visible => true;
}
