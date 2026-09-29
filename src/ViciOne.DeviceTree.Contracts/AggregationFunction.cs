namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// How a series of values is aggregated over a compression/pooling interval.
/// </summary>
public enum AggregationFunction
{
    Avg = 0,
    Min = 1,
    Max = 2,
    Diff = 3,
    Sum = 4,
    MinMaxAvg = 5,
    Last = 6,
}
