namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class DeviceTreeDeviceExtension
{
    extension(DeviceTreeDevice device)
    {
        public bool IsRecordingDevice => device.Name.Contains("vvb", StringComparison.OrdinalIgnoreCase);
    }
}
