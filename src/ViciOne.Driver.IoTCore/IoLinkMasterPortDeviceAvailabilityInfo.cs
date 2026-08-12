namespace ViciOne.Driver.IoTCore;

/// <summary>
/// A port's connected-device identity and availability, as reported to a subscriber FunctionBlock so it can
/// tell whether it's still looking at the same, expected device.
/// </summary>
internal record IoLinkMasterPortDeviceAvailabilityInfo
{
    /// <summary>Whether a device is currently connected and reporting a valid status.</summary>
    public required bool Available { get; init; }

    /// <summary>The connected device's device ID.</summary>
    public required uint DeviceId { get; init; }

    /// <summary>The connected device's vendor ID.</summary>
    public required ushort VendorId { get; init; }
}
