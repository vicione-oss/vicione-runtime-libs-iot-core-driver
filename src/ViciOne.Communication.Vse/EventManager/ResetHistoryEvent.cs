using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised when a VSE device's history was reset.
/// </summary>
public sealed class ResetHistoryEvent(IVSEDevice? device) : BaseDeviceEvent(device);
