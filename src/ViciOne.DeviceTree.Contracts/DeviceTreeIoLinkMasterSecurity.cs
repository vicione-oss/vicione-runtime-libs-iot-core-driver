namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// Security-related information about a <see cref="DeviceTreeIoLinkMaster"/>, as reported by the master
/// itself (via its <c>GetIdentity</c> service's <c>security</c> block) - as opposed to
/// <see cref="DeviceTreeIoLinkMaster.Username"/>/<see cref="DeviceTreeIoLinkMaster.Password"/>, which are what
/// was configured to authenticate with.
/// <para>Older IoTCore firmware doesn't report a <c>security</c> block at all - every property here then just
/// keeps its default (unauthenticated-looking) value.</para>
/// </summary>
public sealed class DeviceTreeIoLinkMasterSecurity
{
    /// <summary>
    /// Whether the master reported that it requires authentication (<c>security.passwordstatus == "required"</c>),
    /// independently of whether <see cref="DeviceTreeIoLinkMaster.Username"/>/<see cref="DeviceTreeIoLinkMaster.Password"/>
    /// were actually configured.
    /// <para>Lets the engineering UI show "this master needs credentials" even when none were entered yet.</para>
    /// </summary>
    public bool RequiresAuthentication { get; set; }

    /// <summary>
    /// The authentication scheme reported by the master (<c>security.authscheme</c>), or <see langword="null"/>
    /// if not reported.
    /// <para>This driver only supports plain username/password authentication - a master reporting a different
    /// scheme here can't actually be authenticated against, even if <see cref="RequiresAuthentication"/> is
    /// <see langword="true"/>.</para>
    /// </summary>
    public string? AuthenticationScheme { get; set; }

    /// <summary>
    /// The security mode reported by the master (<c>security.securitymode</c>, e.g. <c>"enabled"</c>), or
    /// <see langword="null"/> if not reported.
    /// </summary>
    public string? SecurityMode { get; set; }
}
