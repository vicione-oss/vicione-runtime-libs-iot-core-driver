namespace ViciOne.DeviceTree.Contracts.Constants;

/// <summary>
/// Resolves a VSE device's address to its actual port, falling back to <see cref="VseDefaultPort"/> when none
/// was explicitly specified.
/// </summary>
public static class VseAddresses
{
    /// <summary>
    /// The default TCP port a VSE device listens on if no port is specified in its address.
    /// </summary>
    public static int VseDefaultPort { get; } = 3321;

    /// <summary>
    /// Parses <paramref name="vseAddress"/> and returns it with an explicit port, filling in
    /// <see cref="VseDefaultPort"/> if none was specified.
    /// </summary>
    public static Uri GetVseAddressWithPort(string vseAddress)
    {
        UriBuilder builder = new(vseAddress);
        builder.Port = GetVsePortFromUri(builder.Uri);
        return builder.Uri;
    }

    /// <summary>
    /// Returns <paramref name="deviceUri"/>'s port, or <see cref="VseDefaultPort"/> if none was explicitly specified.
    /// </summary>
    public static int GetVsePortFromUri(Uri deviceUri)
        => deviceUri.IsDefaultPort
            ? VseDefaultPort
            : deviceUri.Port;
}
