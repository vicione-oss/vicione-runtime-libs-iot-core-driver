namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A single timestamped sample within a <see cref="Measurement"/>.
/// </summary>
public sealed class TimedValue
{
    /// <summary>
    /// The sample's timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// The sample's value.
    /// </summary>
    public double Value { get; set; }
}
