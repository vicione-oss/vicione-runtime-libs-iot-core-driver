using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.VSE;

[FunctionBlockDesign("dcffafe9-51c4-4a42-82e8-7516edf7b837", "535fbb7b-7991-46f4-8d35-d85cedcee5f3", "1eb3a2c7-3803-49f7-94c3-eb12ad108755")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class VariantsSubscriber : SubscriberBase<VariantsSubscriber>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("bf753864-91c8-46f8-80a4-45a2e646081a")]
        public IRuntimeConnector<long> ActiveVariant { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("2f5b156d-39a1-4698-af59-278c8e80d1f5")]
        public IRuntimeConnector<long> ActiveVariant { get; internal set; }
        [OutputConnectorDesign("c9fb7647-7708-44b5-9ab4-d09176f7d4e9")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    public override Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_ : IVseSubscriberSettings
    {
        [SettingDesign("53e06fe6-dd7a-4d50-968d-a14c678ce349", "4d3fa29b-c5f1-4969-aed7-60946ebfd727", "1cab48d2-6fd8-4bc2-aab8-6dbff034f4fe")]
        [DefaultValue("192.168.10.100")]
        public IRuntimeSetting<string> Address { get; internal set; }
        [SettingDesign("fdf66cf7-1f5d-422e-a790-16a24bfe7e89", "a581a0b3-9545-4250-b55d-198c3ecd26cf", "85bd1849-d6be-48b8-85c7-a89b5baaaaeb")]
        [DefaultValue("/Device/Variants")]
        public IRuntimeSetting<string> NodePath { get; internal set; }
    }
}
