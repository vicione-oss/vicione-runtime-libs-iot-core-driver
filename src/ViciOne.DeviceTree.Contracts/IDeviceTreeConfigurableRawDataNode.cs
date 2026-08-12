namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A data node whose raw-data recordings can be configured, keyed by the configuration's id.
/// </summary>
public interface IDeviceTreeConfigurableRawDataNode : IDeviceTreeDataNode
{
    /// <summary>
    /// The configured raw-data recording settings for this node, keyed by configuration id.
    /// </summary>
    Dictionary<Guid, RawDataSettings> RawDataConfigurations { get; }
}
