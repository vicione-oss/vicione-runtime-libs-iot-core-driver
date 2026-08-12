namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A configured compression rule for a data node, aggregating values over a time interval before storage.
/// </summary>
public sealed class CompressorConfiguration
{
    /// <summary>
    /// The compression interval, in seconds.
    /// </summary>
    public int CompressionTime { get; set; }

    /// <summary>
    /// The identifier grouping this configuration with related configurations.
    /// </summary>
    public required Guid DataGroupIdentifier { get; set; }

    /// <summary>
    /// Whether this configuration is currently active.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// How values are aggregated over <see cref="CompressionTime"/>.
    /// </summary>
    public AggregationFunction Aggregation { get; set; }
}
