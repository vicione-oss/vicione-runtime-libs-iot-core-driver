using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

/// <summary>
/// Raised when a Vse device completed a self-test.
/// </summary>
public sealed class SelfTestCompletedEvent(IVSEDevice? device, SelfTestResult result) : BaseDeviceEvent(device)
{
    /// <summary>
    /// The self-test's result.
    /// </summary>
    public SelfTestResult Result { get; } = result;
}
