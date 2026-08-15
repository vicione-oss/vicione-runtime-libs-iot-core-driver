using ViciOne.Communication.Vse.Interfaces;

namespace ViciOne.Communication.Vse.DataProcessing;

/// <summary>
/// Returns the average <see cref="ObjectDataSample"/> of the window, averaging <c>Value</c>, <c>RotSpeed</c>
/// and <c>RefValue</c> independently.
/// </summary>
internal sealed class AlgorithmDeviceObjectMean : IAlgorithm<float>
{
    public DataSample<float> Calculate(List<DataSample<float>> data)
    {
        if (data.Count == 0)
            return ObjectDataSample.Empty();

        // Note: sums each attribute separately (rather than re-deriving the mean from per-sample means) to avoid
        // an extra pass over the window compared to the original algorithm.
        float sumValues = 0;
        float sumRpm = 0;
        float sumRef = 0;
        foreach (ObjectDataSample dataValue in data.Cast<ObjectDataSample>())
        {
            sumValues += dataValue.Value;
            sumRpm += dataValue.RotSpeed;
            sumRef += dataValue.RefValue;
        }

        return new ObjectDataSample(sumValues / data.Count, sumRpm / data.Count, sumRef / data.Count);
    }
}
