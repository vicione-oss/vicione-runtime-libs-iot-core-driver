using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// The result of a single blocking raw-data recording, plus the request parameters it was captured with
/// (<see cref="Duration"/>/<see cref="SampleRate"/>) so the capture is self-contained - a caller doesn't need to
/// separately track what it originally asked for alongside what it got back.
/// </summary>
public readonly struct RawDataCapture(DateTime start, uint duration, uint sampleRate, byte[] rawData)
{
    public DateTime Start { get; } = start;
    public uint Duration { get; } = duration;
    public uint SampleRate { get; } = sampleRate;

    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Plain data-transfer type - the caller needs the actual byte[] to forward into the cloud adapter's Measurement.RawData, not a copy-on-read view.")]
    public byte[] RawData { get; } = rawData;

    /// <summary>
    /// The same samples as <see cref="RawData"/>, reinterpreted back to <see langword="float"/>s - computed on
    /// each access rather than stored separately, so a caller that only needs the raw bytes (e.g. because it
    /// ignores the timed value array) never pays for a second, otherwise-unused copy of the whole capture.
    /// </summary>
    public IReadOnlyList<float> Values => MemoryMarshal.Cast<byte, float>(RawData).ToArray();
}
