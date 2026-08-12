using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.IoTCore;

internal static partial class DeviceManagerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while initializing IODD manager.")]
    public static partial void IoddCatalogInitializationErrorOccurred(this ILogger logger, Exception ex);
}
