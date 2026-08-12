using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.DeviceTree.Contracts;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.IOLink;

/// <summary>
/// Triggers and receives a BLOB-channel raw-data recording from an IO-Link device.
/// </summary>
[FunctionBlockDesign("61b0e746-9834-483b-8bbe-7ed76654c6de", "a590d4eb-2d6f-489e-9cd9-e90268e38ba8", "e6424f2b-cbfc-4cda-ba67-af1a89208c32")]
[Driver<IoTCoreDriver>]
[Description("Triggers and receives a BLOB-channel raw-data recording from an IO-Link device.")]
public partial class BlobSubscriber : RuntimeFunctionBlock<BlobSubscriber>
{
    /// <summary>
    /// The FunctionBlock's process data inputs.
    /// </summary>
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();

    /// <summary>
    /// The process data inputs of <see cref="BlobSubscriber"/>.
    /// </summary>
    [InputConnectorDesigns]
    public class Inputs_
    {
        /// <summary>
        /// Set to start a recording.
        /// </summary>
        [InputConnectorDesign("29265909-8da8-4cf0-acb8-812d6ebf025b")]
        [Description("Set to start a recording.")]
        public IRuntimeConnector<bool> Trigger { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="BlobSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The finished recording's result.
        /// </summary>
        [OutputConnectorDesign("a9abc094-d629-41ed-a0c7-0e3815734b45")]
        [Description("The finished recording's result.")]
        public IRuntimeConnector<Measurement> Data { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's configuration settings.
    /// </summary>
    public new Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="BlobSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_
    {
        /// <summary>
        /// The IO-Link master's address.
        /// </summary>
        [SettingDesign("25229950-3fd8-4a80-95b7-9d6534215de0", "70f2817f-e8dc-4ba7-82d2-1f51cc3d11e7", "ab8938f9-0574-4c58-9e49-321396c04c10")]
        [Description("The IO-Link master's address.")]
        public IRuntimeSetting<string> Address { get; internal set; }

        /// <summary>
        /// An optional free-text tag identifying this specific configuration.
        /// </summary>
        [SettingDesign("6e9eb289-1cff-499c-a070-9fe81b73562e", "f041b8ee-1a5b-4640-8ca9-f37759ca8775", "c5a7e476-3018-44c8-9e53-a4615c9b484c")]
        [Description("An optional free-text tag identifying this specific configuration.")]
        public IRuntimeSetting<string> ApplicationSpecificTag { get; internal set; }

        /// <summary>
        /// The expected connected device's vendor ID.
        /// </summary>
        [SettingDesign("b4724efb-7f33-415f-ae9e-17e6045d41e8", "78e0c57a-a4e0-4acc-83a2-a9dc2e077896", "fefbc141-4178-44aa-8fe5-d331baeb7c10")]
        [Description("The expected connected device's vendor ID.")]
        public IRuntimeSetting<ushort> VendorId { get; internal set; }

        /// <summary>
        /// The expected connected device's device ID.
        /// </summary>
        [SettingDesign("46438cbc-46d3-4d24-8b49-c5b1188cee24", "8809dc76-076b-4472-a298-acc9ed87b884", "8809dc76-076b-4472-a298-acc9ed87b884")]
        [Description("The expected connected device's device ID.")]
        public IRuntimeSetting<uint> DeviceId { get; internal set; }

        /// <summary>
        /// The expected connected device's product name.
        /// </summary>
        [SettingDesign("cb484a49-0d1c-411f-84fe-36e02f289fc4", "2222064b-ccd3-4e43-a956-1735facca843", "dac26826-eece-440e-a08f-f388ae33c5e4")]
        [Description("The expected connected device's product name.")]
        public IRuntimeSetting<string> ProductName { get; internal set; }

        /// <summary>
        /// The IO-Link master port the device is connected to.
        /// </summary>
        [SettingDesign("22ee0a83-1503-4699-8a4c-c0cbd9383db5", "36d55bca-7213-458e-9d06-0d461564e107", "f7991537-b265-479e-97ff-f0515a6e76b5")]
        [Description("The IO-Link master port the device is connected to.")]
        public IRuntimeSetting<ushort> PortIndex { get; internal set; }

        /// <summary>
        /// Whether to skip decoding the recorded raw data into timed values.
        /// </summary>
        [SettingDesign("1788008d-a651-4941-92d5-22b338d37249", "552192c9-d39a-45c0-90f7-3d70c6d6b2f2", "04014ff6-03d5-427e-9fa3-967734555d8a")]
        [Description("Whether to skip decoding the recorded raw data into timed values.")]
        public IRuntimeSetting<bool> IgnoreTimedValueArray { get; internal set; }
    }
}
