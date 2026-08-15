using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Interfaces;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.IOLink;

/// <summary>
/// Subscribes to an IO-Link device's boolean process-data-in value.
/// </summary>
[FunctionBlockDesign("1f855fea-ea87-4883-b310-a7db41cfaf50", "55d0bce1-de7c-42f1-98b0-7208519a3313", "7869a661-7d4d-43ad-89b9-fe475a98a274")]
[Driver<IoTCoreDriver>]
[Description("Subscribes to an IO-Link device's boolean process-data-in value.")]
public partial class BooleanSubscriber : SubscriberBase<BooleanSubscriber>
{
    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="BooleanSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The subscribed value's unit.
        /// </summary>
        [OutputConnectorDesign("456cd982-76b4-406b-881e-4a2ca5587d4d")]
        [Description("The subscribed value's unit.")]
        [DefaultValue("")]
        public IRuntimeConnector<string> Unit { get; internal set; }

        /// <summary>
        /// The subscribed boolean value.
        /// </summary>
        [OutputConnectorDesign("5324fe8b-da6d-42dd-a3ad-fbce337d4ff8")]
        [Description("The subscribed boolean value.")]
        public IRuntimeConnector<bool> Value { get; internal set; }

        /// <summary>
        /// <see cref="Value"/> as a number (0 or 1).
        /// </summary>
        [OutputConnectorDesign("c40fd331-05ea-4a1e-a122-43cd92e8d2b5")]
        [Description("Value as a number (0 or 1).")]
        public IRuntimeConnector<double> NumericValue { get; internal set; }

        /// <summary>
        /// Whether the subscribed device is currently available.
        /// </summary>
        [OutputConnectorDesign("4a70addc-a543-40df-9cc8-dc3150c01cfd")]
        [Description("Whether the subscribed device is currently available.")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    /// <inheritdoc/>
    public override Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="BooleanSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_ : IIoLinkSubscriberSettings
    {
        /// <inheritdoc/>
        [SettingDesign("1952d10d-df1d-4d78-8d62-71b38c6f13f0", "e02cd08c-17fe-4cd0-89a7-4dd887a15258", "b03da5d9-dd28-4378-849c-fdeae68576bd")]
        [Description("The identifier of the IoTCoreConfiguration this subscriber reads through.")]
        public IRuntimeSetting<string> Identifier { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("d3c2f6e2-9f4c-4153-88ef-91b33bf3326a", "4bafaaac-3fc9-4e77-957c-825bf9d5b73a", "cad4d671-9f0d-4139-b856-12026c87e453")]
        [Description("The expected connected device's vendor ID.")]
        public IRuntimeSetting<ushort> VendorId { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("9ce0c188-1f51-423c-9e21-de9da593dcf7", "0a9ea5b2-fc38-4c8c-9ba5-7dde68fead2d", "53812c52-abfd-4bee-bd35-edad06a0e766")]
        [Description("The expected connected device's device ID.")]
        public IRuntimeSetting<uint> DeviceId { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("8eb74be8-63a6-470c-8eca-3b8fcedcd7a7", "201762d5-d6b3-4ec7-9b0d-cd9f297193f8", "2bf240ce-0375-4af4-b332-04b36e503456")]
        [Description("The IO-Link master port the device is connected to.")]
        public IRuntimeSetting<ushort> PortIndex { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("319e6bd0-0369-4852-b5e7-e6a539e35668", "5136516b-c17f-4d12-9eff-49a42cc56e5e", "a33ed614-85f7-4ac0-9cb8-387902e66edc")]
        [Description("The ISDU index of the process data to subscribe to.")]
        public IRuntimeSetting<ushort> ProcessDataInIndex { get; internal set; }
    }
}
