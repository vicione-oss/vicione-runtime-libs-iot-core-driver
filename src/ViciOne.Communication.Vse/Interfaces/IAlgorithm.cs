using ViciOne.Communication.Vse.DataProcessing;

namespace ViciOne.Communication.Vse.Interfaces;

/// <summary>
/// Reduces a window of samples to a single aggregated <see cref="DataSample{T}"/>.
/// </summary>
internal interface IAlgorithm<T> where T : new()
{
    /// <summary>
    /// Aggregates <paramref name="data"/> into a single resulting sample.
    /// </summary>
    DataSample<T> Calculate(List<DataSample<T>> data);
}
