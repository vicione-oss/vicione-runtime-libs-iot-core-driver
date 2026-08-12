using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.Vse;
using ViciOne.Driver.Vse.Interfaces;
using ViciOne.Communication.Vse.Data;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Subscribes to a VSE device's monitored vibration/frequency object.
/// </summary>
[FunctionBlockDesign("e20d1fa9-ec9e-46cc-b697-bd475090ec1a", "d45bc887-0a96-4201-aef6-00fc1c5911f3", "75217a92-19cc-4d2e-a6db-482d66c6007f")]
[Driver<VseDriver>]
[Description("Subscribes to a VSE device's monitored vibration/frequency object.")]
public partial class ObjectSubscriber : SubscriberBase<ObjectSubscriber, DeviceObject>
{
    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="ObjectSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The object's average value over its configured averaging period.
        /// </summary>
        [OutputConnectorDesign("141788da-7f2a-4a20-bcc2-02edc556d60c")]
        [Description("The object's average value over its configured averaging period.")]
        public IRuntimeConnector<double> Average { get; internal set; }

        /// <summary>
        /// The object's maximum value over its configured averaging period.
        /// </summary>
        [OutputConnectorDesign("34880527-8802-4213-9fce-6b21963d2168")]
        [Description("The object's maximum value over its configured averaging period.")]
        public IRuntimeConnector<double> Maximum { get; internal set; }

        /// <summary>
        /// The object's minimum value over its configured averaging period.
        /// </summary>
        [OutputConnectorDesign("bd018c63-4819-4bb8-89b3-8fad430a8325")]
        [Description("The object's minimum value over its configured averaging period.")]
        public IRuntimeConnector<double> Minimum { get; internal set; }

        /// <summary>
        /// The object's unit.
        /// </summary>
        [OutputConnectorDesign("de5a61ef-cadd-49a3-9203-b4293c5eb219")]
        [Description("The object's unit.")]
        public IRuntimeConnector<string> Unit { get; internal set; }

        /// <summary>
        /// The object's reference value, averaged over its configured averaging period.
        /// </summary>
        [OutputConnectorDesign("f513e97c-e0ba-4d5e-9761-06b50945d114")]
        [Description("The object's reference value, averaged over its configured averaging period.")]
        public IRuntimeConnector<double> RefValue { get; internal set; }

        /// <summary>
        /// The object's rotational speed, averaged over its configured averaging period.
        /// </summary>
        [OutputConnectorDesign("c771f93f-e7e9-4551-a6aa-bd1f76488769")]
        [Description("The object's rotational speed, averaged over its configured averaging period.")]
        public IRuntimeConnector<double> RotSpeed { get; internal set; }

        /// <summary>
        /// The object's configured warning threshold.
        /// </summary>
        [OutputConnectorDesign("436f1b47-ff5d-4b9d-bfee-64d37ffeaba6")]
        [Description("The object's configured warning threshold.")]
        public IRuntimeConnector<double> Warning { get; internal set; }

        /// <summary>
        /// The object's configured baseline value.
        /// </summary>
        [OutputConnectorDesign("8971b0ab-6a0a-4f44-b5d3-9fffb85ea34c")]
        [Description("The object's configured baseline value.")]
        public IRuntimeConnector<double> BaseLine { get; internal set; }

        /// <summary>
        /// The object's configured damage threshold.
        /// </summary>
        [OutputConnectorDesign("51c3ca81-ee2a-49d2-92bd-beec77ddd7c5")]
        [Description("The object's configured damage threshold.")]
        public IRuntimeConnector<double> Damage { get; internal set; }

        /// <summary>
        /// The time period, in milliseconds, over which <see cref="Average"/>/<see cref="Maximum"/>/<see cref="Minimum"/> are computed.
        /// </summary>
        [OutputConnectorDesign("62add194-e0f0-4c9f-8da2-25669ba656ab")]
        [Description("The time period, in milliseconds, over which Average/Maximum/Minimum are computed.")]
        public IRuntimeConnector<double> AveragingPeriod { get; internal set; }

        /// <summary>
        /// A human-readable description of the object's current error flags, if any.
        /// </summary>
        [OutputConnectorDesign("2befa247-00e5-4c9b-9de1-dc9e975ba018")]
        [Description("A human-readable description of the object's current error flags, if any.")]
        public IRuntimeConnector<string> Error { get; internal set; }

        /// <summary>
        /// The measured values captured at the moment <see cref="Error"/> last changed.
        /// </summary>
        [OutputConnectorDesign("32c15026-9b38-4064-a209-af0ad017cc5a")]
        [Description("The measured values captured at the moment Error last changed.")]
        public IRuntimeConnector<ErrorState> ErrorState { get; internal set; }

        /// <summary>
        /// The object's node path paired with its current <see cref="RotSpeed"/>.
        /// </summary>
        [OutputConnectorDesign("5529fee0-3437-4adc-83b6-9332f4094096")]
        [Description("The object's node path paired with its current RotSpeed.")]
        public IRuntimeConnector<RotationalFrequencyTuple> RotationalFrequencyTuple { get; internal set; }

        /// <summary>
        /// Whether the object's current state and flags represent a fault-free reading.
        /// </summary>
        [OutputConnectorDesign("bb18a71b-f1a1-48ca-8e8a-e192919b0b8a")]
        [Description("Whether the object's current state and flags represent a fault-free reading.")]
        public IRuntimeConnector<bool> Valid { get; internal set; }

        /// <summary>
        /// Whether the subscribed device is currently available.
        /// </summary>
        [OutputConnectorDesign("c38f7f40-ef59-453e-80d6-59755ad24880")]
        [Description("Whether the subscribed device is currently available.")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    /// <inheritdoc/>
    public override Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="ObjectSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_ : IVseSubscriberSettings
    {
        /// <inheritdoc/>
        [SettingDesign("225dac1a-a373-48c2-966f-b6cf38d62792", "b2ee2fe8-43ab-44b8-bcd0-060a9eba06ec", "3fe3b34f-48a6-40a2-9d77-23ee4d8421af")]
        [Description("The VSE device's address.")]
        [DefaultValue("192.168.10.100")]
        public IRuntimeSetting<string> Address { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("614a1ffe-258d-48bf-ad91-d0e1b852d24d", "690076fb-5765-4818-a2c7-0ad8ff4aeb3c", "a57b5625-440a-4f9e-9271-7f1a97493b44")]
        [Description("The node path in the VSE device's data model to subscribe to.")]
        [DefaultValue("/Device/Objects/Object01")]
        public IRuntimeSetting<string> NodePath { get; internal set; }
    }
}
