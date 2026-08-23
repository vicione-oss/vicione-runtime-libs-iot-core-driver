using System;
using System.Text.Json.Nodes;
using ifm.Common.Variant;
using ViciOne.DeviceTree.Contracts;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// Interprets the <c>security</c> block of an IoTCore <c>GetIdentity</c> response.
/// <para>Firmware differs in both where the block sits and how it names its fields: newer firmware nests it under
/// <c>iot</c> with lowercase names (<c>passwordstatus</c>/<c>securitymode</c>/<c>authscheme</c>), while older firmware
/// puts it at the response root with camelCase ones (<c>securityMode</c>/<c>authScheme</c>) and reports
/// <c>isPasswdSet</c> instead of <c>passwordstatus</c>. Firmware old enough to report no block at all is treated as
/// unauthenticated.</para>
/// <para>The same block reaches us in two representations - the network scan parses raw JSON, a mirrored master gets
/// IoTCore variants - so both readers live here, over one shared interpretation. Keeping them together is deliberate:
/// while they were separate, a fix applied to the scanner alone left masters flagged as authentication-required in
/// the scan list but not on their own tree node.</para>
/// </summary>
internal static class IoTCoreSecurityBlock
{
    /// <summary>
    /// Reads the security block from the <c>data</c> node of a raw <c>getidentity</c> response body.
    /// </summary>
    /// <param name="data">The response body's <c>data</c> node, or <see langword="null"/> if it had none.</param>
    /// <returns>The device's security info; every value keeps its default if no security block was found.</returns>
    internal static DeviceSecurity Read(JsonNode? data)
        => Read(
            data,
            static (node, key) => node[key],
            // Not GetValue<string>(): some firmware reports isPasswdSet as a JSON bool rather than a string, and that
            // throws instead of just reading "true".
            static (node, key) => node[key] is JsonValue value
                ? value.TryGetValue(out string? text) ? text : value.ToString()
                : null);

    /// <summary>
    /// Reads the security block from the return value of a mirrored device's <c>GetIdentity</c> service.
    /// </summary>
    /// <param name="data">The service's returned object, or <see langword="null"/>.</param>
    /// <returns>The device's security info; every value keeps its default if no security block was found.</returns>
    internal static DeviceSecurity Read(VariantObject? data)
        => Read(
            data,
            static (node, key) => node.TryGetValue(key, out Variant value) ? value as VariantObject : null,
            static (node, key) => node.TryGetValue(key, out Variant value) ? value.ToString() : null);

    /// <summary>
    /// Locates the security block below <paramref name="root"/> and interprets it, independently of how the caller's
    /// nodes are represented.
    /// </summary>
    /// <typeparam name="TNode">The caller's node representation.</typeparam>
    /// <param name="root">The response body's root node - the object the <c>security</c> block sits in or under.</param>
    /// <param name="child">Returns the named child object of a node, or <see langword="null"/> if absent or not an object.</param>
    /// <param name="field">Returns the named field of a node as text, or <see langword="null"/> if absent.</param>
    private static DeviceSecurity Read<TNode>(TNode? root, Func<TNode, string, TNode?> child, Func<TNode, string, string?> field)
        where TNode : class
    {
        if (root is null)
            return new DeviceSecurity();

        TNode? security = child(root, "security") ?? (child(root, "iot") is { } iot ? child(iot, "security") : null);
        if (security is null)
            return new DeviceSecurity();

        return new DeviceSecurity
        {
            // Newer firmware states it outright; on older firmware it has to be inferred from the security mode plus
            // whether a password was actually set.
            RequiresAuthentication =
                Matches(Read("passwordstatus"), "required") ||
                (Matches(Read("securitymode", "securityMode"), "enabled") && Matches(Read("isPasswdSet"), "true")),
            AuthenticationScheme = Read("authscheme", "authScheme"),
            SecurityMode = Read("securitymode", "securityMode"),
        };

        // The first of the given spellings that the block actually carries.
        string? Read(params string[] keys)
        {
            foreach (string key in keys)
                if (field(security, key) is { } value)
                    return value;

            return null;
        }

        static bool Matches(string? value, string expected)
            => string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }
}
