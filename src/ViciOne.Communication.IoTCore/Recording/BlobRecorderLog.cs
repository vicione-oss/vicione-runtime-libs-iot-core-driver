using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.IoTCore.Recording;

internal static partial class BlobRecorderLog
{
    [LoggerMessage(EventId = 7000, EventName = "ReceivedWholeBlob", Level = LogLevel.Information, Message = "Master '{MasterAddress}': received whole BLOB in {BlobReceiveDuration}.")]
    public static partial void ReceivedWholeBlob(this ILogger logger, string masterAddress, TimeSpan blobReceiveDuration);
}
