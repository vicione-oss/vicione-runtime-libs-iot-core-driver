using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.Vse;

internal static partial class VseParserLog
{
    [LoggerMessage(EventId = 12000, EventName = "NoUnitSet", Level = LogLevel.Warning, Message = "No unit appears to be set on the Vse device-tree element '{ElementName}' of '{DeviceUri}'.")]
    public static partial void NoUnitSet(this ILogger logger, string elementName, Uri deviceUri);
    [LoggerMessage(EventId = 12001, EventName = "FailedToConnectToVse", Level = LogLevel.Warning, Message = "Failed to connect to the Vse device at '{DeviceUri}'.")]
    public static partial void FailedToConnectToVse(this ILogger logger, Uri deviceUri);
    [LoggerMessage(EventId = 12002, EventName = "ParsingVseCategoryFailed", Level = LogLevel.Warning, Message = "Parsing category '{Category}' of the Vse device at '{DeviceUri}' failed.")]
    public static partial void ParsingVseCategoryFailed(this ILogger logger, string category, Uri deviceUri, Exception exception);
}
