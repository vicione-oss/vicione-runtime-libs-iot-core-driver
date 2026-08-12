using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Common base for every VSE device event dispatched through <see cref="DeviceEventManager"/>.
/// </summary>
public abstract class BaseDeviceEvent(IVSEDevice? device) : IDeviceEvent
{
    /// <inheritdoc/>
    public IVSEDevice? Device { get; } = device;
}
