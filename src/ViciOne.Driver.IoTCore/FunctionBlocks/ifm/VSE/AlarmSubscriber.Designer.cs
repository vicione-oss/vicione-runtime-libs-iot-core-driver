using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.VSE;

[FunctionBlockDesign("dbe6a560-f406-44f6-8551-d6d566cb54ed", "1db9323f-8f0d-4d0e-8ca8-f69711c84949", "fff156d5-b382-4c76-abaa-76fac593ff20")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class AlarmSubscriber : SubscriberBase<AlarmSubscriber>
{
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("bd69cffd-79af-438d-8c60-99430f44b63b")]
        public IRuntimeConnector<bool> Value { get; internal set; }
        [OutputConnectorDesign("eca1448a-0f8c-40b3-a606-77d940b2a487")]
        public IRuntimeConnector<double> NumericValue { get; internal set; }
        [OutputConnectorDesign("90f17401-b5b9-4c92-93f7-e244c0478fdb")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    public override Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_ : IVseSubscriberSettings
    {
        [SettingDesign("b1e6884a-0768-4f66-b503-8da34a645b6d", "acdd08c4-a690-412e-917f-d11e0f4a4dd6", "8c781c44-a2c1-4fb9-bbf5-7db0f4475dab")]
        [DefaultValue("192.168.10.100")]
        public IRuntimeSetting<string> Address { get; internal set; }
        [SettingDesign("080450c4-ca1f-4b27-8425-c4618ccbd7af", "4a81c2b8-8621-4442-9ce3-81bfd449af01", "ffa5fcb2-7e37-4596-9fdd-d8e021910165")]
        [DefaultValue("/Device/Alarms/Alarm01")]
        public IRuntimeSetting<string> NodePath { get; internal set; }
    }
}
