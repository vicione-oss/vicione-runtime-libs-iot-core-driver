using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.IoTCore.Recording;

// Logging helpers for the BlobRecorder class. See BlobRecorderLog for the actual [LoggerMessage] definitions.
public sealed partial class BlobRecorder
{
    private void LogReceivedWholeBlob(ILogger logger, TimeSpan blobReceiveDuration)
        => logger.ReceivedWholeBlob(address, blobReceiveDuration);
}
