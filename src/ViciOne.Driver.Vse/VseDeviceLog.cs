using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.Vse;

internal static partial class VseDeviceLog
{
    [LoggerMessage(EventId = 11000, EventName = "AddressToWriteNotFound", Level = LogLevel.Warning, Message = "The address '{VseAddressToWrite}' to write was not found in the IoTCore tree of '{DeviceAddress}'.")]
    public static partial void AddressToWriteNotFound(this ILogger logger, string vseAddressToWrite, string deviceAddress);
    [LoggerMessage(EventId = 11001, EventName = "ConnectionLost", Level = LogLevel.Information, Message = "The connection to '{DeviceAddress}' has been lost.")]
    public static partial void ConnectionLost(this ILogger logger, string deviceAddress);
    [LoggerMessage(EventId = 11002, EventName = "ConnectionReEstablished", Level = LogLevel.Information, Message = "The connection to '{DeviceAddress}' has been re-established.")]
    public static partial void ConnectionReEstablished(this ILogger logger, string deviceAddress);
    [LoggerMessage(EventId = 11003, EventName = "DeviceToWriteNotConnected", Level = LogLevel.Warning, Message = "Cannot write to device '{DeviceAddress}': it is not connected.")]
    public static partial void DeviceToWriteNotConnected(this ILogger logger, string deviceAddress);
    [LoggerMessage(EventId = 11004, EventName = "ExecutionErrorOccurred", Level = LogLevel.Error, Message = "An error occurred while executing action '{Action}' on '{DeviceAddress}'.")]
    public static partial void ExecutionErrorOccurred(this ILogger logger, Exception ex, string action, string deviceAddress);
    [LoggerMessage(EventId = 11005, EventName = "PollingStarted", Level = LogLevel.Information, Message = "Polling for '{DeviceAddress}' started.")]
    public static partial void PollingStarted(this ILogger logger, string deviceAddress);
    [LoggerMessage(EventId = 11006, EventName = "PollingStopped", Level = LogLevel.Information, Message = "Polling for '{DeviceAddress}' stopped.")]
    public static partial void PollingStopped(this ILogger logger, string deviceAddress);
    [LoggerMessage(EventId = 11007, EventName = "RawDataPullingErrorOccurred", Level = LogLevel.Error, Message = "An error occurred while pulling raw data from the Vse device at '{DeviceAddress}'.")]
    public static partial void RawDataPullingErrorOccurred(this ILogger logger, Exception ex, string deviceAddress);
    [LoggerMessage(EventId = 11008, EventName = "WritingStarted", Level = LogLevel.Information, Message = "Writing to '{DeviceAddress}' started.")]
    public static partial void WritingStarted(this ILogger logger, string deviceAddress);
    [LoggerMessage(EventId = 11009, EventName = "WritingStopped", Level = LogLevel.Information, Message = "Writing to '{DeviceAddress}' stopped.")]
    public static partial void WritingStopped(this ILogger logger, string deviceAddress);
}
