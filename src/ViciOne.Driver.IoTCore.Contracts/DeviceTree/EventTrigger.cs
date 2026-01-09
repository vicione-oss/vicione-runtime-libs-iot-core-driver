namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public sealed class EventTrigger
{
    public required Guid DataGroupIdentifier { get; set; }
    public int Delay { get; set; }
    public bool Enabled { get; set; }
    public bool OnDamage { get; set; }
    public bool OnWarning { get; set; }
}
