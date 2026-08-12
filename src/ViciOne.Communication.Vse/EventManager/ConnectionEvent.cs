using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised when a VSE device's connection status changed.
/// </summary>
public sealed class ConnectionEvent(IVSEDevice? device, ConnectionStatus newStatus) : BaseDeviceEvent(device)
{
    /// <summary>
    /// The device's new connection status.
    /// </summary>
    public ConnectionStatus Status { get; } = newStatus;
}
