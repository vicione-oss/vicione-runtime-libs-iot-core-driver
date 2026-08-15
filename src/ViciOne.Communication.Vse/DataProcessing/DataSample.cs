namespace ViciOne.Communication.Vse.DataProcessing;

/// <summary>
/// A single timestamped value sampled from a Vse data channel, with room for algorithm-specific
/// extra attributes (see <see cref="ObjectDataSample"/>).
/// </summary>
internal class DataSample<T> where T : new()
{
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// Defaults to <c>new T()</c> so the parameterless constructor (used as an empty-data fallback by the algorithms)
    /// still yields a well-defined value.
    /// </summary>
    public T Value { get; init; } = new();

    public DataSample()
    { }

    public DataSample(T value, DateTime timestamp)
    {
        Value = value;
        Timestamp = timestamp;
    }
}
