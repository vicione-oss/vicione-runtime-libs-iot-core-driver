using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.IoTCore;

internal static partial class DeviceManagerLog
{
    [LoggerMessage(EventId = 3000, EventName = "IoddCatalogInitializationErrorOccurred", Level = LogLevel.Error, Message = "An error occurred while initializing the IODD manager.")]
    public static partial void IoddCatalogInitializationErrorOccurred(this ILogger logger, Exception ex);
}
