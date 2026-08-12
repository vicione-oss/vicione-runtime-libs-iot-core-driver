using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised when one of a VSE device's parameters changed.
/// </summary>
public sealed class ParameterChangedEvent(IVSEDevice? device) : BaseDeviceEvent(device);
