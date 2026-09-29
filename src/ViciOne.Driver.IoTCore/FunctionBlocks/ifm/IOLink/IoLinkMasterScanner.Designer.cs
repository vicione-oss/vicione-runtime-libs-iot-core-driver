using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.DeviceTree.Contracts.Scanning;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.IOLink;

/// <summary>
/// Scans the local network for ifm IO-Link masters via PROFINET DCP and checks each found master's HTTP
/// reachability.
/// </summary>
[FunctionBlockDesign("87f58ff5-f65a-4572-8e44-b9afe662ef72", "bbf81971-18fb-4e9d-a95f-53844d9ba688", "4b0486b3-3887-40c5-8b10-b17c85db0d34")]
[Description("Scans the local network for ifm IO-Link masters via PROFINET DCP and checks each found master's HTTP reachability.")]
public partial class IoLinkMasterScanner : RuntimeFunctionBlock<IoLinkMasterScanner>
{
    /// <summary>
    /// The FunctionBlock's process data inputs.
    /// </summary>
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();

    /// <summary>
    /// The process data inputs of <see cref="IoLinkMasterScanner"/>.
    /// </summary>
    [InputConnectorDesigns]
    public class Inputs_
    {
        /// <summary>
        /// Set to start a scan.
        /// </summary>
        [InputConnectorDesign("8a66090b-a157-42f9-be3f-2b31f36e61df")]
        [Description("Set to start a scan.")]
        public IRuntimeConnector<bool> Trigger { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="IoLinkMasterScanner"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The most recently completed scan's found masters.
        /// </summary>
        [OutputConnectorDesign("7f0170d9-b6d0-4554-b4b6-f5fa1902394e")]
        [Description("The most recently completed scan's found masters.")]
        public IRuntimeConnector<DcpScanningResult> Devices { get; internal set; }
    }
}
