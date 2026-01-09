using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Driver.IoTCore.BlobRecording;

internal sealed class BlobControl
{
    private const ushort BlobId = 0xF000;

    private readonly IIoTCoreDevice _device;
    private readonly int _port;

    public BlobControl(IIoTCoreDevice device, int port)
        => (_device, _port) = (device, port);

    /// <summary>
    ///     Sends the Stop Command for the Blob
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task<int> StopBlobAsync(CancellationToken token)
        => await SendBlobCommandAsync(BlobCommand.BLOB_Abort, null, token).ConfigureAwait(false);

    /// <summary>
    ///     Sends the Start Command for the Blob
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task<int> StartBlobAsync(CancellationToken token)
        => await SendBlobCommandAsync(BlobCommand.BLOB_Start, BlobId, token).ConfigureAwait(false);

    /// <summary>
    ///     Sends the Finish Command for the Blob
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    internal async Task<int> FinishBlobAsync(CancellationToken token)
        => await SendBlobCommandAsync(BlobCommand.BLOB_Finish, null, token).ConfigureAwait(false);

    /// <summary>
    ///     Sends an iolwriteacyclic IoTCore command
    /// </summary>
    /// <param name="command"></param>
    /// <param name="blobId">Nullable BlobId</param>
    /// <param name="token"></param>
    /// <returns></returns>
    private async Task<int> SendBlobCommandAsync(BlobCommand command, ushort? blobId, CancellationToken token) =>
        await WriteToBlobAsync(BlobIndex.BLOB_Channel,
            (((int)command).ToString("X", CultureInfo.InvariantCulture) + blobId?.ToString("X", CultureInfo.InvariantCulture)) ?? "", token).ConfigureAwait(false);

    /// <summary>
    ///     Sends an iolwriteacyclic IoTCore command
    /// </summary>
    /// <param name="index">ByteToWrite</param>
    /// <param name="value">Command + BlobId</param>
    /// <param name="token"></param>
    /// <returns></returns>
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
    ///     Checks if the Recording is still running and tries to stop it
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
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
    ///     Checks if the Recording is still running and tries to stop it
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
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
    ///     Reads the whole Blob
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
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
    ///     Requests an iolreadacyclic from IoTCore
    /// </summary>
    /// <param name="blobIndex"></param>
    /// <param name="token"></param>
    /// <returns></returns>
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
    ///     Sends the Start Command for recording
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
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
