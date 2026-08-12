namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// Identifies one raw-data recording request on a VSE device's sensor channel, correlating a
/// <see cref="Measurement"/> result back to the subscriber FunctionBlock that requested it.
/// </summary>
public readonly record struct VseRawDataIdentifier(string Address, int SensorIndex, int Duration, int SamplingRate)
{
    /// <summary>
    /// Formats this identifier the way it's stored in <see cref="Measurement.Identifier"/>.
    /// </summary>
    public override string ToString()
        => $"//vse//{Address}//RawData//{SensorIndex}//{Duration}//{SamplingRate}";
}
