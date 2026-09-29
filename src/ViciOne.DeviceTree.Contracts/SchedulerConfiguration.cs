namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A configured recording schedule for a data node, defined as one or more times of day per weekday.
/// </summary>
public sealed class SchedulerConfiguration
{
    /// <summary>
    /// The identifier grouping this configuration with related configurations.
    /// </summary>
    public required Guid DataGroupIdentifier { get; set; }

    /// <summary>
    /// Whether this configuration is currently active.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The configured recording times, keyed by weekday.
    /// </summary>
    public Dictionary<DayOfWeek, TimeSpan[]> Times { get; set; } = [];
}
