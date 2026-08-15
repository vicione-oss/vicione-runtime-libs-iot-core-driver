using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised when a Vse device's teach-in process changed.
/// </summary>
public sealed class TeachEvent(IVSEDevice? device, ITeachIn teachIn) : BaseDeviceEvent(device)
{
    /// <summary>
    /// The teach-in process this event pertains to.
    /// </summary>
    public ITeachIn TeachIn { get; } = teachIn;
}
