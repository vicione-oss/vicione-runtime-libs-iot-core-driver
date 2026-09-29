using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Interfaces;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.IOLink;

/// <summary>
/// Subscribes to an IO-Link device's numeric process-data-in value, exposing it as a <see cref="double"/>.
/// </summary>
[FunctionBlockDesign("70b13cb7-af9e-42c5-bcf1-6249576326c5", "77591ef2-d135-45b8-9149-635f056a7bd6", "8a5479fe-e6a9-4ca5-8d91-da6d7c7eed57")]
[Driver<IoTCoreDriver>]
[Description("Subscribes to an IO-Link device's numeric process-data-in value, exposing it as a double.")]
public partial class DoubleSubscriber : SubscriberBase<DoubleSubscriber>
{
    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="DoubleSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The subscribed value's unit.
        /// </summary>
        [OutputConnectorDesign("2a16682a-aabc-4fda-abb0-87f0f15882e0")]
        [Description("The subscribed value's unit.")]
        [DefaultValue("")]
        public IRuntimeConnector<string> Unit { get; internal set; }

        /// <summary>
        /// The subscribed numeric value.
        /// </summary>
        [OutputConnectorDesign("37acbdbc-decf-470d-963b-f4c36336aee9")]
        [Description("The subscribed numeric value.")]
        public IRuntimeConnector<double> Value { get; internal set; }

        /// <summary>
        /// Whether the subscribed device is currently available.
        /// </summary>
        [OutputConnectorDesign("57a3f618-2d28-43a4-b0f4-b03aeab78e44")]
        [Description("Whether the subscribed device is currently available.")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    /// <inheritdoc/>
    public override Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="DoubleSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_ : IIoLinkSubscriberSettings
    {
        /// <inheritdoc/>
        [SettingDesign("8418803c-e3db-413d-9c33-d0e5a7da8a74", "da48559e-eb76-4fe0-a252-63971d29edce", "fe02d7b6-b2f0-4dec-bd16-fa2fe8b45b5d")]
        [Description("The identifier of the IoTCoreConfiguration this subscriber reads through.")]
        public IRuntimeSetting<string> Identifier { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("22ae0260-0325-4c79-bfaa-c8206d2dec80", "bd88c0eb-c30d-44c6-b43e-4f864972c3f1", "e65440e9-6109-487b-a1d1-a5002a9daedc")]
        [Description("The expected connected device's vendor ID.")]
        public IRuntimeSetting<ushort> VendorId { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("2cec8a97-b943-42b9-a722-f58223f45552", "9a40d4b6-f5ca-4fef-b623-20c8ade854a4", "725544fc-c8da-44b6-9ae1-f7045025d146")]
        [Description("The expected connected device's device ID.")]
        public IRuntimeSetting<uint> DeviceId { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("154e2818-8646-4d75-9ee8-95c5a21dbc1c", "9d4a572f-e85c-4f38-96bc-46ce262332f3", "d604da47-6308-40cf-a772-b0b27c4b8654")]
        [Description("The IO-Link master port the device is connected to.")]
        public IRuntimeSetting<ushort> PortIndex { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("e284ee60-67d9-4d0d-ade6-e9227cbbd090", "b679536f-3a88-4980-ae2f-69385b06b7aa", "b4389493-075b-4974-bfa3-e1b87aa8c6ea")]
        [Description("The ISDU index of the process data to subscribe to.")]
        public IRuntimeSetting<ushort> ProcessDataInIndex { get; internal set; }
    }
}
