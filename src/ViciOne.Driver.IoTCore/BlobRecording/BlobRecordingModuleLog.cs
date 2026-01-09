using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Driver.IoTCore.BlobRecording;

internal static partial class BlobRecordingModuleLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Received whole BLOB in {BlobReceiveDuration}.")]
    public static partial void ReceivedWholeBlob(this ILogger logger, TimeSpan blobReceiveDuration);
}
