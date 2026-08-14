using System;
using Microsoft.Extensions.Logging;

namespace ifm.IOLink;

internal static partial class IoLinkMasterScannerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while scanning for IO-Link Masters.")]
    public static partial void ScanningErrorOccurred(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "An error occurred while scanning for IO-Link Masters.")]
    public static partial void GenericScanningWarningOccurred(this ILogger logger, Exception ex);
}
