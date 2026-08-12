using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// A single VSE counter (e.g. VSE uptime, totalisator, ...), kept up to date by <see cref="Counters"/>
/// from the VSE-SDK's own monitoring channel push.
/// </summary>
public sealed class Counter(ICounter instance)
{
    private bool _hasValue;

    public int Id => instance.Id;
    public string Name => instance.Name;
    public string Type { get; } = ReflectionUtils.CounterType2Str(instance.Type);
    public string Unit => instance.Unit;
    public int Limit => instance.Limit;

    /// <summary>
    /// The id of the input this counter is derived from, if any.
    /// </summary>
    public int? InputId => instance.InputId;

    public float Value { get; private set; }
    public string State { get; private set; } = string.Empty;

    /// <summary>
    /// The timestamp of the last received sample, or null before the first sample has arrived.
    /// </summary>
    public DateTime? Timestamp { get; private set; }

    /// <summary>
    /// Whether the last received sample was reported valid by the device.
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Raised whenever a new sample actually changes <see cref="Value"/> or <see cref="State"/> - <see cref="Timestamp"/>
    /// and <see cref="IsValid"/> are updated regardless, but don't gate the event since they are metadata about the
    /// sample rather than the counter's own observable reading.
    /// </summary>
    public event Action? Changed;

    internal void Received(ICounterDataSample dataSample)
    {
        float value = dataSample.GetValue();
        string state = dataSample.Elapsed ? "Elapsed" : string.Empty;

        // Gated on _hasValue rather than comparing against Value/State's own default: the device could
        // legitimately report a value/state that happens to match those .NET defaults on its very first sample,
        // which must still count as a change (there was no prior real reading to compare against).
        bool changed = !_hasValue || !value.IsNearlyEqualTo(Value) || state != State;
        _hasValue = true;

        Value = value;
        State = state;
        Timestamp = new DateTime(dataSample.GetTimestamp());
        IsValid = dataSample.IsValid();

        if (changed)
            Changed?.Invoke();
    }
}
