namespace ViciOne.Driver.IoTCore.Contracts;

public class ErrorState
{
    public double? Average { get; set; }
    public double? Maximum { get; set; }
    public double? Minimum { get; set; }
    public string ObjectId { get; set; } = string.Empty;
    public string ObjectName { get; set; } = string.Empty;
    public double? RefValue { get; set; }
    public double? RotSpeed { get; set; }
    public string TriggerType { get; set; } = string.Empty;
}
