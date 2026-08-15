using System;
using Microsoft.Extensions.Logging;

namespace ifm.Vse;

internal static partial class VseScannerLog
{
    [LoggerMessage(EventId = 13000, EventName = "ScanningErrorOccurred", Level = LogLevel.Error, Message = "An error occurred while scanning for Vse devices.")]
    public static partial void ScanningErrorOccurred(this ILogger logger, Exception ex);

    [LoggerMessage(EventId = 13001, EventName = "GenericScanningWarningOccurred", Level = LogLevel.Warning, Message = "An error occurred while scanning for Vse devices.")]
    public static partial void GenericScanningWarningOccurred(this ILogger logger, Exception ex);
}
