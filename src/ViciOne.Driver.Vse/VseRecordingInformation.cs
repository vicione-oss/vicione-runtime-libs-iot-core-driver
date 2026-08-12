using System;

namespace ViciOne.Driver.Vse;

/// <summary>
/// A queued raw-data recording request, as tracked by <see cref="VseRawDataCollector"/>.
/// </summary>
internal sealed class VseRecordingInformation
{
    /// <summary>
    /// The device to record from.
    /// </summary>
    public required VseDevice Device { get; set; }

    /// <summary>
    /// The recording's duration, in milliseconds.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Called if the recording fails.
    /// </summary>
    public required Action<Exception> HandleError { get; set; }

    /// <summary>
    /// An identifier for correlating this request with its eventual result.
    /// </summary>
    public required string Identifier { get; set; }

    /// <summary>
    /// Whether to skip decoding the recorded raw data into timed values.
    /// </summary>
    public bool IgnoreTimedValueArray { get; set; }

    /// <summary>
    /// The recording's sample rate, in Hz.
    /// </summary>
    public int SampleRate { get; set; }

    /// <summary>
    /// The sensor to record from.
    /// </summary>
    public int SensorIndex { get; set; }
}
