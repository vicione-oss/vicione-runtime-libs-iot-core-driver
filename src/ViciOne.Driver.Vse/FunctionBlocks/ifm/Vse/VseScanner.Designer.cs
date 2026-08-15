using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.DeviceTree.Contracts.Scanning;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Scans the local network for Vse devices.
/// </summary>
[FunctionBlockDesign("e909ab02-03d4-43a8-8af1-dfebf836c5d7", "bba9266c-ac2b-4ebc-a5cd-4bd57c68e80b", "db579e4b-ef94-4721-957d-5088dc566aca")]
[Description("Scans the local network for VSE devices.")]
public partial class VseScanner : RuntimeFunctionBlock<VseScanner>
{
    /// <summary>
    /// The FunctionBlock's process data inputs.
    /// </summary>
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();

    /// <summary>
    /// The process data inputs of <see cref="VseScanner"/>.
    /// </summary>
    [InputConnectorDesigns]
    public class Inputs_
    {
        /// <summary>
        /// Set to start a scan.
        /// </summary>
        [InputConnectorDesign("7229c094-2c07-4bc5-8a84-ab23a488684d")]
        [Description("Set to start a scan.")]
        public IRuntimeConnector<bool> Trigger { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="VseScanner"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The most recently completed scan's found devices.
        /// </summary>
        [OutputConnectorDesign("26f1db36-c96c-4515-a2b1-06d989de063c")]
        [Description("The most recently completed scan's found devices.")]
        public IRuntimeConnector<VseScanningResult> Devices { get; internal set; }
    }
}
