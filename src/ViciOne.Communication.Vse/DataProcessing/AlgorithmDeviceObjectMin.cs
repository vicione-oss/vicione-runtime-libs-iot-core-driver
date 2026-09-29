using ViciOne.Communication.Vse.Interfaces;

namespace ViciOne.Communication.Vse.DataProcessing;

/// <summary>
/// Returns the <see cref="ObjectDataSample"/> with the lowest value in the window.
/// </summary>
internal sealed class AlgorithmDeviceObjectMin : IAlgorithm<float>
{
    public DataSample<float> Calculate(List<DataSample<float>> data)
    {
        if (data.Count == 0)
            return ObjectDataSample.Empty();

        var min = (ObjectDataSample)data[0];
        foreach (ObjectDataSample val in data.Cast<ObjectDataSample>())
            if (val.Value < min.Value)
                min = val;

        return min;
    }
}
