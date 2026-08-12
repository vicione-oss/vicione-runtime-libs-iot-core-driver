using System;
using Microsoft.Extensions.Logging;

namespace ifm.IOLink;

internal static partial class DeviceTreeSubscriberLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while parsing IO-Link device.")]
    public static partial void IoLinkParseDeviceErrorOccurred(this ILogger logger, Exception ex);
}
