namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// The kind of subscriber FunctionBlock a process data value's <see cref="DataType"/> should be wired into when
/// generating a Dataflow, independent of which concrete FunctionBlock a consumer (e.g. the engineering UI) uses
/// for it.
/// </summary>
public enum SubscriberValueKind
{
    /// <summary>
    /// A true/false value.
    /// </summary>
    Flag,

    /// <summary>
    /// A numeric value (whole, unsigned whole, or floating-point).
    /// </summary>
    Number,

    /// <summary>
    /// A text value.
    /// </summary>
    Text,
}
