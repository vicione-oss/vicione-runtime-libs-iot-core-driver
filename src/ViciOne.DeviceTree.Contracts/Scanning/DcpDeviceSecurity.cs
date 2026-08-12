namespace ViciOne.DeviceTree.Contracts.Scanning;

/// <summary>
/// Security-related information about a <see cref="DcpDevice"/>, derived from its <c>getidentity</c>
/// response's <c>security</c> block.
/// <para>Older IoTCore firmware doesn't report a <c>security</c> block at all - every property here then just
/// keeps its default (unauthenticated-looking) value.</para>
/// </summary>
public sealed class DcpDeviceSecurity
{
    /// <summary>
    /// Whether the device reported that it requires authentication (<c>security.passwordstatus == "required"</c>).
    /// </summary>
    public bool RequiresAuthentication { get; set; }

    /// <summary>
    /// The authentication scheme reported by the device (<c>security.authscheme</c>), or <see langword="null"/>
    /// if not reported.
    /// <para>This driver's own authentication (<c>AuthenticationRequestServiceData</c>) only supports plain
    /// username/password - a device reporting a different scheme here can't actually be authenticated against,
    /// even if <see cref="RequiresAuthentication"/> is <see langword="true"/>.</para>
    /// </summary>
    public string? AuthenticationScheme { get; set; }

    /// <summary>
    /// The security mode reported by the device (<c>security.securitymode</c>, e.g. <c>"enabled"</c>), or
    /// <see langword="null"/> if not reported.
    /// </summary>
    public string? SecurityMode { get; set; }
}
