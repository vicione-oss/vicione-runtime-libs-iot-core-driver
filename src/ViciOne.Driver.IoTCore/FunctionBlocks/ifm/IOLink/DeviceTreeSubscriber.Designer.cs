using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace ifm.IOLink;

[FunctionBlockDesign("d43d0507-5f31-4e56-aaed-4ca76a44d971", "102c8c74-6b78-48bb-ab78-c92eacb85f6a", "e34ecfd3-d1ff-431b-909d-e894343ff667")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class DeviceTreeSubscriber : RuntimeFunctionBlock<DeviceTreeSubscriber>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("0ae3d21f-0797-4949-bed9-2e885934753e")]
        public IRuntimeConnector<bool> Trigger { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("9105930c-9fb9-4a24-9db9-bebfd5ab1960")]
        public IRuntimeConnector<DeviceTreeIoLinkMaster> DeviceTree { get; internal set; }
        [OutputConnectorDesign("34e64b82-0a49-4ed6-9d97-01ab020b2ecc")]
        public IRuntimeConnector<string> IoddDirectory { get; internal set; }
    }

    public new Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_
    {
        [SettingDesign("588d363b-0823-47c9-adf7-37db039ac408", "d9247849-fe00-4abb-97db-8267ed227a3a", "4d3c8be5-e3a2-4a38-9e3e-3119c4958c8e")]
        public IRuntimeSetting<string> Url { get; internal set; }
        [SettingDesign("dc73e1e1-1588-4269-88a4-1c06ec943c30", "def4c16f-98e4-4560-b5f9-c9e180f2169f", "94a48226-e3c6-4b5b-b756-b4e7c941d5de")]
        public IRuntimeSetting<string> IoddDirectory { get; internal set; }
        [SettingDesign("b556fca2-9ede-4a4b-a853-e58cb31acfcf", "ea05ac5a-4eac-4030-af2e-09995cb8cf34", "67b40d70-dd8d-48a3-8513-327d7b71bf24")]
        public IRuntimeSetting<bool> IoddAutoDownload { get; internal set; }
    }
}
