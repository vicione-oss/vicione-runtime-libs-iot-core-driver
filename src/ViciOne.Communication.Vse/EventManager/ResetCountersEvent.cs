using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised when a Vse device's counter was reset.
/// </summary>
public sealed class ResetCountersEvent(IVSEDevice? device, int counter) : BaseDeviceEvent(device)
{
    /// <summary>
    /// The counter that was reset.
    /// </summary>
    public int Counter { get; } = counter;
}
