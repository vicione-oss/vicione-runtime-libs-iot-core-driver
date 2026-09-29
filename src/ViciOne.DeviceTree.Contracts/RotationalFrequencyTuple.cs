namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A named object's rotational frequency.
/// </summary>
public class RotationalFrequencyTuple
{
    /// <summary>
    /// The object's name.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Bezeichner enthält Typnamen", Justification = "Dictated by the contract.")]
    public string Object { get; set; } = string.Empty;

    /// <summary>
    /// The object's rotational frequency.
    /// </summary>
    public float RotationalFrequency { get; set; }
}
