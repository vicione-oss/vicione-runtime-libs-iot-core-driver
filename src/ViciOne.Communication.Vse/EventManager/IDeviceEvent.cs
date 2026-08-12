using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

public interface IDeviceEvent
{
    /// <summary>
    /// The device that raised the event, or null for events that are not tied to a specific device (e.g. tree rebuild markers).
    /// </summary>
    IVSEDevice? Device { get; }
}
