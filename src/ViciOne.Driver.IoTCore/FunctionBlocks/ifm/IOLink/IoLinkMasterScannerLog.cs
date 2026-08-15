using System;
using System.Net;
using Microsoft.Extensions.Logging;

namespace ifm.IOLink;

internal static partial class IoLinkMasterScannerLog
{
    [LoggerMessage(EventId = 5000, EventName = "ScanningErrorOccurred", Level = LogLevel.Error, Message = "An error occurred while scanning for IO-Link Masters.")]
    public static partial void ScanningErrorOccurred(this ILogger logger, Exception ex);

    [LoggerMessage(EventId = 5001, EventName = "GenericScanningWarningOccurred", Level = LogLevel.Warning, Message = "An error occurred while scanning for IO-Link Masters.")]
    public static partial void GenericScanningWarningOccurred(this ILogger logger, Exception ex);

    [LoggerMessage(EventId = 5002, EventName = "DeviceInfoFetchFailed", Level = LogLevel.Information, Message = "Fetching information for device '{DeviceAddress}' failed with status {StatusCode}.")]
    public static partial void DeviceInfoFetchFailed(this ILogger logger, string deviceAddress, HttpStatusCode? statusCode);

    [LoggerMessage(EventId = 5003, EventName = "DeviceInfoFetchErrorOccurred", Level = LogLevel.Warning, Message = "An error occurred while fetching information for device '{DeviceAddress}'.")]
    public static partial void DeviceInfoFetchErrorOccurred(this ILogger logger, Exception ex, string deviceAddress);
}
