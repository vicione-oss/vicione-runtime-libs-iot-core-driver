using ViciOne.Communication.Vse.Data;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.Vse;
using ViciOne.Driver.Vse.Interfaces;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Subscribes to a VSE device's digital alarm.
/// </summary>
[FunctionBlockDesign("dbe6a560-f406-44f6-8551-d6d566cb54ed", "1db9323f-8f0d-4d0e-8ca8-f69711c84949", "fff156d5-b382-4c76-abaa-76fac593ff20")]
[Driver<VseDriver>]
[Description("Subscribes to a VSE device's digital alarm.")]
public partial class DigitalAlarmSubscriber : SubscriberBase<DigitalAlarmSubscriber, DigitalAlarm>
{
    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="DigitalAlarmSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The alarm's current state.
        /// </summary>
        [OutputConnectorDesign("bd69cffd-79af-438d-8c60-99430f44b63b")]
        [Description("The alarm's current state.")]
        public IRuntimeConnector<bool> Value { get; internal set; }

        /// <summary>
        /// <see cref="Value"/> as a number (0 or 1).
        /// </summary>
        [OutputConnectorDesign("eca1448a-0f8c-40b3-a606-77d940b2a487")]
        [Description("Value as a number (0 or 1).")]
        public IRuntimeConnector<double> NumericValue { get; internal set; }

        /// <summary>
        /// Whether the subscribed device is currently available.
        /// </summary>
        [OutputConnectorDesign("90f17401-b5b9-4c92-93f7-e244c0478fdb")]
        [Description("Whether the subscribed device is currently available.")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    /// <inheritdoc/>
    public override Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="DigitalAlarmSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_ : IVseSubscriberSettings
    {
        /// <inheritdoc/>
        [SettingDesign("b1e6884a-0768-4f66-b503-8da34a645b6d", "acdd08c4-a690-412e-917f-d11e0f4a4dd6", "8c781c44-a2c1-4fb9-bbf5-7db0f4475dab")]
        [Description("The VSE device's address.")]
        [DefaultValue("192.168.10.100")]
        public IRuntimeSetting<string> Address { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("080450c4-ca1f-4b27-8425-c4618ccbd7af", "4a81c2b8-8621-4442-9ce3-81bfd449af01", "ffa5fcb2-7e37-4596-9fdd-d8e021910165")]
        [Description("The node path in the VSE device's data model to subscribe to.")]
        [DefaultValue("/Device/Alarms/Alarm01")]
        public IRuntimeSetting<string> NodePath { get; internal set; }
    }
}
