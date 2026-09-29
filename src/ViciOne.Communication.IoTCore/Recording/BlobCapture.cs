using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;

namespace ViciOne.Communication.IoTCore.Recording;

/// <summary>
/// A single BLOB-channel recording result.
/// <para>Only <see cref="RawData"/> is stored - <see cref="Values"/> is derived from it on access rather than
/// cached, since not every caller needs the decoded values.</para>
/// </summary>
public sealed class BlobCapture(DateTime start, double duration, double frequency, byte[] rawData)
{
    // Each sample is a little-endian Int16 scaled to physical units - see Values below.
    private const double ScalingFactor = 125.0 / 65536.0 * 9.80665; // 125 / Math.Pow(2, 16) * 9.80665
    private const int SignificantDigits = 6;

    public DateTime Start { get; } = start;
    public double Duration { get; } = duration;
    public double Frequency { get; } = frequency;

    [SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "A lightweight, short-lived capture result, not a general-purpose collection type.")]
    public byte[] RawData { get; } = rawData;

    public IReadOnlyList<double> Values
    {
        get
        {
            double[] values = new double[RawData.Length / 2];
            for (int i = 0; i < values.Length; i++)
                values[i] = Math.Round(BinaryPrimitives.ReadInt16LittleEndian(RawData.AsSpan(i * 2, 2)) * ScalingFactor,
                    SignificantDigits, MidpointRounding.AwayFromZero);

            return values;
        }
    }
}
