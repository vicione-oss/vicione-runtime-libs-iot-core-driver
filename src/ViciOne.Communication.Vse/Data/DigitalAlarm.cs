using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// A single digital (on/off) VSE alarm, kept up to date by <see cref="DigitalAlarms"/> from the VSE-SDK's own
/// monitoring channel push.
/// </summary>
public sealed class DigitalAlarm(IDigitalAlarm instance)
{
    private bool _hasValue;

    public int Id => instance.Id;
    public string Name => instance.Name;
    public string Type => ReflectionUtils.AlarmType2Str(instance.Type);
    public bool Value { get; private set; }

    /// <summary>
    /// Raised whenever the alarm transitions between active/inactive (not on every sample - <see cref="Value"/>
    /// is a discrete state, unlike e.g. <see cref="Counter.Changed"/>/<see cref="Input.Changed"/>).
    /// </summary>
    public event Action? Changed;

    internal void Received(IIODataSample dataSample)
    {
        // A NormallyClosed alarm reports "active" as anything other than 1.0; a NormallyOpen one reports
        // "active" as exactly 1.0. Comparing with a small epsilon rather than `== 1.0` since the device
        // reports this as a float.
        bool value = instance.NormallyClosed
            ? Math.Abs(dataSample.GetValue() - 1.0) > 0.0000001
            : Math.Abs(dataSample.GetValue() - 1.0) < 0.0000001;

        // Gated on _hasValue rather than comparing against Value's own default (false): the device could
        // legitimately report "inactive" as its very first sample, which must still count as a change.
        if (_hasValue && value == Value)
            return;

        _hasValue = true;
        Value = value;
        Changed?.Invoke();
    }
}
