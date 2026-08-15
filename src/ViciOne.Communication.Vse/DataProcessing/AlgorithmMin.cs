using ViciOne.Communication.Vse.Interfaces;

namespace ViciOne.Communication.Vse.DataProcessing;

/// <summary>
/// Returns the sample with the lowest value in the window.
/// </summary>
internal sealed class AlgorithmMin : IAlgorithm<float>
{
    public DataSample<float> Calculate(List<DataSample<float>> data)
    {
        if (data.Count == 0)
            return new DataSample<float>();

        DataSample<float> min = data[0];
        foreach (DataSample<float> val in data)
            if (val.Value < min.Value)
                min = val;

        return min;
    }
}
