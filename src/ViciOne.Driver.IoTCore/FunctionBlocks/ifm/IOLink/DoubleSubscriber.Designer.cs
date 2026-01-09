using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace ifm.IOLink;

[FunctionBlockDesign("70b13cb7-af9e-42c5-bcf1-6249576326c5", "77591ef2-d135-45b8-9149-635f056a7bd6", "8a5479fe-e6a9-4ca5-8d91-da6d7c7eed57")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
[Driver<IoTCoreDriver>]
public partial class DoubleSubscriber : SubscriberBase<DoubleSubscriber>
{
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("2a16682a-aabc-4fda-abb0-87f0f15882e0")]
        [DefaultValue("")]
        public IRuntimeConnector<string> Unit { get; internal set; }
        [OutputConnectorDesign("37acbdbc-decf-470d-963b-f4c36336aee9")]
        public IRuntimeConnector<double> Value { get; internal set; }
        [OutputConnectorDesign("57a3f618-2d28-43a4-b0f4-b03aeab78e44")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    public override Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_ : IIoLinkSubscriberSettings
    {
        [SettingDesign("8418803c-e3db-413d-9c33-d0e5a7da8a74", "da48559e-eb76-4fe0-a252-63971d29edce", "fe02d7b6-b2f0-4dec-bd16-fa2fe8b45b5d")]
        public IRuntimeSetting<string> Address { get; internal set; }
        [SettingDesign("47c6e8a6-7537-4bfd-943d-204ea757e61e", "b8b604c3-32d4-4e5e-a696-35771f02c747", "6ec6bd95-1050-4c87-863e-00d7f48ad58a")]
        public IRuntimeSetting<string> ApplicationSpecificTag { get; internal set; }
        [SettingDesign("22ae0260-0325-4c79-bfaa-c8206d2dec80", "bd88c0eb-c30d-44c6-b43e-4f864972c3f1", "e65440e9-6109-487b-a1d1-a5002a9daedc")]
        public IRuntimeSetting<ushort> VendorId { get; internal set; }
        [SettingDesign("2cec8a97-b943-42b9-a722-f58223f45552", "9a40d4b6-f5ca-4fef-b623-20c8ade854a4", "725544fc-c8da-44b6-9ae1-f7045025d146")]
        public IRuntimeSetting<uint> DeviceId { get; internal set; }
        [SettingDesign("b199a673-92e3-424f-9c60-50cab0824865", "643e8822-0a9f-4be5-a5f3-5d6207846358", "46f6a26a-3b4a-434d-8725-4f07087bf6c5")]
        public IRuntimeSetting<string> ProductName { get; internal set; }
        [SettingDesign("154e2818-8646-4d75-9ee8-95c5a21dbc1c", "9d4a572f-e85c-4f38-96bc-46ce262332f3", "d604da47-6308-40cf-a772-b0b27c4b8654")]
        public IRuntimeSetting<ushort> PortIndex { get; internal set; }
        [SettingDesign("e284ee60-67d9-4d0d-ade6-e9227cbbd090", "b679536f-3a88-4980-ae2f-69385b06b7aa", "b4389493-075b-4974-bfa3-e1b87aa8c6ea")]
        public IRuntimeSetting<ushort> ProcessDataInIndex { get; internal set; }
    }
}
