namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node's connection status.
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// The node's owning device is reachable and responding normally.
    /// </summary>
    Online,

    /// <summary>
    /// The node's owning device is unreachable (e.g. a network timeout).
    /// </summary>
    Offline,

    /// <summary>
    /// The node's owning device is reachable, but requires authentication that wasn't provided (or was rejected).
    /// </summary>
    AuthenticationRequired,
}
