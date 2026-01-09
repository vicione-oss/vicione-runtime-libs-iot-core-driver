using Microsoft.Extensions.Logging;
using System;

namespace ViciOne.Driver.IoTCore;

internal static partial class VseDeviceLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Address '{VseAddressToWrite}' to write was not found in IoTCore tree of '{DeviceAddress}'.")]
    public static partial void AddressToWriteNotFound(this ILogger logger, string vseAddressToWrite, string deviceAddress);
    [LoggerMessage(Level = LogLevel.Information, Message = "Connection to '{DeviceAddress}' has been lost.")]
    public static partial void ConnectionLost(this ILogger logger, string deviceAddress);
    [LoggerMessage(Level = LogLevel.Information, Message = "Connection to '{DeviceAddress}' has been re-established.")]
    public static partial void ConnectionReEstablished(this ILogger logger, string deviceAddress);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Cannot write to device: '{DeviceAddress}' is not connnected.")]
    public static partial void DeviceToWriteNotConnected(this ILogger logger, string deviceAddress);
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while executing action '{Action}' on '{DeviceAddress}'.")]
    public static partial void ExecutionErrorOccurred(this ILogger logger, Exception ex, string action, string deviceAddress);
    [LoggerMessage(Level = LogLevel.Information, Message = "Polling for '{DeviceAddress}' started.")]
    public static partial void PollingStarted(this ILogger logger, string deviceAddress);
    [LoggerMessage(Level = LogLevel.Information, Message = "Polling for '{DeviceAddress}' stopped.")]
    public static partial void PollingStopped(this ILogger logger, string deviceAddress);
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while pulling raw data from VSE at '{DeviceAddress}'.")]
    public static partial void RawDataPullingErrorOccurred(this ILogger logger, Exception ex, string deviceAddress);
    [LoggerMessage(Level = LogLevel.Information, Message = "Writing to '{DeviceAddress}' started.")]
    public static partial void WritingStarted(this ILogger logger, string deviceAddress);
    [LoggerMessage(Level = LogLevel.Information, Message = "Writing to '{DeviceAddress}' stopped.")]
    public static partial void WritingStopped(this ILogger logger, string deviceAddress);
}
