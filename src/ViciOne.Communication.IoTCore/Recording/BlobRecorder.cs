using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using ViciOne.DeviceTree.Contracts;

namespace ViciOne.Communication.IoTCore.Recording;

/// <summary>
/// Records a device's BLOB channel and returns the result as a plain <see cref="BlobCapture"/>.
/// <para>Shaping the capture into a <c>Measurement</c> (identifier, timestamped values, ...) is the caller's
/// job, not this class's.</para>
/// <para>Owns the HTTP connection to <paramref name="address"/> for its whole lifetime.</para>
/// </summary>
public sealed class BlobRecorder(string address) : IDisposable
{
    private readonly BlobHttpClient _device = new(new Uri($"http://{address}"));

    public void Dispose()
        => _device.Dispose();

    /// <summary>
    /// Records one full BLOB-channel cycle (idle-check, start, read until the last segment, finish) on
    /// <paramref name="port"/>.
    /// </summary>
    /// <param name="port">The IO-Link master port to record.</param>
    /// <param name="logger">Logger for diagnostic messages about the recording progress.</param>
    /// <param name="token">Cancellation token for the underlying HTTP requests.</param>
    /// <returns><see langword="true"/> with the resulting <see cref="BlobCapture"/> on success; otherwise <see langword="false"/> with a message describing why.</returns>
    public async Task<(bool Success, string? ErrorMessage, BlobCapture? Capture)> ExecuteAsync(int port, ILogger logger, CancellationToken token)
    {
        BlobControl blobControl = new(_device, port);

        // Read BLOB ID
        (bool success, BlobError? error) result = await blobControl.PrepareBlobIdAsync(token).ConfigureAwait(false);
        if (!result.success)
            return (false, result.error?.Message, null);

        // Start Blobbing
        result = await blobControl.SendBlobStartRequestAsync(token).ConfigureAwait(false);
        if (!result.success)
            return (false, result.error?.Message, null);

        // Read Blob
        DateTime startTime = DateTime.UtcNow;
        double duration = DeviceTreeBlobData.ReadBlobDuration;
        const double Frequency = DeviceTreeBlobData.ReadBlobSampleRate;
        StringBuilder sbResult = new();
        bool error = false;
        byte flowControl = 0;

        while (true)
        {
            token.ThrowIfCancellationRequested();

            string value = await blobControl.RequestDataFromBlobAsync(BlobIndex.BLOB_Channel, token).ConfigureAwait(false);

            // Get flow control stuff
            int firstByte = GetFirstByte(value);

            // Blob Beginning
            if (firstByte == (byte)BlobFunction.BLOB_Info_Read)
            {
                byte[] arrBytes = StringToByteArray(value);
                // First BLOB TODO write message
                uint blobSize = (uint)(arrBytes[1] << 24) +
                           (uint)(arrBytes[2] << 16) +
                           (uint)(arrBytes[3] << 8) +
                           (uint)(arrBytes[4] << 0);

                duration = GetDurationOfBlob(blobSize, Frequency);
                continue;
            }

            int crc = firstByte & 0x0F;

            // Append to result
            string received = RemoveFlowControlByte(value);
            sbResult.Append(received);

            // Last Segment of BLOB
            if (firstByte == (byte)BlobFunction.BLOB_Last_Segment)
            {
                logger.ReceivedWholeBlob(DateTime.UtcNow.Subtract(startTime));
                break;
            }

            // Check if segment is valid
            if (crc != flowControl)
            {
                error = true;
                break;
            }

            flowControl++;
            if (flowControl > 15)
                flowControl = 0;
        }

        byte[] rawData = StringToByteArray(sbResult.ToString());
        await blobControl.FinishBlobAsync(token).ConfigureAwait(false);

        return (!error, null, new BlobCapture(startTime, duration, Frequency, rawData));
    }

    /// <summary>
    /// Reads the flow-control byte (the first hex-encoded byte) from a BLOB segment.
    /// </summary>
    /// <param name="value">The hex-encoded segment value, as returned by the device.</param>
    /// <returns>The segment's flow-control byte.</returns>
    private static int GetFirstByte(string value)
        => byte.Parse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    /// <summary>
    /// Strips the leading flow-control byte (its first two hex characters) off a BLOB segment, leaving only
    /// its data payload.
    /// </summary>
    /// <param name="value">The hex-encoded segment value, as returned by the device.</param>
    /// <returns>The segment's hex-encoded data payload, without the flow-control byte.</returns>
    private static string RemoveFlowControlByte(string value)
        => value[2..];

    /// <summary>
    /// Estimates a BLOB's recording duration from its reported size and sample rate.
    /// </summary>
    /// <param name="blobSize">The BLOB's size in bytes, as reported by the device's info segment.</param>
    /// <param name="sampleRate">The device's fixed sample rate, in samples per second.</param>
    /// <returns>The estimated duration, in milliseconds.</returns>
    private static double GetDurationOfBlob(uint blobSize, double sampleRate)
        => blobSize / (sampleRate * 2) * 1000;

    /// <summary>
    /// Converts a hex-encoded string into its raw bytes.
    /// </summary>
    /// <param name="hex">The hex-encoded string to convert.</param>
    /// <returns>The decoded bytes.</returns>
    private static byte[] StringToByteArray(string hex)
    {
        int numberChars = hex.Length;
        byte[] bytes = new byte[numberChars / 2];
        for (int i = 0; i < numberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}
