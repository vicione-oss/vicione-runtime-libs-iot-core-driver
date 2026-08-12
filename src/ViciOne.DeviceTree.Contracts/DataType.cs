namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree data node value's underlying data type.
/// </summary>
public enum DataType
{
    /// <summary>
    /// The data type could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// A true/false value.
    /// </summary>
    Flag,

    /// <summary>
    /// A signed whole number.
    /// </summary>
    Whole,

    /// <summary>
    /// An unsigned whole number.
    /// </summary>
    UnsignedWhole,

    /// <summary>
    /// A signed floating-point number.
    /// </summary>
    Real,

    /// <summary>
    /// Human-readable text.
    /// </summary>
    Text,

    /// <summary>
    /// A short, bounded sequence of raw bytes.
    /// </summary>
    Octets,

    /// <summary>
    /// A large binary payload, transferred out-of-band from the regular process data (see <see cref="IDeviceTreeSchedulableDataNode"/>).
    /// </summary>
    Blob,
}
