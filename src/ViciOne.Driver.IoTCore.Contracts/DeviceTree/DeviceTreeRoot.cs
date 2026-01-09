using System.Diagnostics;

namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

[DebuggerDisplay("{nameof(Id),nq}={Id} {nameof(Name),nq}={Name}")]
public sealed class DeviceTreeRoot : IDeviceTreeBase
{
    public List<IDeviceTreeBase> Children { get; set; } = [];
    public DeviceTreeNodeDescription? Description { get; set; }
    public string Id { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public string Name { get; set; }
    public bool Visible => true;

    public DeviceTreeRoot()
    {
        Id = "root";
        Name = "Devices";
    }
}
