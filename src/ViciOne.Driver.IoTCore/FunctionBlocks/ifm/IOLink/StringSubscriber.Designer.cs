using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.IOLink;

[FunctionBlockDesign("23213725-a157-4a4b-93c0-9bc0642e4944", "284190c9-8056-4a57-8335-b933332d30e6", "185dc596-d01e-4c5c-afc7-00916fe59ce4")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class StringSubscriber : SubscriberBase<StringSubscriber>
{
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("1b3df6f5-10df-4fed-b018-7d1b503b7e7d")]
        [DefaultValue("")]
        public IRuntimeConnector<string> Unit { get; internal set; }
        [OutputConnectorDesign("928ef1a8-fa95-47e1-8815-4a200ddd49df")]
        public IRuntimeConnector<string> Value { get; internal set; }
        [OutputConnectorDesign("4a0a04fb-8c2e-4aed-ac13-0beb767e4f1f")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    public override Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_ : IIoLinkSubscriberSettings
    {
        [SettingDesign("9f4fcc89-cc14-4180-b334-eac411ae1cbd", "b98cbd16-1bfa-4d33-9a70-d37c922ba3ee", "e9f36db9-0708-4a31-a7a3-c5e47dcc156b")]
        public IRuntimeSetting<string> Address { get; internal set; }
        [SettingDesign("56cb2ec6-d362-466c-a445-c60c815d75a0", "a58afc0e-ff2b-42ee-974a-dea70cc5efc6", "4511a8c4-74ce-4c36-84cc-af4707c0921f")]
        public IRuntimeSetting<string> ApplicationSpecificTag { get; internal set; }
        [SettingDesign("d73c2bf5-3a09-478c-b2f0-ba5e4ff5ad5b", "c93a42cb-89a3-41d4-a609-869d00c03f02", "9ed12cf5-1c7a-4954-b809-e8d217129b6a")]
        public IRuntimeSetting<ushort> VendorId { get; internal set; }
        [SettingDesign("4ce703e9-6199-4837-afc5-1c49f76a8767", "72548f40-0996-448d-83fb-1002bfe1bca9", "47aa106b-9bfc-49d9-9690-a3af651c1978")]
        public IRuntimeSetting<uint> DeviceId { get; internal set; }
        [SettingDesign("80a81f61-8f2b-4a75-b2bf-dd917bccc3b7", "16094400-e96f-43ab-ab93-139c2fe7b8be", "4ba603d5-ae5e-4158-b634-2cfd665e4842")]
        public IRuntimeSetting<string> ProductName { get; internal set; }
        [SettingDesign("a9813049-46e9-4d78-a5e2-d9fe4bd97c95", "684faf23-a599-4640-a2de-46ca9f4e9f9c", "ccd1d190-1cc7-42a8-b674-570a127dca99")]
        public IRuntimeSetting<ushort> PortIndex { get; internal set; }
        [SettingDesign("439567dc-06cd-4b5a-80a1-cd1a49c2174b", "7f12406c-7264-41ac-9011-08054c875765", "260a9d72-0464-45e1-9af1-eeb22b87e288")]
        public IRuntimeSetting<ushort> ProcessDataInIndex { get; internal set; }
    }
}
