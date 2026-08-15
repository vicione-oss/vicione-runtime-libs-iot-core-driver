using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse;

/// <summary>
/// Maps Vse-SDK enums to human-readable string identifiers.
/// </summary>
internal static class ReflectionUtils
{
    private const string Unknown = "unknown";

    public static string ObjectType2Str(ObjectType oType) => oType switch
    {
        ObjectType.Unbalance => "unbalance",
        ObjectType.Bearing => "bearing",
        ObjectType.A_RMS_FD => "a_rms_fd",
        ObjectType.V_RMS_FD => "v_rms_fd",
        ObjectType.D_RMS_FD => "d_rms_fd",
        ObjectType.A_RMS_TD => "a_rms_td",
        ObjectType.V_RMS_TD => "v_rms_td",
        ObjectType.A_PEAK_TD => "a_peak_td",
        ObjectType.UpperMonitor => "uppermonitor",
        ObjectType.LowerMonitor => "lowermonitor",
        ObjectType.Other => "other",
        _ => Unknown,
    };

    public static string InType2Str(InputType iType) => iType switch
    {
        InputType.None => "none",
        InputType.Analog => "analog",
        InputType.Dynamic => "dynamic",
        InputType.External => "external",
        InputType.Digital => "digital",
        _ => Unknown,
    };

    public static string SysMode2Str(SystemMode sMode) => sMode switch
    {
        SystemMode.None => "none",
        SystemMode.SelfTest => "selftest",
        SystemMode.Setup => "setup",
        SystemMode.Startup => "startup",
        SystemMode.Supervise => "supervise",
        SystemMode.Measure => "measure",
        _ => Unknown,
    };

    public static string DeviceType2Str(DeviceType dType) => dType switch
    {
        DeviceType.VSE002 => "VSE002",
        DeviceType.VSE003 => "VSE003",
        DeviceType.VSE100 => "VSE100",
        DeviceType.VSE150 => "VSE150",
        DeviceType.VSE151 => "VSE151",
        DeviceType.VSE152 => "VSE152",
        DeviceType.VSE153 => "VSE153",
        DeviceType.VSE154 => "VSE154",
        DeviceType.VSE160 => "VSE160",
        DeviceType.VSE163 => "VSE163",
        DeviceType.VSE101 => "VSE101",
        DeviceType.VSE903 => "VSE903",
        DeviceType.VSE950 => "VSE950",
        DeviceType.VSE951 => "VSE951",
        DeviceType.VSE953 => "VSE953",
        DeviceType.VSE960 => "VSE960",
        DeviceType.VSE963 => "VSE963",
        DeviceType.VYE004 => "VYE004",
        DeviceType.VYE113 => "VYE113",
        DeviceType.VYE114 => "VYE114",
        DeviceType.VYE115 => "VYE115",
        DeviceType.VYE299 => "VYE299",
        _ => Unknown,
    };

    public static string CounterType2Str(CounterType? dType) => dType switch
    {
        CounterType.VSEUptime => "VSEUptime",
        CounterType.AnalogRuntime => "AnalogRuntime",
        CounterType.DigitalRuntime => "DigitalRuntime",
        CounterType.ObjectState => "ObjectState",
        CounterType.Totalisator => "Totalisator",
        _ => Unknown,
    };

    public static string AlarmType2Str(AlarmType? dType) => dType switch
    {
        AlarmType.Warning => "Warning",
        AlarmType.Damage => "Damage",
        AlarmType.Counter => "Counter",
        AlarmType.Selftest => "Selftest",
        AlarmType.Custom => "Custom",
        AlarmType.Analog => "Analog",
        _ => "None",
    };
}
