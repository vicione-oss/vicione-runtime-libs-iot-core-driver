using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// A single analog Vse alarm, kept up to date by <see cref="AnalogAlarms"/> from the device's own monitoring
/// channel push.
/// </summary>
public sealed class AnalogAlarm(IAnalogAlarm instance)
{
    private bool _hasValue;

    public int Id => instance.Id;
    public string Name => instance.Name;
    public string Unit => instance.Unit;
    public float Value { get; private set; }

    /// <summary>
    /// Raised whenever a new sample actually changes <see cref="Value"/>.
    /// </summary>
    public event Action? Changed;

    internal void Received(IIODataSample dataSample)
    {
        float value = dataSample.GetValue();

        // Gated on _hasValue rather than comparing against Value's own default (0): the device could
        // legitimately report exactly 0 as its very first sample, which must still count as a change.
        if (_hasValue && value.IsNearlyEqualTo(Value))
            return;

        _hasValue = true;
        Value = value;
        Changed?.Invoke();
    }
}
