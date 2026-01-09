namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public sealed class SchedulerConfiguration
{
    public required Guid DataGroupIdentifier { get; set; }
    public bool Enabled { get; set; }
    public Dictionary<DayOfWeek, TimeSpan[]> Times { get; set; } = [];
}
