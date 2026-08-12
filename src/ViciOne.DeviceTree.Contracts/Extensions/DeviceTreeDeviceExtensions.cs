namespace ViciOne.DeviceTree.Contracts.Extensions;

/// <summary>
/// Extension members for <see cref="DeviceTreeDevice"/>.
/// </summary>
// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class DeviceTreeDeviceExtensions
{
    extension(DeviceTreeDevice device)
    {
        /// <summary>
        /// Whether this device supports BLOB-channel raw-data recording, inferred from its name containing "vvb".
        /// </summary>
        public bool IsRecordingDevice => device.Name.Contains("vvb", StringComparison.OrdinalIgnoreCase);
    }
}
