namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A single raw-data/blob recording result, pushed out to the cloud data adapter. Moved in from the external
/// <c>ACX.ViciOne.Data.Driver.CloudDataAdapter.Contracts</c> package, which contained nothing but this class and
/// <see cref="TimedValue"/> - reproduced here instead of pulling in a whole package for two DTOs.
/// </summary>
public sealed class Measurement
{
    /// <summary>
    /// The recording's duration, in milliseconds.
    /// </summary>
    public double Duration { get; set; }

    /// <summary>
    /// The recording's sample rate, in Hz.
    /// </summary>
    public double Frequency { get; set; }

    /// <summary>
    /// The identifier correlating this result with the request that triggered it.
    /// </summary>
    public required string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// The recording's raw, still-encoded data.
    /// </summary>
    public byte[] RawData { get; set; } = [];

    /// <summary>
    /// The conditions that triggered this recording.
    /// </summary>
    public IEnumerable<object> RecordReasons { get; set; } = [];

    /// <summary>
    /// The recording's start time.
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// The recording's decoded, timestamped values.
    /// </summary>
    public TimedValue[] Values { get; set; } = [];
}
