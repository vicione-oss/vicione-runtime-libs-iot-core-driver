namespace ViciOne.Driver.IoTCore.Interfaces;

/// <summary>
/// The seam between <see cref="IoLinkMaster"/> and the concrete IO-Link FunctionBlocks that register for its
/// process data (<see cref="IoLinkMaster.RegisterProcessDataIn"/>/<see cref="IoLinkMaster.UnregisterProcessDataIn"/>).
/// <para>Lets <see cref="IoLinkMaster"/> push decoded values to a FunctionBlock without depending on the
/// Engine-runtime FunctionBlock base type.</para>
/// </summary>
public interface IIoLinkSubscriberFunctionBlock
{
    /// <summary>
    /// Called with a newly decoded process data value.
    /// </summary>
    /// <param name="value">The decoded value.</param>
    /// <param name="displayFormat">The value's IODD display format (e.g. for numeric scaling/formatting).</param>
    void SetValue(object value, string displayFormat);

    /// <summary>
    /// Called with the process data's unit of measurement, if the IODD defines one.
    /// </summary>
    /// <param name="unit">The unit text.</param>
    void SetUnit(string unit);
}
