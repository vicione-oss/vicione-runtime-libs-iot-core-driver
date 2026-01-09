using Microsoft.Extensions.Logging;
using System;

namespace ifm.IOLink;

internal static partial class IoLinkMasterFinderLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while scanning for IO-Link Masters.")]
    public static partial void ScanningErrorOccurred(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "An error occurred while scanning for IO-Link Masters.")]
    public static partial void GenericScanningWarningOccurred(this ILogger logger, Exception ex);
}
