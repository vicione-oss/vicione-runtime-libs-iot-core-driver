namespace ViciOne.Driver.IoTCore.Contracts.Constants;

public static class Identifiers
{
    private const string DatabaseIdentifierSeparator = "//";

    public static string Recording { get; } = "Recording";
    public static string DeviceTypeIoLink { get; } = "iolink";
    public static string DeviceTypeVse { get; } = "vse";

    public static string GetIoLinkMasterIdentifier(string identifier, string applicationSpecificTag, int port,
        ushort vendorId, uint deviceId, string productName, string processName, string poolingMode)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeIoLink}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}{applicationSpecificTag}" +
           $"{DatabaseIdentifierSeparator}{port}" +
           $"{DatabaseIdentifierSeparator}{vendorId}" +
           $"{DatabaseIdentifierSeparator}{deviceId}" +
           $"{DatabaseIdentifierSeparator}{productName}" +
           $"{DatabaseIdentifierSeparator}{processName}" +
           $"{DatabaseIdentifierSeparator}{poolingMode}";

    public static string GetIoLinkRawDataIdentifier(string identifier, string applicationSpecificTag, int port,
        ushort vendorId, uint deviceId, string productName, string processName, int duration, int frequency)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeIoLink}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}{applicationSpecificTag}" +
           $"{DatabaseIdentifierSeparator}{port}" +
           $"{DatabaseIdentifierSeparator}{vendorId}" +
           $"{DatabaseIdentifierSeparator}{deviceId}" +
           $"{DatabaseIdentifierSeparator}{productName}" +
           $"{DatabaseIdentifierSeparator}{processName}" +
           $"{DatabaseIdentifierSeparator}{duration}" +
           $"{DatabaseIdentifierSeparator}{frequency}";

    public static string GetVseAlarmIdentifier(string identifier, string alarmName, string alarmType, string childName, string poolingMode)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Alarm" +
           $"{DatabaseIdentifierSeparator}Digital" +
           $"{DatabaseIdentifierSeparator}{alarmName}" +
           $"{DatabaseIdentifierSeparator}{alarmType}" +
           $"{DatabaseIdentifierSeparator}{childName}" +
           $"{DatabaseIdentifierSeparator}{poolingMode}";

    public static string GetVseCounterIdentifier(string identifier, string counterName, string counterType, string counterUnit, string childName, string poolingMode)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Counter" +
           $"{DatabaseIdentifierSeparator}{counterName}" +
           $"{DatabaseIdentifierSeparator}{counterType}" +
           $"{DatabaseIdentifierSeparator}{counterUnit}" +
           $"{DatabaseIdentifierSeparator}{childName}" +
           $"{DatabaseIdentifierSeparator}{poolingMode}";

    public static string GetVseInputIdentifier(string identifier, string inputType, int inputId, string inputName, string inputUnit, string childName, string poolingMode)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Input" +
           $"{DatabaseIdentifierSeparator}{inputType}" +
           $"{DatabaseIdentifierSeparator}{inputId}" +
           $"{DatabaseIdentifierSeparator}{inputName}" +
           $"{DatabaseIdentifierSeparator}{inputUnit}" +
           $"{DatabaseIdentifierSeparator}{childName}" +
           $"{DatabaseIdentifierSeparator}{poolingMode}";

    public static string GetVseObjectIdentifier(string identifier, string inputType, string inputId, string objectName, string objectId, string objectType, string unit, string childName, string poolingMode)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Object" +
           $"{DatabaseIdentifierSeparator}{inputType}" +
           $"{DatabaseIdentifierSeparator}{inputId}" +
           $"{DatabaseIdentifierSeparator}{objectName}" +
           $"{DatabaseIdentifierSeparator}{objectId}" +
           $"{DatabaseIdentifierSeparator}{objectType}" +
           $"{DatabaseIdentifierSeparator}{unit}" +
           $"{DatabaseIdentifierSeparator}{childName}" +
           $"{DatabaseIdentifierSeparator}{poolingMode}";

    public static string GetVseRawDataIdentifier(string identifier, int sensorIndex, int duration, int samplingRate)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}RawData" +
           $"{DatabaseIdentifierSeparator}{sensorIndex}" +
           $"{DatabaseIdentifierSeparator}{duration}" +
           $"{DatabaseIdentifierSeparator}{samplingRate}";

    public static string GetVseVariantIdentifier(string identifier, string poolingMode)
        => $"{DatabaseIdentifierSeparator}{DeviceTypeVse}" +
           $"{DatabaseIdentifierSeparator}{identifier}" +
           $"{DatabaseIdentifierSeparator}Variant" +
           $"{DatabaseIdentifierSeparator}{poolingMode}";
}
