namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A configured duration/frequency pair for a raw-data recording.
/// </summary>
public class RawDataSettings
{
    /// <summary>
    /// The recording's duration, in milliseconds.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// The recording's sample rate, in Hz.
    /// </summary>
    public int Frequency { get; set; }
}
