using System;
using Microsoft.Extensions.Logging;

namespace ifm.Vse;

internal static partial class DeviceTreeSubscriberLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while parsing the device tree.")]
    public static partial void ParsingErrorOccurred(this ILogger logger, Exception ex);
}
