using ViciOne.Communication.Vse.Interfaces;

namespace ViciOne.Communication.Vse.DataProcessing;

/// <summary>
/// Returns the sample with the highest value in the window.
/// </summary>
internal sealed class AlgorithmMax : IAlgorithm<float>
{
    public DataSample<float> Calculate(List<DataSample<float>> data)
    {
        if (data.Count == 0)
            return new DataSample<float>();

        DataSample<float> max = data[0];
        foreach (DataSample<float> val in data)
            if (val.Value > max.Value)
                max = val;

        return max;
    }
}
