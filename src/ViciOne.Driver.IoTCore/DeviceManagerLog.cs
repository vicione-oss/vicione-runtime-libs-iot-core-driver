using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Driver.IoTCore;

internal static partial class DeviceManagerLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while initializing IODD manager.")]
    public static partial void IoddParserManagerInitializationErrorOccurred(this ILogger logger, Exception ex);
}
