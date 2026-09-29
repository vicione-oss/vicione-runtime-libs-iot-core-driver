namespace ViciOne.Driver.Vse.Interfaces;

/// <summary>
/// The seam between <see cref="VseDevice"/> and the concrete Vse FunctionBlocks that register for its data
/// (<see cref="VseDevice.RegisterAddress"/>/<see cref="VseDevice.UnregisterAddress"/>).
/// <para>Lets <see cref="VseDevice"/> push newly received values to a FunctionBlock without depending on the
/// Engine-runtime FunctionBlock base type.</para>
/// </summary>
public interface IVseSubscriberFunctionBlock
{
    /// <summary>
    /// Called with a newly received value for the registered node path.
    /// </summary>
    /// <param name="item">The received value.</param>
    void SetValue(object item);
}
