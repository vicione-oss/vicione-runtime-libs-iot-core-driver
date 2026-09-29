using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.IoTCore;

internal static partial class RecordingManagerLog
{
    [LoggerMessage(EventId = 4000, EventName = "RecordingDataReceived", Level = LogLevel.Information, Message = "Successfully received recording data from port {DevicePort} of device '{Address}'.")]
    public static partial void RecordingDataReceived(this ILogger logger, int devicePort, string address);
    [LoggerMessage(EventId = 4001, EventName = "RecordingDataWarningOccurred", Level = LogLevel.Warning, Message = "An error occurred while downloading recording data from port {DevicePort} of device '{Address}': {ErrorMessage}.")]
    public static partial void RecordingDataWarningOccurred(this ILogger logger, int devicePort, string address, string errorMessage);
    [LoggerMessage(EventId = 4002, EventName = "RecordingDataErrorOccurred", Level = LogLevel.Error, Message = "An error occurred while downloading recording data from port {DevicePort} of device '{Address}'.")]
    public static partial void RecordingDataErrorOccurred(this ILogger logger, Exception ex, int devicePort, string address);
}
