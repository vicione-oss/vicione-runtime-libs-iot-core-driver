using ViciOne.Communication.Vse.Interfaces;

namespace ViciOne.Communication.Vse.DataProcessing;

/// <summary>
/// Keeps a time-windowed buffer of <see cref="DataSample{T}"/>s (samples older than <see cref="TimespanInMs"/>
/// are dropped) and evaluates named <see cref="IAlgorithm{T}"/> instances (min/max/mean, ...) over that window.
/// </summary>
internal sealed class SlidingWindowDataProcessor(uint timespanInMs) : IDataProcessor<float>, IDisposable
{
    private readonly Dictionary<string, IAlgorithm<float>> _algorithm = [];
    private readonly List<DataSample<float>> _samples = [];
    private readonly Lock _lock = new();
    private bool _disposedValue;

    public IDictionary<string, IAlgorithm<float>> Algorithm => _algorithm;
    public uint TimespanInMs { get; set; } = timespanInMs;

    public DataSample<float> GetCurrentValue() => _samples.Last();

    private void RemoveOldSamples()
    {
        if (_samples.Count == 0)
            return;

        DateTime now = DateTime.Now;
        while (_samples.First() != _samples.Last())
        {
            if ((now - _samples.First().Timestamp).TotalMilliseconds > TimespanInMs)
                _samples.RemoveAt(0);
            else
                break;
        }
    }

    public void AddAlgorithm(IAlgorithm<float> algo, string name) => _algorithm[name] = algo;

    public TResult Calculate<TResult>(string algoName) where TResult : DataSample<float>
    {
        if (_algorithm.TryGetValue(algoName, out IAlgorithm<float>? algoInstance) == false)
            return default!;

        List<DataSample<float>> data;
        lock (_lock)
        {
            RemoveOldSamples();
            data = [.. _samples];
        }

        return (TResult)algoInstance.Calculate(data);
    }

    public void AddSample(DataSample<float> sample)
    {
        lock (_lock)
        {
            _samples.Add(sample);
            RemoveOldSamples();
        }
    }

    public void AddSamples(IEnumerable<DataSample<float>> samples)
    {
        lock (_lock)
        {
            _samples.AddRange(samples);
            RemoveOldSamples();
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposedValue)
            return;

        if (disposing)
        {
            lock (_lock)
            {
                _algorithm.Clear();
                _samples.Clear();
            }
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
