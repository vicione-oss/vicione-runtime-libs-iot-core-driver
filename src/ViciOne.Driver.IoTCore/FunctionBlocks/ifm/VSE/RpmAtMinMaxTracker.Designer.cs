using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;

namespace ifm.VSE;

[FunctionBlockDesign("8b01c744-167d-4ba3-af3e-66050790ea57", "584503ed-0446-4461-a53d-3493ef3ec086", "c5a218d3-c3ed-4101-8de6-d85b3647eced")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
public partial class RpmAtMinMaxTracker : RuntimeFunctionBlock<RpmAtMinMaxTracker>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("9f0d4c0c-b45e-4294-b1b0-c6f8eed0f103")]
        public IRuntimeConnector<double> Average { get; internal set; }
        [InputConnectorDesign("291d3f55-6970-4f13-9a1a-008adcc124d0")]
        public IRuntimeConnector<double> Difference { get; internal set; }
        [InputConnectorDesign("ecdac43a-8ed4-48f6-8e99-91bb85dfcd3c")]
        public IRuntimeConnector<double> Maximum { get; internal set; }
        [InputConnectorDesign("864724a2-729f-4df0-83b5-18032ca9098b")]
        public IRuntimeConnector<double> Minimum { get; internal set; }
        [InputConnectorDesign("8d4a8f9b-a901-408b-a61d-16a68fa2a78b")]
        public IRuntimeConnector<double> Sum { get; internal set; }
        [InputConnectorDesign("aaa824a8-3192-422e-9d8f-8b3c34558791")]
        public IRuntimeConnector<double> Value { get; internal set; }
        [InputConnectorDesign("6d8de9e0-a7be-4553-a04b-496eb7fe8c4d")]
        public IRuntimeConnector<double> RefValue { get; internal set; }
        [InputConnectorDesign("0d46c06e-20be-4b70-82e4-c1771fa9569f")]
        public IRuntimeConnector<double> RotSpeed { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("1aa4398b-3a45-4433-9b79-cefd281ea3b8")]
        public IRuntimeConnector<double> Average { get; internal set; }
        [OutputConnectorDesign("8e20fb20-05a4-460f-914d-9fa0abc2f394")]
        public IRuntimeConnector<double> Difference { get; internal set; }
        [OutputConnectorDesign("214febc9-7e9d-4157-846c-b668d423ecc4")]
        public IRuntimeConnector<double> Maximum { get; internal set; }
        [OutputConnectorDesign("c263ff39-962a-4ff5-942a-ecb7160696b8")]
        public IRuntimeConnector<double> Minimum { get; internal set; }
        [OutputConnectorDesign("58f91954-2e71-4d3b-ab54-0b7a12c83271")]
        public IRuntimeConnector<double> Sum { get; internal set; }
        [OutputConnectorDesign("cbdda594-4f8d-4da9-a664-9aa45001fadf")]
        public IRuntimeConnector<double> RefValueAtMaximum { get; internal set; }
        [OutputConnectorDesign("8414706a-0b94-4efc-87af-4e10ed3c1299")]
        public IRuntimeConnector<double> RefValueAtMinimum { get; internal set; }
        [OutputConnectorDesign("dfba1ff7-924b-47af-a753-63ebf973653a")]
        public IRuntimeConnector<double> RotSpeedAtMaximum { get; internal set; }
        [OutputConnectorDesign("ff567407-9948-4c4f-a86a-d775421f660d")]
        public IRuntimeConnector<double> RotSpeedAtMinimum { get; internal set; }
    }
}
