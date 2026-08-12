namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A single configured condition that triggers a recording.
/// </summary>
public sealed class EventTrigger
{
    /// <summary>
    /// The identifier grouping this trigger with related configurations.
    /// </summary>
    public required Guid DataGroupIdentifier { get; set; }

    /// <summary>
    /// The delay, in seconds, after the triggering condition before the recording starts.
    /// </summary>
    public int Delay { get; set; }

    /// <summary>
    /// Whether this trigger is currently active.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Whether this trigger fires on a damage-level condition.
    /// </summary>
    public bool OnDamage { get; set; }

    /// <summary>
    /// Whether this trigger fires on a warning-level condition.
    /// </summary>
    public bool OnWarning { get; set; }
}
