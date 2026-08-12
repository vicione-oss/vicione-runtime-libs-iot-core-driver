namespace ViciOne.Driver.IoTCore;

/// <summary>
/// A snapshot of one IO-Link master port's connected device identity and availability, as last read by the
/// polling loop.
/// </summary>
internal sealed class IoLinkMasterPortDeviceInfo
{
    /// <summary>The port's element address.</summary>
    public required string Address { get; init; }

    /// <summary>Whether a device is currently connected and reporting a valid status, if known yet.</summary>
    public bool? Available { get; set; }

    /// <summary>The connected device's device ID, if known yet.</summary>
    public uint? DeviceId { get; set; }

    /// <summary>The element address to read <see cref="DeviceId"/> from.</summary>
    public required string DeviceIdAddress { get; init; }

    /// <summary>
    /// True whenever this port's availability or identity changed since the last successful full device-tree
    /// parse (starts true so the very first parse always reads every port in full).
    /// <para>Lets <c>IoLinkParser.ParseDeviceAsync</c> skip the expensive per-device detail reads (IODD data,
    /// product info, ...) for ports that stayed continuously connected, reusing the cached result from last
    /// time instead.</para>
    /// </summary>
    public bool NeedsFullRefresh { get; set; } = true;

    /// <summary>The element address to read this port's own mode from - used as a lightweight connectivity probe.</summary>
    public required string PortModeAddress { get; init; }

    /// <summary>The port's index.</summary>
    public required int PortIndex { get; init; }

    /// <summary>The connected device's vendor ID, if known yet.</summary>
    public ushort? VendorId { get; set; }

    /// <summary>The element address to read <see cref="VendorId"/> from.</summary>
    public required string VendorIdAddress { get; init; }
}
