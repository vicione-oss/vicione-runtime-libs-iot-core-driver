using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.IoTCore.Recording;

internal static partial class BlobRecorderLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received whole BLOB in {BlobReceiveDuration}.")]
    public static partial void ReceivedWholeBlob(this ILogger logger, TimeSpan blobReceiveDuration);
}
