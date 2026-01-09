namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

public static class DeviceTreeDeviceExtension
{
    public static bool IsRecordingDevice(this DeviceTreeDevice device)
        => device.Name.Contains("vvb", StringComparison.OrdinalIgnoreCase);
}
