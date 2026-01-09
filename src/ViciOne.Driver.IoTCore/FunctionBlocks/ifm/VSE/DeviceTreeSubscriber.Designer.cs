using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Core.Runtime;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Driver.IoTCore.Contracts;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.BlobRecording;
using ViciOne.Driver.IoTCore;

namespace ifm.VSE;

[FunctionBlockDesign("8a3246e7-c0a1-44df-8e1e-0acdad717432", "897607f9-1f26-4b0c-ac94-980717cb4dec", "c77f0993-38ed-40e0-922c-5991debd4c2b")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class DeviceTreeSubscriber : RuntimeFunctionBlock<DeviceTreeSubscriber>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("c3f46bc9-2ce3-4169-a061-2af95dc50fc8")]
        public IRuntimeConnector<bool> Trigger { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("74010ddc-7f1d-4bf4-895f-237225a77329")]
        public IRuntimeConnector<DeviceTreeVseDevice> DeviceTree { get; internal set; }
    }

    public new Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_
    {
        [SettingDesign("64b80988-b76e-41e9-ba47-8f0884b0e90e", "5dcf4a09-b9f2-4e6a-9af2-1ac7d9f95de4", "3d9b635c-b8ad-43f0-b342-da14011719ed")]
        public IRuntimeSetting<string> Url { get; internal set; }
    }
}

