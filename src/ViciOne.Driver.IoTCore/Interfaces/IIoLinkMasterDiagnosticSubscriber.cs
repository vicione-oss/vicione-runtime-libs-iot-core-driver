namespace ViciOne.Driver.IoTCore.Interfaces;

/// <summary>
/// A snapshot of an IO-Link master's own diagnostic values, as read in one polling cycle.
/// <para>Each value is <see langword="null"/> when the master does not expose it, so a master model without a
/// given channel simply leaves that output untouched.</para>
/// </summary>
public readonly record struct MasterDiagnostics(
    double? Temperature,
    string TemperatureUnit,
    double? SupplyVoltage,
    string SupplyVoltageUnit,
    double? Current,
    string CurrentUnit,
    long? SupervisionStatus);

/// <summary>
/// The seam between <see cref="IoLinkMaster"/> and the master-diagnostic FunctionBlock that registers for the
/// master's own diagnostic values (<see cref="IoLinkMaster.RegisterMasterDiagnostics"/>/
/// <see cref="IoLinkMaster.UnregisterMasterDiagnostics"/>).
/// <para>Lets <see cref="IoLinkMaster"/> push read values to the FunctionBlock without depending on the
/// Engine-runtime FunctionBlock base type.</para>
/// </summary>
public interface IIoLinkMasterDiagnosticSubscriber
{
    /// <summary>
    /// Called each polling cycle (while the master is reachable) with the master's freshly read diagnostic values.
    /// </summary>
    /// <param name="diagnostics">The values read this cycle.</param>
    void SetDiagnostics(MasterDiagnostics diagnostics);
}
