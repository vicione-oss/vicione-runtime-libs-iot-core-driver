namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// The condition that triggered a recording, along with the measured values around that condition.
/// </summary>
public class ErrorState
{
    /// <summary>
    /// The average measured value around the trigger, if applicable.
    /// </summary>
    public double? Average { get; set; }

    /// <summary>
    /// The maximum measured value around the trigger, if applicable.
    /// </summary>
    public double? Maximum { get; set; }

    /// <summary>
    /// The minimum measured value around the trigger, if applicable.
    /// </summary>
    public double? Minimum { get; set; }

    /// <summary>
    /// The id of the object that triggered the recording, if applicable.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the object that triggered the recording, if applicable.
    /// </summary>
    public string ObjectName { get; set; } = string.Empty;

    /// <summary>
    /// The configured reference value the trigger condition was evaluated against, if applicable.
    /// </summary>
    public double? RefValue { get; set; }

    /// <summary>
    /// The rotational speed measured at the time of the trigger, if applicable.
    /// </summary>
    public double? RotSpeed { get; set; }

    /// <summary>
    /// The kind of condition that triggered the recording (e.g. "Interval", a schedule, or an event trigger).
    /// </summary>
    public string TriggerType { get; set; } = string.Empty;
}
