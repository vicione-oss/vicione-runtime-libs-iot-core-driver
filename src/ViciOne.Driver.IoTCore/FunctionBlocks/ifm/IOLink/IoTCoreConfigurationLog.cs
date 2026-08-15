using System;
using Microsoft.Extensions.Logging;

namespace ifm.IOLink;

internal static partial class IoTCoreConfigurationLog
{
    [LoggerMessage(EventId = 6000, EventName = "IoLinkParseDeviceErrorOccurred", Level = LogLevel.Error, Message = "Master '{MasterAddress}': an error occurred while parsing the IO-Link device.")]
    public static partial void IoLinkParseDeviceErrorOccurred(this ILogger logger, string masterAddress, Exception ex);
}
