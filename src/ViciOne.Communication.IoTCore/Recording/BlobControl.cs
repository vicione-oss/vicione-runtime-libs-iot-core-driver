using System.Globalization;
using System.Net;

namespace ViciOne.Communication.IoTCore.Recording;

/// <summary>
/// Drives the low-level BLOB-channel wire protocol (start/stop/read/write commands over iolwriteacyclic/
/// iolreadacyclic) for a single IO-Link master port.
/// </summary>
internal sealed class BlobControl
{
    private const ushort BlobId = 0xF000;

    private readonly IBlobHttpClient _device;
    private readonly int _port;

    public BlobControl(IBlobHttpClient device, int port)
        => (_device, _port) = (device, port);

    /// <summary>
    /// Sends the Abort command, stopping any recording currently in progress on this port.
    /// </summary>
    /// <param name="token">Cancellation token for the underlying HTTP request.</param>
    /// <returns>The response code reported by the device.</returns>
    private async Task<int> StopBlobAsync(CancellationToken token)
        => await SendBlobCommandAsync(BlobCommand.BLOB_Abort, null, token).ConfigureAwait(false);

    /// <summary>
    /// Sends the Start command, beginning a new recording on this port.
    /// </summary>
    /// <param name="token">Cancellation token for the underlying HTTP request.</param>
    /// <returns>The response code reported by the device.</returns>
    private async Task<int> StartBlobAsync(CancellationToken token)
        => await SendBlobCommandAsync(BlobCommand.BLOB_Start, BlobId, token).ConfigureAwait(false);

    /// <summary>
    /// Sends the Finish command, ending the current recording on this port.
    /// </summary>
    /// <param name="token">Cancellation token for the underlying HTTP request.</param>
    /// <returns>The response code reported by the device.</returns>
    internal async Task<int> FinishBlobAsync(CancellationToken token)
        => await SendBlobCommandAsync(BlobCommand.BLOB_Finish, null, token).ConfigureAwait(false);

    /// <summary>
    /// Builds the hex-encoded command (+ optional blob ID) payload and writes it to the BLOB channel.
    /// </summary>
    /// <param name="command">The BLOB command to send.</param>
    /// <param name="blobId">The blob ID to append after the command, or <see langword="null"/> to omit it.</param>
    /// <param name="token">Cancellation token for the underlying HTTP request.</param>
    /// <returns>The response code reported by the device.</returns>
    private async Task<int> SendBlobCommandAsync(BlobCommand command, ushort? blobId, CancellationToken token) =>
        await WriteToBlobAsync(BlobIndex.BLOB_Channel,
            (((int)command).ToString("X", CultureInfo.InvariantCulture) + blobId?.ToString("X", CultureInfo.InvariantCulture)) ?? "", token).ConfigureAwait(false);

    /// <summary>
    /// Writes <paramref name="value"/> to <paramref name="index"/> on the device's BLOB channel via an
    /// iolwriteacyclic request.
    /// </summary>
    /// <param name="index">The BLOB channel index to write to.</param>
    /// <param name="value">The hex-encoded payload to write.</param>
    /// <param name="token">Cancellation token for the underlying HTTP request.</param>
    /// <returns>The response code reported by the device.</returns>
    /// <exception cref="InvalidOperationException">The device did not return a response.</exception>
    private async Task<int> WriteToBlobAsync(BlobIndex index, string value, CancellationToken token)
    {
        var data = new
        {
            index = (int)index,
            subindex = 0,
            value
        };

        // Write start "Read Blob" command to IO-Link
        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "")
            .AddIotCoreSingleRequest($"/iolinkmaster/port[{_port}]/iolinkdevice/iolwriteacyclic", data);

