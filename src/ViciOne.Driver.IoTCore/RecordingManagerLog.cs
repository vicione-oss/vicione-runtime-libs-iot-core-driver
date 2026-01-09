using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Driver.IoTCore;

internal static partial class RecordingManagerLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Successfully received recording data from port {DevicePort} of device '{Address}'.")]
    public static partial void RecordingDataReceived(this ILogger logger, int devicePort, string address);
    [LoggerMessage(Level = LogLevel.Warning, Message = "An error occurred while downloading recording data from port {DevicePort} of device '{Address}': {ErrorMessage}.")]
    public static partial void RecordingDataWarningOccurred(this ILogger logger, int devicePort, string address, string errorMessage);
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while downloading recording data from port {DevicePort} of device '{Address}'.")]
    public static partial void RecordingDataErrorOccurred(this ILogger logger, Exception ex, int devicePort, string address);
}
