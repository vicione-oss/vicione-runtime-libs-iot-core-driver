using ACX.ViciOne.Data.Driver.CloudDataAdapter.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace ViciOne.Driver.IoTCore.BlobRecording;

/// <summary>
///     Custom Module to read the Blob
/// </summary>
internal sealed class BlobRecordingModule(IIoTCoreDevice device)
{
    private const int CharsPerValue = 4;
    private const int SignificantDigits = 6;
    private const double ScalingFactor = 125.0 / 65536.0 * 9.80665; // 125 / Math.Pow(2, 16) * 9.80665

    private readonly IIoTCoreDevice _device = device ?? throw new ArgumentNullException(nameof(device));

    /// <summary>
    ///     Starts recording and reads the Blob
    /// </summary>
    /// <param name="port"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<(bool success, BlobError? error, Measurement? measurement)> ExecuteAsync(int port, bool ignoreTimedValueArray, ILogger logger, CancellationToken token)
    {
        BlobControl blobControl = new(_device, port);

        bool error = false;
        byte flowControl = 0;

        // Read BLOB ID
        (bool success, BlobError? error) result = await blobControl.PrepareBlobIdAsync(token).ConfigureAwait(false);

        if (!result.success)
            return (false, result.error, null);

        // Start Blobbing
        result = await blobControl.SendBlobStartRequestAsync(token).ConfigureAwait(false);

        if (!result.success)
            return (false, result.error, null);

        // Read Blob

        DateTime startTime = DateTime.UtcNow;
        Measurement measurement = new()
        {
            Duration = DeviceTreeBlobData.ReadBlobDuration,
            Frequency = DeviceTreeBlobData.ReadBlobSampleRate,
            Start = startTime,
            RecordReasons = [],
        };
        StringBuilder sbResult = new();

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

                measurement.Duration = GetDurationOfBlob(blobSize, DeviceTreeBlobData.ReadBlobSampleRate);
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

        string data = sbResult.ToString();
        measurement.RawData = StringToByteArray(data);
        measurement.Values = ignoreTimedValueArray
                                ? []
                                : HexStringToTimedValues(data.ToCharArray(),
                                                        measurement.Start,
                                                        measurement.Duration / (measurement.Duration * measurement.Frequency / 1000));

        await blobControl.FinishBlobAsync(token).ConfigureAwait(false);

        return (!error, null, measurement);

        static TimedValue[] HexStringToTimedValues(char[] chars, DateTime start, double timeOffset)
        {
            double currentOffset = 0;
            var timedValues = new List<TimedValue>();

            char[] block = new char[CharsPerValue];
            for (int i = 0; i <= chars.Length - 4; i += 4)
            {
                // swap the bytes
                block[0] = chars[i + 2];
                block[1] = chars[i + 3];
                block[2] = chars[i];
                block[3] = chars[i + 1];

                timedValues.Add(new TimedValue()
                {
                    Timestamp = start.AddMilliseconds(currentOffset),
                    Value = Math.Round(Convert.ToInt16(new string(block), 16) * ScalingFactor,
                                       SignificantDigits,
                                       MidpointRounding.AwayFromZero),
                });

                currentOffset += timeOffset;
            }

            return [.. timedValues];
        }
    }

    /// <summary>
    ///     Gets the First Byte of the BLOB segment (FlowControl)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static int GetFirstByte(string value)
        => byte.Parse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    /// <summary>
    ///     Removes the first two halfbytes from the BLOB segment
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static string RemoveFlowControlByte(string value)
        => value[2..];

    /// <summary>
    ///     Calculates the estimated duration for the BLOB
    /// </summary>
    /// <param name="blobSize"></param>
    /// <param name="sampleRate"></param>
    /// <returns></returns>
    private static double GetDurationOfBlob(uint blobSize, double sampleRate)
        => blobSize / (sampleRate * 2) * 1000;

    /// <summary>
    ///     Converts a hexstring into a ByteArray
    /// </summary>
    /// <param name="hex"></param>
    /// <returns></returns>
    private static byte[] StringToByteArray(string hex)
    {
        int numberChars = hex.Length;
        byte[] bytes = new byte[numberChars / 2];
        for (int i = 0; i < numberChars; i += 2)
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        return bytes;
    }
}
