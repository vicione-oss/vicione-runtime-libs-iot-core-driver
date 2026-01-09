using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.IOLink;

[FunctionBlockDesign("f7598b22-ff1d-4007-ab98-cc754cb13896", "0adf1b27-44af-4786-a70b-71a3419101f1", "cffa5bd4-470d-41af-9390-ff67ce105016")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class LongSubscriber : SubscriberBase<LongSubscriber>
{
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("659f86c7-0545-4580-b040-78c5c37dc139")]
        [DefaultValue("")]
        public IRuntimeConnector<string> Unit { get; internal set; }
        [OutputConnectorDesign("cc0aba8a-9035-404a-aa0a-2e1ececc8cf3")]
        public IRuntimeConnector<long> Value { get; internal set; }
        [OutputConnectorDesign("8bb6160c-3cd7-4304-a384-4e43cb2e454e")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    public override Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_ : IIoLinkSubscriberSettings
    {
        [SettingDesign("838d1ecb-ff53-481e-b1a4-ca09c7422f1e", "4f254e60-8ba6-4887-84c2-ae4cd2fd4a55", "253b6108-e886-4e59-be43-f20c67c2fa21")]
        public IRuntimeSetting<string> Address { get; internal set; }
        [SettingDesign("e5e4e36e-0b94-4a19-84bc-d8c17b6b4074", "e07ca88f-8946-4319-8a30-585d9bb6a1f7", "f26cdca5-36ce-43de-8008-6939c82cd0f9")]
        public IRuntimeSetting<string> ApplicationSpecificTag { get; internal set; }
        [SettingDesign("8c9672d5-64e7-4a1f-aa2c-c199a8dc4de9", "e6f6821a-8bc6-43c9-8c12-6b25bab84a16", "9db884ac-5d80-4a27-81f0-b8f8c45933cb")]
        public IRuntimeSetting<ushort> VendorId { get; internal set; }
        [SettingDesign("34879c30-6c45-440c-b38b-15219208ea8e", "dd9a185e-8acf-4a17-b709-d696ce2b3019", "652685be-1c74-4648-a0a8-a7ceece385b3")]
        public IRuntimeSetting<uint> DeviceId { get; internal set; }
        [SettingDesign("3a85c63f-1fff-4c89-8d77-11cb4bc1bbac", "b2800aef-8ba0-44ad-ba10-c8d70b09aa94", "720b4904-b54a-4d79-a856-7a680b2d34a9")]
        public IRuntimeSetting<string> ProductName { get; internal set; }
        [SettingDesign("63c84842-4a4e-4144-bfef-634107eebd8b", "78994054-0ded-4766-8e6a-9071421b72f5", "00384d0d-6b82-4f63-8ab9-0ae2e5c65ce0")]
        public IRuntimeSetting<ushort> PortIndex { get; internal set; }
        [SettingDesign("7221e73e-b1d1-4e04-9a10-006aa319bcef", "9f4aec43-048c-476d-bb21-dfc4ebd8ebae", "4d0a1d8b-b23b-4671-b099-9f67c22cfeeb")]
        public IRuntimeSetting<ushort> ProcessDataInIndex { get; internal set; }
    }
}