        string content = await _device.SendHttpRequestAsync(request, token).ConfigureAwait(false);
        return content is null
            ? throw new InvalidOperationException("Could not connect to Blob.")
            : content.GetJsonPropertyValue<int>("code");
    }

    /// <summary>
    /// Ensures the device's BLOB channel is idle before a new recording starts.
    /// <para>Polls the BLOB ID (<see cref="CheckBlobIdAsync"/>) and turns any failure into a
    /// <see cref="BlobError"/> result instead of letting the exception propagate.</para>
    /// </summary>
    /// <param name="token">Cancellation token for the underlying HTTP requests.</param>
    /// <returns><see langword="true"/> with no error if the channel is ready; otherwise <see langword="false"/> with a <see cref="BlobError"/> describing why.</returns>
    internal async Task<(bool success, BlobError? error)> PrepareBlobIdAsync(CancellationToken token)
    {
        try
        {
            await CheckBlobIdAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return (false, new BlobError(BlobErrorState.NoConnection, $"Could not read Blob ID: {ex.Message}", ex));
        }

        return (true, null);
    }

    /// <summary>
    /// Polls the BLOB ID up to 5 times.
    /// <para>If it ever comes back non-zero (a stale recording left over from a previous session), issues a
    /// Stop command and tries again.</para>
    /// </summary>
    /// <param name="token">Cancellation token for the underlying HTTP requests.</param>
    private async Task CheckBlobIdAsync(CancellationToken token)
    {
        for (int i = 0; i < 5; i++)
        {
            string readBlobIdResult = await RequestDataFromBlobAsync(BlobIndex.BLOB_Id, token).ConfigureAwait(false);
            short result = short.Parse(readBlobIdResult, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            const int BLOB_ID_OK = 0;

            switch (result)
            {
                case BLOB_ID_OK:
                    i = 5;
                    continue;
                default:
                    await StopBlobAsync(token).ConfigureAwait(false);
                    break;
            }
        }
    }

    /// <summary>
    /// Drains any leftover data from the BLOB channel, one segment at a time, until the last segment or a CRC
    /// marker is seen.
    /// <para>Lets a fresh recording start from a clean channel after a failed stop/restart.</para>
    /// </summary>
    /// <param name="token">Cancellation token for the underlying HTTP requests.</param>
    private async Task EmptyWholeBlobManuallyAsync(CancellationToken token)
    {
        int requestCounter = 0;
        byte crc;
        do
        {
            requestCounter++;
            string value = await RequestDataFromBlobAsync(BlobIndex.BLOB_Channel, token).ConfigureAwait(false);
            crc = byte.Parse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        } while (crc is not (((byte)BlobFunction.BLOB_Last_Segment) or ((byte)BlobFunction.BLOB_CRC)));
    }

    /// <summary>
    /// Describes <paramref name="blobResult"/> as a human-readable error - the well-known IO-Link error code,
    /// or otherwise its value interpreted as an HTTP status code.
    /// </summary>
    /// <param name="blobResult">The raw response code to describe.</param>
    /// <returns>A short, human-readable description of <paramref name="blobResult"/>.</returns>
    private static string GetError(int blobResult)
    {
        const int IOLINKERROR = 531;

        return blobResult switch
        {
            IOLINKERROR => $"IO-Link Error ({531})",
            _ => ((HttpStatusCode)blobResult).ToString(),
        };
    }

    /// <summary>
    /// Reads the current value of <paramref name="blobIndex"/> from the device's BLOB channel via an
    /// iolreadacyclic request.
    /// </summary>
    /// <param name="blobIndex">The BLOB channel index to read.</param>
    /// <param name="token">Cancellation token for the underlying HTTP request.</param>
    /// <returns>The hex-encoded value reported by the device.</returns>
    /// <exception cref="InvalidOperationException">The device did not return a value.</exception>
    internal async Task<string> RequestDataFromBlobAsync(BlobIndex blobIndex, CancellationToken token)
    {
        var readData = new
        {
            index = (int)blobIndex,
            subindex = 0
        };

        using HttpRequestMessage requestBlob = new HttpRequestMessage(HttpMethod.Post, "")
            .AddIotCoreSingleRequest($"/iolinkmaster/port[{_port}]/iolinkdevice/iolreadacyclic", readData);
        string contentBlob = await _device.SendHttpRequestAsync(requestBlob, token);
        string? value = contentBlob.GetJsonPropertyValueAsString("data", "value");

        return value ?? throw new InvalidOperationException("Could not get data from response.");
    }

    /// <summary>
    /// Starts a new recording.
    /// <para>Retries through stop/drain/restart if the device reports it's still busy with a previous one.</para>
    /// </summary>
    /// <param name="token">Cancellation token for the underlying HTTP requests.</param>
    /// <returns><see langword="true"/> with no error if the recording started; otherwise <see langword="false"/> with a <see cref="BlobError"/> describing why.</returns>
    internal async Task<(bool success, BlobError? error)> SendBlobStartRequestAsync(CancellationToken token)
    {
        int startBlobResult;
        const int OK = 200;

        try
        {
            startBlobResult = await StartBlobAsync(token).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return (false,
                new BlobError(BlobErrorState.CouldNotStartBlob, "Could not start recording: No connection."));
        }

        switch (startBlobResult)
        {
            case OK:
                break;
            default:
                // Try to stop Blob
                startBlobResult = await StopBlobAsync(token).ConfigureAwait(false);

                if (startBlobResult != OK)
                {
                    await EmptyWholeBlobManuallyAsync(token).ConfigureAwait(false);

                    startBlobResult = await StartBlobAsync(token).ConfigureAwait(false);

                    if (startBlobResult != OK)
                        return (false, new BlobError(startBlobResult,
                            $"Could not start recording, returned {startBlobResult} when trying to stop.", new
                            {
                                startBlobResult,
                                error = GetError(startBlobResult)
                            }));
                }

                try
                {
                    startBlobResult = await StartBlobAsync(token).ConfigureAwait(false);
                }
                catch (InvalidOperationException)
                {
                    return (false, new BlobError(BlobErrorState.CouldNotStartBlob,
                        "Could not start recording: No connection"));
                }

                if (startBlobResult != OK)
                    return (false, new BlobError(startBlobResult,
                        $"Could not start recording, returned {startBlobResult} when trying to restart.", new
                        {
                            startBlobResult,
                            error = GetError(startBlobResult)
                        }));

                break;
        }

        return (true, null);
    }
}
