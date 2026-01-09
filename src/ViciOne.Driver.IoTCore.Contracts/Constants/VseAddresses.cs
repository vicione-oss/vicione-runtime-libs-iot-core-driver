namespace ViciOne.Driver.IoTCore.Contracts.Constants;

public static class VseAddresses
{
    public static string Alarms { get; } = "/Device/Alarms";
    public static string Counters { get; } = "/Device/Counters";
    public static string Device { get; } = "/Device";
    public static string DynamicInputs { get; } = "/Device/Inputs/Dynamic";
    public static string DynamicInputsGetData { get; } = $"/Device/Inputs/Dynamic/getdata";
    public static string ExternalInputs { get; } = "/Device/Inputs/External";
    public static string GetData { get; } = "getdata";
    public static string Objects { get; } = "/Device/Objects";
    public static string RawDataSensorClear { get; } = "/Device/RawData/data/sensor{0}/clear";
    public static string RawDataSensorGetData { get; } = "/Device/RawData/data/sensor{0}/getdata";
    public static string RawDataStartRecording { get; } = "/Device/RawData/startRecording";
    public static string Variants { get; } = "/Device/Variants";
    public static string VseInformationGetData { get; } = "/Device/Information/Device/getdata";
    public static string VseMacAddressGetData { get; } = "/Device/Information/IP/MACAddress/getdata";

    public const int VseDefaultPort = 3321;

    public static Uri GetVseAddressWithPort(string vseAddress)
    {
        UriBuilder builder = new(vseAddress);
        builder.Port = GetVsePortFromUri(builder.Uri);
        return builder.Uri;
    }

    public static int GetVsePortFromUri(Uri deviceUri)
        => deviceUri.IsDefaultPort
            ? VseDefaultPort
            : deviceUri.Port;
}
