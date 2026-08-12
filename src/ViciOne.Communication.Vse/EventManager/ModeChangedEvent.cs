using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised whenever the device switches between Supervise (normal process data) and Measure (exclusive raw data recording) mode.
/// </summary>
public sealed class ModeChangedEvent(IVSEDevice? device, SystemMode mode, Origin origin) : BaseDeviceEvent(device)
{
    public SystemMode Mode { get; } = mode;
    public Origin Origin { get; } = origin;
}
