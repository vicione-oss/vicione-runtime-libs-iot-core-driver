using System.Text.Json.Serialization;

namespace ViciOne.DeviceTree.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "!type")]
[JsonDerivedType(typeof(DeviceTreeBlobData), nameof(DeviceTreeBlobData))]
[JsonDerivedType(typeof(DeviceTreeAssignedName), nameof(DeviceTreeAssignedName))]
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

/// <summary>
/// The base contract shared by every node in a device tree, serialized with a type discriminator so the concrete
/// node kind survives a JSON round-trip.
/// </summary>
public interface IDeviceTreeBase
{
    /// <summary>
    /// This node's child nodes.
    /// </summary>
    List<IDeviceTreeBase> Children { get; }

    /// <summary>
    /// A human-readable description of this node, if any.
    /// </summary>
    DeviceTreeNodeDescription? Description { get; set; }

    /// <summary>
    /// This node's stable identifier within the tree.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// Whether this node was just added to the tree and hasn't been seen by a consumer yet.
    /// </summary>
    bool IsNew { get; set; }

    /// <summary>
    /// This node's owning device's current connection status.
    /// </summary>
    ConnectionStatus Status { get; set; }

    /// <summary>
    /// This node's display name.
    /// </summary>
    string Name { get; set; }
}
