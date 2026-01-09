using System.Text.Json.Serialization;

namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "!type")]
[JsonDerivedType(typeof(DeviceTreeBlobData), nameof(DeviceTreeBlobData))]
[JsonDerivedType(typeof(DeviceTreeConstantData), nameof(DeviceTreeConstantData))]
[JsonDerivedType(typeof(DeviceTreeDevice), nameof(DeviceTreeDevice))]
[JsonDerivedType(typeof(DeviceTreeIoLinkMaster), nameof(DeviceTreeIoLinkMaster))]
[JsonDerivedType(typeof(DeviceTreeIoLinkMasterPort), nameof(DeviceTreeIoLinkMasterPort))]
[JsonDerivedType(typeof(DeviceTreeProcessData), nameof(DeviceTreeProcessData))]
[JsonDerivedType(typeof(DeviceTreeRoot), nameof(DeviceTreeRoot))]
[JsonDerivedType(typeof(DeviceTreeStructureNode), nameof(DeviceTreeStructureNode))]
[JsonDerivedType(typeof(DeviceTreeVseAlarm), nameof(DeviceTreeVseAlarm))]
[JsonDerivedType(typeof(DeviceTreeVseCounter), nameof(DeviceTreeVseCounter))]
[JsonDerivedType(typeof(DeviceTreeVseDevice), nameof(DeviceTreeVseDevice))]
[JsonDerivedType(typeof(DeviceTreeVseInput), nameof(DeviceTreeVseInput))]
[JsonDerivedType(typeof(DeviceTreeVseObject), nameof(DeviceTreeVseObject))]
[JsonDerivedType(typeof(DeviceTreeVseRawData), nameof(DeviceTreeVseRawData))]
[JsonDerivedType(typeof(DeviceTreeVseVariants), nameof(DeviceTreeVseVariants))]

public interface IDeviceTreeBase
{
    List<IDeviceTreeBase> Children { get; }
    DeviceTreeNodeDescription? Description { get; set; }
    string Id { get; set; }
    bool IsNew { get; set; }
    bool IsOffline { get; set; }
    string Name { get; set; }
    bool Visible { get; }
}
