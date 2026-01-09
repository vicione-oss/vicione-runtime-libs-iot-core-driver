using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.VSE;

[FunctionBlockDesign("e20d1fa9-ec9e-46cc-b697-bd475090ec1a", "d45bc887-0a96-4201-aef6-00fc1c5911f3", "75217a92-19cc-4d2e-a6db-482d66c6007f")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class ObjectSubscriber : SubscriberBase<ObjectSubscriber>
{
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("141788da-7f2a-4a20-bcc2-02edc556d60c")]
        public IRuntimeConnector<double> Average { get; internal set; }
        [OutputConnectorDesign("34880527-8802-4213-9fce-6b21963d2168")]
        public IRuntimeConnector<double> Maximum { get; internal set; }
        [OutputConnectorDesign("bd018c63-4819-4bb8-89b3-8fad430a8325")]
        public IRuntimeConnector<double> Minimum { get; internal set; }
        [OutputConnectorDesign("de5a61ef-cadd-49a3-9203-b4293c5eb219")]
        public IRuntimeConnector<string> Unit { get; internal set; }
        [OutputConnectorDesign("f513e97c-e0ba-4d5e-9761-06b50945d114")]
        public IRuntimeConnector<double> RefValue { get; internal set; }
        [OutputConnectorDesign("c771f93f-e7e9-4551-a6aa-bd1f76488769")]
        public IRuntimeConnector<double> RotSpeed { get; internal set; }
        [OutputConnectorDesign("436f1b47-ff5d-4b9d-bfee-64d37ffeaba6")]
        public IRuntimeConnector<double> Warning { get; internal set; }
        [OutputConnectorDesign("8971b0ab-6a0a-4f44-b5d3-9fffb85ea34c")]
        public IRuntimeConnector<double> BaseLine { get; internal set; }
        [OutputConnectorDesign("51c3ca81-ee2a-49d2-92bd-beec77ddd7c5")]
        public IRuntimeConnector<double> Damage { get; internal set; }
        [OutputConnectorDesign("62add194-e0f0-4c9f-8da2-25669ba656ab")]
        public IRuntimeConnector<double> AveragingPeriod { get; internal set; }
        [OutputConnectorDesign("2befa247-00e5-4c9b-9de1-dc9e975ba018")]
        public IRuntimeConnector<string> Error { get; internal set; }
        [OutputConnectorDesign("32c15026-9b38-4064-a209-af0ad017cc5a")]
        public IRuntimeConnector<ErrorState> ErrorState { get; internal set; }
        [OutputConnectorDesign("5529fee0-3437-4adc-83b6-9332f4094096")]
        public IRuntimeConnector<RotationalFrequencyTuple> RotationalFrequencyTuple { get; internal set; }
        [OutputConnectorDesign("bb18a71b-f1a1-48ca-8e8a-e192919b0b8a")]
        public IRuntimeConnector<bool> Valid { get; internal set; }
        [OutputConnectorDesign("c38f7f40-ef59-453e-80d6-59755ad24880")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    public override Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_ : IVseSubscriberSettings
    {
        [SettingDesign("225dac1a-a373-48c2-966f-b6cf38d62792", "b2ee2fe8-43ab-44b8-bcd0-060a9eba06ec", "3fe3b34f-48a6-40a2-9d77-23ee4d8421af")]
        [DefaultValue("192.168.10.100")]
        public IRuntimeSetting<string> Address { get; internal set; }
        [SettingDesign("614a1ffe-258d-48bf-ad91-d0e1b852d24d", "690076fb-5765-4818-a2c7-0ad8ff4aeb3c", "a57b5625-440a-4f9e-9271-7f1a97493b44")]
        [DefaultValue("/Device/Objects/Object01")]
        public IRuntimeSetting<string> NodePath { get; internal set; }
    }
}
