namespace ViciOne.Driver.IoTCore.Contracts;

public class RotationalFrequencyTuple
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Bezeichner enthält Typnamen", Justification = "Wird durch den Contract vorgegeben.")]
    public string Object { get; set; } = string.Empty;
    public float RotationalFrequency { get; set; }
}
