namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// Identifies one BLOB-channel raw-data recording request on an IO-Link device, correlating a
/// <see cref="Measurement"/> result back to the subscriber FunctionBlock that requested it.
/// </summary>
public readonly record struct IoLinkRawDataIdentifier(
    string Address,
    int Port,
    ushort VendorId,
    uint DeviceId,
    string ProductName,
    string ProcessName,
    int Duration,
    int Frequency)
{
    /// <summary>
    /// Formats this identifier the way it's stored in <see cref="Measurement.Identifier"/>.
    /// </summary>
    public override string ToString()
        => $"//iolink//{Address}//{Port}//{VendorId}//{DeviceId}//{ProductName}//{ProcessName}//{Duration}//{Frequency}";
}
