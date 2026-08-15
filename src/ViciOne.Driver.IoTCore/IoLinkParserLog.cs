using System;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.IoTCore;

internal static partial class IoLinkParserLog
{
    [LoggerMessage(EventId = 2000, EventName = "CannotReadDataFromIoLinkMasterWhileInTimeout", Level = LogLevel.Debug, Message = "Cannot read data from address '{DataAddress}' while the device is in timeout.")]
    public static partial void CannotReadDataFromIoLinkMasterWhileInTimeout(this ILogger logger, string dataAddress);

    [LoggerMessage(EventId = 2001, EventName = "FailedToConnectToIoLinkMaster", Level = LogLevel.Warning, Message = "Failed to connect to the IO-Link Master with address '{DeviceAddress}'.")]
    public static partial void FailedToConnectToIoLinkMaster(this ILogger logger, string deviceAddress);

    [LoggerMessage(EventId = 2002, EventName = "ErrorWhileDownloadingIodd", Level = LogLevel.Error, Message = "Master '{MasterAddress}': an error occurred while downloading the IODD file for vendor ID {VendorId} and device ID {DeviceId}.")]
    public static partial void ErrorWhileDownloadingIodd(this ILogger logger, string masterAddress, ushort vendorId, uint deviceId, Exception exception);

    [LoggerMessage(EventId = 2003, EventName = "ErrorWhileLoadingIodd", Level = LogLevel.Error, Message = "Master '{MasterAddress}': an error occurred while loading the IODD file for vendor ID {VendorId} and device ID {DeviceId}.")]
    public static partial void ErrorWhileLoadingIodd(this ILogger logger, string masterAddress, ushort vendorId, uint deviceId, Exception exception);

    [LoggerMessage(EventId = 2004, EventName = "ErrorWhileParsingDevice", Level = LogLevel.Error, Message = "An error occurred while parsing the device with address '{DeviceAddress}'.")]
    public static partial void ErrorWhileParsingDevice(this ILogger logger, string deviceAddress, Exception exception);

    [LoggerMessage(EventId = 2005, EventName = "ErrorWhileReadingAndDecodingProcessData", Level = LogLevel.Error, Message = "An error occurred while reading and decoding process data for the device with address '{DeviceAddress}'.")]
    public static partial void ErrorWhileReadingAndDecodingProcessData(this ILogger logger, string deviceAddress, Exception exception);

    [LoggerMessage(EventId = 2006, EventName = "ErrorWhileReadingDataFromIoLinkMaster", Level = LogLevel.Debug, Message = "An error occurred while reading data from address '{DataAddress}'.")]
    public static partial void ErrorWhileReadingDataFromIoLinkMaster(this ILogger logger, string dataAddress, Exception exception);
}
