using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.IoTCore;

internal static partial class IoLinkMasterLog
{
    [LoggerMessage(EventId = 1000, EventName = "AcyclicReadFailed", Level = LogLevel.Warning, Message = "Acyclic read of the condition variable (index {Index}, subindex {SubIndex}) on '{DeviceAddress}' failed; using the default value from the IODD instead.")]
    public static partial void AcyclicReadFailed(this ILogger logger, Exception ex, ushort index, byte? subIndex, string deviceAddress);
    [LoggerMessage(EventId = 1001, EventName = "ConnectionReEstablished", Level = LogLevel.Information, Message = "The connection to master '{DeviceUri}' has been re-established.")]
    public static partial void ConnectionReEstablished(this ILogger logger, Uri deviceUri);
    [LoggerMessage(EventId = 1002, EventName = "InputProcessDataIndexUnknown", Level = LogLevel.Warning, Message = "The input process-data index {ProcessDataInIndex} is unknown for the device with vendor ID {VendorId} and device ID {DeviceId} on master '{DeviceUri}'.")]
    public static partial void InputProcessDataIndexUnknown(this ILogger logger, int processDataInIndex, ushort vendorId, uint deviceId, Uri deviceUri);
    [LoggerMessage(EventId = 1003, EventName = "MirroringCompleted", Level = LogLevel.Information, Message = "Master '{DeviceUri}' has been mirrored.")]
    public static partial void MirroringCompleted(this ILogger logger, Uri deviceUri);
    [LoggerMessage(EventId = 1004, EventName = "MirroringErrorOccurred", Level = LogLevel.Error, Message = "An error occurred while mirroring master '{DeviceUri}'.")]
    public static partial void MirroringErrorOccurred(this ILogger logger, Exception ex, Uri deviceUri);
    [LoggerMessage(EventId = 1005, EventName = "PollingErrorOccurred", Level = LogLevel.Error, Message = "An error occurred while polling master '{DeviceUri}'.")]
    public static partial void PollingErrorOccurred(this ILogger logger, Exception ex, Uri deviceUri);
    [LoggerMessage(EventId = 1006, EventName = "SecurityQueryFailed", Level = LogLevel.Warning, Message = "Could not query the security information of master '{DeviceUri}'.")]
    public static partial void SecurityQueryFailed(this ILogger logger, Exception ex, Uri deviceUri);
    [LoggerMessage(EventId = 1007, EventName = "PollingStarted", Level = LogLevel.Information, Message = "Polling of master '{DeviceUri}' has started.")]
    public static partial void PollingStarted(this ILogger logger, Uri deviceUri);
    [LoggerMessage(EventId = 1008, EventName = "PollingStopped", Level = LogLevel.Information, Message = "Polling of master '{DeviceUri}' has stopped.")]
    public static partial void PollingStopped(this ILogger logger, Uri deviceUri);
    [LoggerMessage(EventId = 1009, EventName = "UnknownDeviceFound", Level = LogLevel.Warning, Message = "Unknown device (vendor ID {VendorId}, device ID {DeviceId}) on master '{DeviceUri}'.")]
    public static partial void UnknownDeviceFound(this ILogger logger, ushort vendorId, uint deviceId, Uri deviceUri);
}
