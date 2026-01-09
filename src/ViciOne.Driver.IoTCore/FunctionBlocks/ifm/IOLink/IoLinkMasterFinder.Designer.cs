using System.Collections.Generic;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore.Contracts.Dcp;

namespace ifm.IOLink;

[FunctionBlockDesign("87f58ff5-f65a-4572-8e44-b9afe662ef72", "bbf81971-18fb-4e9d-a95f-53844d9ba688", "4b0486b3-3887-40c5-8b10-b17c85db0d34")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
public partial class IoLinkMasterFinder : RuntimeFunctionBlock<IoLinkMasterFinder>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("8a66090b-a157-42f9-be3f-2b31f36e61df")]
        public IRuntimeConnector<bool> Trigger { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("7f0170d9-b6d0-4554-b4b6-f5fa1902394e")]
        public IRuntimeConnector<DcpScanningResult> Devices { get; internal set; }
    }
}
