namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// Security-related information about a device, as the device itself reports it in the <c>security</c> block of its
/// <c>GetIdentity</c> response - as opposed to <see cref="DeviceTreeIoLinkMaster.Username"/>/
/// <see cref="DeviceTreeIoLinkMaster.Password"/>, which are what was configured to authenticate with.
/// <para>The same block is read both while scanning the network for devices and after mirroring one, so both a
/// <see cref="Scanning.DcpDevice"/> and a <see cref="DeviceTreeIoLinkMaster"/> carry this.</para>
/// <para>Firmware old enough to report no <c>security</c> block at all leaves every property at its default
/// (unauthenticated-looking) value.</para>
/// </summary>
public sealed class DeviceSecurity
{
    /// <summary>
    /// Whether the device reported that it requires authentication, independently of whether
    /// <see cref="DeviceTreeIoLinkMaster.Username"/>/<see cref="DeviceTreeIoLinkMaster.Password"/> were actually
    /// configured.
    /// <para>Lets the engineering UI show "this device needs credentials" even when none were entered yet.</para>
    /// </summary>
    public bool RequiresAuthentication { get; set; }

    /// <summary>
    /// The authentication scheme reported by the device (<c>security.authscheme</c>), or <see langword="null"/>
    /// if not reported.
    /// <para>This driver only supports plain username/password authentication
    /// (<c>AuthenticationRequestServiceData</c>) - a device reporting a different scheme here can't actually be
    /// authenticated against, even if <see cref="RequiresAuthentication"/> is <see langword="true"/>.</para>
    /// </summary>
    public string? AuthenticationScheme { get; set; }

    /// <summary>
    /// The security mode reported by the device (<c>security.securitymode</c>, e.g. <c>"enabled"</c>), or
    /// <see langword="null"/> if not reported.
    /// </summary>
    public string? SecurityMode { get; set; }
}
