using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised when a Vse device's date/time was reset.
/// </summary>
public sealed class ResetDateTimeEvent(IVSEDevice? device, DateTime date) : BaseDeviceEvent(device)
{
    /// <summary>
    /// The date/time the device was reset to.
    /// </summary>
    public DateTime Date { get; } = date;
}
