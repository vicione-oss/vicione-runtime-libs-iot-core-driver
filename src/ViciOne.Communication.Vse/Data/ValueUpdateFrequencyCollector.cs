namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// Tracks how often <see cref="AddValue"/> is called, to derive a rolling incoming-value frequency
/// and the age of the last value (used by <see cref="DeviceObject"/> for its diagnostic properties).
/// </summary>
internal sealed class ValueUpdateFrequencyCollector(int timespanInMs)
{
    private DateTime _lastStatUpdateTimestamp = DateTime.Now;
    private DateTime _lastValueTimestamp = DateTime.Now;
    private int _valueCount;

    public float ValueFrequency { get; private set; }
    public uint LastDataAge => (uint)(DateTime.Now - _lastValueTimestamp).TotalMilliseconds;

    public void AddValue()
    {
        _lastValueTimestamp = DateTime.Now;
        TimeSpan elapsed = _lastValueTimestamp - _lastStatUpdateTimestamp;
        _valueCount++;

        if (elapsed.TotalMilliseconds < timespanInMs)
            return;

        ValueFrequency = 1000 * (float)(_valueCount / elapsed.TotalMilliseconds);
        _lastStatUpdateTimestamp = DateTime.Now;
        _valueCount = 1;
    }
}
