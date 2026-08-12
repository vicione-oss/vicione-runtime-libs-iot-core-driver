using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised when a VSE device reboots.
/// </summary>
public sealed class RebootEvent(IVSEDevice? device) : BaseDeviceEvent(device);
