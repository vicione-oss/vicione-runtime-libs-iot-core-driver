using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.DataProcessing;

internal sealed class ObjectDataSample(float value, float rotSpeed, float refValue) : DataSample<float>(value, DateTime.Now)
{
    public float RotSpeed { get; } = rotSpeed;
    public float RefValue { get; } = refValue;

    /// <summary>
    /// An all-NaN placeholder, used by the min/max/mean algorithms when the sliding window
    /// currently has no samples in it (e.g. right after startup).
    /// </summary>
    public static ObjectDataSample Empty() => new(float.NaN, float.NaN, float.NaN);

    public static List<ObjectDataSample> FromDeviceObjectDataSample(IObjectDataSample dataSample)
    {
        IReadOnlyList<float> values = dataSample.GetValues();
        IReadOnlyList<float> rotSpeeds = dataSample.GetSpeedValues();
        IReadOnlyList<float> refs = dataSample.GetReferenceValues();
        IReadOnlyList<long> timestamps = dataSample.GetTimestamps();

        List<ObjectDataSample> res = new(values.Count);

        for (int idx = 0; idx < values.Count; idx++)
        {
            float cRef = refs.Count > idx ? refs[idx] : float.NaN;

            ObjectDataSample tds = new(values[idx], rotSpeeds[idx], cRef)
            {
                Timestamp = new DateTime(timestamps[idx]),
            };

            res.Add(tds);
        }

        return res;
    }
}
