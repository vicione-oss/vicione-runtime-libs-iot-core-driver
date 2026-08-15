using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised when a Vse device's active variant changed.
/// </summary>
public sealed class VariantChangedEvent(IVSEDevice? device, IVariant variant) : BaseDeviceEvent(device)
{
    /// <summary>
    /// The device's new variant.
    /// </summary>
    public IVariant Variant { get; } = variant;
}
