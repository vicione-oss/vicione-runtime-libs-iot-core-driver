using ViciOne.Communication.Vse.Interfaces;

namespace ViciOne.Communication.Vse.DataProcessing;

/// <summary>
/// Returns the average value of the samples in the window, timestamped with the current time.
/// </summary>
internal sealed class AlgorithmMean : IAlgorithm<float>
{
    public DataSample<float> Calculate(List<DataSample<float>> data)
    {
        if (data.Count == 0)
            return new DataSample<float>();

        float value = 0;
        foreach (DataSample<float> val in data)
            value += val.Value;

        return new DataSample<float>(value / data.Count, DateTime.Now);
    }
}
