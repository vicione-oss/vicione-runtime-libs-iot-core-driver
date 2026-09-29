using System;
using Microsoft.Extensions.Logging;

namespace ifm.Vse;

internal static partial class DeviceTreeSubscriberLog
{
    [LoggerMessage(EventId = 14000, EventName = "ParsingErrorOccurred", Level = LogLevel.Error, Message = "Device '{DeviceAddress}': an error occurred while parsing the device tree.")]
    public static partial void ParsingErrorOccurred(this ILogger logger, Exception ex, string deviceAddress);
}
