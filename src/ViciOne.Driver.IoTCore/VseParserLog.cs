using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Driver.IoTCore;

internal static partial class VseParserLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "No unit appears to be set on VSE device tree element '{ElementName}' on '{DeviceUri}'.")]
    public static partial void NoUnitSet(this ILogger logger, string elementName, Uri deviceUri);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to connect to VSE device at '{DeviceUri}'.")]
    public static partial void FailedToConnectToVse(this ILogger logger, Uri deviceUri);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Parsing VSE device at '{DeviceUri}' failed: {Message}, {StackTrace}.")]
    public static partial void ParsingVseFailed(this ILogger logger, Uri deviceUri, string message, string stackTrace);
}
