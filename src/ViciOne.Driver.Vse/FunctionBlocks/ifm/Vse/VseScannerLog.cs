using System;
using Microsoft.Extensions.Logging;

namespace ifm.Vse;

internal static partial class VseScannerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while scanning for VSE devices.")]
    public static partial void ScanningErrorOccurred(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "An error occurred while scanning for VSE devices.")]
    public static partial void GenericScanningWarningOccurred(this ILogger logger, Exception ex);
}
