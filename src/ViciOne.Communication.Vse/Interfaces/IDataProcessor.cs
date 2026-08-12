using ViciOne.Communication.Vse.DataProcessing;

namespace ViciOne.Communication.Vse.Interfaces;

/// <summary>
/// Collects samples over a rolling time window and aggregates them on demand using named
/// <see cref="IAlgorithm{T}"/> implementations.
/// </summary>
internal interface IDataProcessor<T> : IDisposable where T : new()
{
    /// <summary>
    /// The size of the rolling sample window, in milliseconds.
    /// </summary>
    uint TimespanInMs { get; }

    /// <summary>
    /// The registered algorithms, keyed by name.
    /// </summary>
    IDictionary<string, IAlgorithm<T>> Algorithm { get; }

    /// <summary>
    /// Registers an algorithm under the given name.
    /// </summary>
    void AddAlgorithm(IAlgorithm<T> algo, string name);

    /// <summary>
    /// Adds a single sample to the rolling window.
    /// </summary>
    void AddSample(DataSample<T> sample);

    /// <summary>
    /// Adds multiple samples to the rolling window.
    /// </summary>
    void AddSamples(IEnumerable<DataSample<T>> sample);

    /// <summary>
    /// Runs the named algorithm over the current window.
    /// </summary>
    TResult Calculate<TResult>(string algoName) where TResult : DataSample<T>;
}
