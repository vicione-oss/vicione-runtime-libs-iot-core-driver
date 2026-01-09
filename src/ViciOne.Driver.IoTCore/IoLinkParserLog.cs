using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.IoTCore;

internal static partial class IoLinkParserLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Cannot read data from address '{DataAddress}' while device is in timeout.")]
    public static partial void CannotReadDataFromIoLinkMasterWhileInTimeout(this ILogger logger, string dataAddress);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to connect to IO-Link Master with address '{DeviceAddress}'.")]
    public static partial void FailedToConnectToIoLinkMaster(this ILogger logger, string deviceAddress);

    [LoggerMessage(Level = LogLevel.Debug, Message = "An error occurred while reading data from address '{DataAddress}' (Message: {ErrorMessage}).")]
    public static partial void ErrorWhileReadingDataFromIoLinkMaster(this ILogger logger, string dataAddress, string errorMessage, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while downloading the IODD file for Vendor ID={VendorId} and Device ID {DeviceId} (Message: {ErrorMessage}).")]
    public static partial void ErrorWhileDownloadingIodd(this ILogger logger, ushort vendorId, uint deviceId, string errorMessage, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while loading the IODD file for Vendor ID={VendorId} and Device ID {DeviceId} (Message: {ErrorMessage}).")]
    public static partial void ErrorWhileLoadingIodd(this ILogger logger, ushort vendorId, uint deviceId, string errorMessage, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "An error occurred while reading and decoding process data for device with address '{deviceAddress}' (Message: {ErrorMessage}).")]
    public static partial void ErrorWhileReadingAndDecodingProcessData(this ILogger logger, string deviceAddress, string errorMessage, Exception exception);
}
