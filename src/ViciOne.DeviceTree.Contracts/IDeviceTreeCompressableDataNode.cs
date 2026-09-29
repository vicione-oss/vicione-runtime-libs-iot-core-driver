namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A data node whose values can be compressed for storage according to one or more configured thresholds.
/// </summary>
public interface IDeviceTreeCompressableDataNode : IDeviceTreeDataNode
{
    /// <summary>
    /// The configured compressor thresholds for this node's values.
    /// </summary>
    List<CompressorConfiguration> CompressorConfigurations { get; }
}
