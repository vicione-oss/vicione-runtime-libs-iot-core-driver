using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.IoTCore;

internal static partial class IoLinkMasterLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Connection to '{DeviceUri}' has been re-established.")]
    public static partial void ConnectionReEstablished(this ILogger logger, Uri deviceUri);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Input process data index {ProcessDataInIndex} unknown for device with vendor ID {VendorId} and device ID {DeviceId} on remote device '{DeviceUri}'.")]
    public static partial void InputProcessDataIndexUnknown(this ILogger logger, int processDataInIndex, ushort vendorId, uint deviceId, Uri deviceUri);
    [LoggerMessage(Level = LogLevel.Information, Message = "Remote device '{DeviceUri}' is mirrored.")]
    public static partial void MirroringCompleted(this ILogger logger, Uri deviceUri);
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while mirroring remote device '{DeviceUri}'.")]
    public static partial void MirroringErrorOccurred(this ILogger logger, Exception ex, Uri deviceUri);
    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while polling of '{DeviceUri}'.")]
    public static partial void PollingErrorOccurred(this ILogger logger, Exception ex, Uri deviceUri);
    [LoggerMessage(Level = LogLevel.Information, Message = "Polling for '{DeviceUri}' started.")]
    public static partial void PollingStarted(this ILogger logger, Uri deviceUri);
    [LoggerMessage(Level = LogLevel.Information, Message = "Polling for '{DeviceUri}' stopped.")]
    public static partial void PollingStopped(this ILogger logger, Uri deviceUri);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Unknown device with vendor Id '{VendorId}' and device ID '{DeviceId}' on remote device '{DeviceUri}'.")]
    public static partial void UnknownDeviceFound(this ILogger logger, ushort vendorId, uint deviceId, Uri deviceUri);
}
