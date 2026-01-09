using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.IOLink;

[FunctionBlockDesign("1f855fea-ea87-4883-b310-a7db41cfaf50", "55d0bce1-de7c-42f1-98b0-7208519a3313", "7869a661-7d4d-43ad-89b9-fe475a98a274")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class BooleanSubscriber : SubscriberBase<BooleanSubscriber>
{
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("456cd982-76b4-406b-881e-4a2ca5587d4d")]
        [DefaultValue("")]
        public IRuntimeConnector<string> Unit { get; internal set; }
        [OutputConnectorDesign("5324fe8b-da6d-42dd-a3ad-fbce337d4ff8")]
        public IRuntimeConnector<bool> Value { get; internal set; }
        [OutputConnectorDesign("c40fd331-05ea-4a1e-a122-43cd92e8d2b5")]
        public IRuntimeConnector<double> NumericValue { get; internal set; }
        [OutputConnectorDesign("4a70addc-a543-40df-9cc8-dc3150c01cfd")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    public override Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_ : IIoLinkSubscriberSettings
    {
        [SettingDesign("1952d10d-df1d-4d78-8d62-71b38c6f13f0", "e02cd08c-17fe-4cd0-89a7-4dd887a15258", "b03da5d9-dd28-4378-849c-fdeae68576bd")]
        public IRuntimeSetting<string> Address { get; internal set; }
        [SettingDesign("8129883e-5df3-429b-8d9c-7b18f191f381", "f8e259b0-cef6-4c4d-82b2-6c8f6fa3e2db", "c3d3af24-100d-4564-91ae-2167155f12cc")]
        public IRuntimeSetting<string> ApplicationSpecificTag { get; internal set; }
        [SettingDesign("d3c2f6e2-9f4c-4153-88ef-91b33bf3326a", "4bafaaac-3fc9-4e77-957c-825bf9d5b73a", "cad4d671-9f0d-4139-b856-12026c87e453")]
        public IRuntimeSetting<ushort> VendorId { get; internal set; }
        [SettingDesign("9ce0c188-1f51-423c-9e21-de9da593dcf7", "0a9ea5b2-fc38-4c8c-9ba5-7dde68fead2d", "53812c52-abfd-4bee-bd35-edad06a0e766")]
        public IRuntimeSetting<uint> DeviceId { get; internal set; }
        [SettingDesign("06e858fb-f9ff-4b89-859b-6063898acba7", "aef878a0-bcd3-4a0a-8487-87f4f5e8afb7", "4a5c55a7-ded6-4f75-946f-a4ea73c22b41")]
        public IRuntimeSetting<string> ProductName { get; internal set; }
        [SettingDesign("8eb74be8-63a6-470c-8eca-3b8fcedcd7a7", "201762d5-d6b3-4ec7-9b0d-cd9f297193f8", "2bf240ce-0375-4af4-b332-04b36e503456")]
        public IRuntimeSetting<ushort> PortIndex { get; internal set; }
        [SettingDesign("319e6bd0-0369-4852-b5e7-e6a539e35668", "5136516b-c17f-4d12-9eff-49a42cc56e5e", "a33ed614-85f7-4ac0-9cb8-387902e66edc")]
        public IRuntimeSetting<ushort> ProcessDataInIndex { get; internal set; }
    }
}
