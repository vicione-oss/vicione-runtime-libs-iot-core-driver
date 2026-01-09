using ACX.ViciOne.Data.Driver.CloudDataAdapter.Contracts;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.IOLink;

[FunctionBlockDesign("61b0e746-9834-483b-8bbe-7ed76654c6de", "a590d4eb-2d6f-489e-9cd9-e90268e38ba8", "e6424f2b-cbfc-4cda-ba67-af1a89208c32")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class BlobSubscriber : RuntimeFunctionBlock<BlobSubscriber>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("29265909-8da8-4cf0-acb8-812d6ebf025b")]
        public IRuntimeConnector<bool> Trigger { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("a9abc094-d629-41ed-a0c7-0e3815734b45")]
        public IRuntimeConnector<Measurement> Data { get; internal set; }
    }

    public new Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_
    {
        [SettingDesign("25229950-3fd8-4a80-95b7-9d6534215de0", "70f2817f-e8dc-4ba7-82d2-1f51cc3d11e7", "ab8938f9-0574-4c58-9e49-321396c04c10")]
        public IRuntimeSetting<string> Address { get; internal set; }
        [SettingDesign("6e9eb289-1cff-499c-a070-9fe81b73562e", "f041b8ee-1a5b-4640-8ca9-f37759ca8775", "c5a7e476-3018-44c8-9e53-a4615c9b484c")]
        public IRuntimeSetting<string> ApplicationSpecificTag { get; internal set; }
        [SettingDesign("b4724efb-7f33-415f-ae9e-17e6045d41e8", "78e0c57a-a4e0-4acc-83a2-a9dc2e077896", "fefbc141-4178-44aa-8fe5-d331baeb7c10")]
        public IRuntimeSetting<ushort> VendorId { get; internal set; }
        [SettingDesign("46438cbc-46d3-4d24-8b49-c5b1188cee24", "8809dc76-076b-4472-a298-acc9ed87b884", "8809dc76-076b-4472-a298-acc9ed87b884")]
        public IRuntimeSetting<uint> DeviceId { get; internal set; }
        [SettingDesign("cb484a49-0d1c-411f-84fe-36e02f289fc4", "2222064b-ccd3-4e43-a956-1735facca843", "dac26826-eece-440e-a08f-f388ae33c5e4")]
        public IRuntimeSetting<string> ProductName { get; internal set; }
        [SettingDesign("22ee0a83-1503-4699-8a4c-c0cbd9383db5", "36d55bca-7213-458e-9d06-0d461564e107", "f7991537-b265-479e-97ff-f0515a6e76b5")]
        public IRuntimeSetting<ushort> PortIndex { get; internal set; }
        [SettingDesign("1788008d-a651-4941-92d5-22b338d37249", "552192c9-d39a-45c0-90f7-3d70c6d6b2f2", "04014ff6-03d5-427e-9fa3-967734555d8a")]
        public IRuntimeSetting<bool> IgnoreTimedValueArray { get; internal set; }
    }
}
