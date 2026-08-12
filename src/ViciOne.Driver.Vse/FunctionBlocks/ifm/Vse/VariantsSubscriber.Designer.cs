using ViciOne.Communication.Vse.Data;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.Vse;
using ViciOne.Driver.Vse.Interfaces;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Subscribes to a VSE device's active variant selection, allowing it to be written from the runtime as well as read.
/// </summary>
[FunctionBlockDesign("dcffafe9-51c4-4a42-82e8-7516edf7b837", "535fbb7b-7991-46f4-8d35-d85cedcee5f3", "1eb3a2c7-3803-49f7-94c3-eb12ad108755")]
[Driver<VseDriver>]
[Description("Subscribes to a VSE device's active variant selection, allowing it to be written from the runtime as well as read.")]
public partial class VariantsSubscriber : SubscriberBase<VariantsSubscriber, Variants>
{
    /// <summary>
    /// The FunctionBlock's process data inputs.
    /// </summary>
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();

    /// <summary>
    /// The process data inputs of <see cref="VariantsSubscriber"/>.
    /// </summary>
    [InputConnectorDesigns]
    public class Inputs_
    {
        /// <summary>
        /// The variant id to write to the device when it changes.
        /// </summary>
        [InputConnectorDesign("bf753864-91c8-46f8-80a4-45a2e646081a")]
        [Description("The variant id to write to the device when it changes.")]
        public IRuntimeConnector<long> ActiveVariant { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="VariantsSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The device's currently active variant id.
        /// </summary>
        [OutputConnectorDesign("2f5b156d-39a1-4698-af59-278c8e80d1f5")]
        [Description("The device's currently active variant id.")]
        public IRuntimeConnector<long> ActiveVariant { get; internal set; }

        /// <summary>
        /// Whether the subscribed device is currently available.
        /// </summary>
        [OutputConnectorDesign("c9fb7647-7708-44b5-9ab4-d09176f7d4e9")]
        [Description("Whether the subscribed device is currently available.")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    /// <inheritdoc/>
    public override Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="VariantsSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_ : IVseSubscriberSettings
    {
        /// <inheritdoc/>
        [SettingDesign("53e06fe6-dd7a-4d50-968d-a14c678ce349", "4d3fa29b-c5f1-4969-aed7-60946ebfd727", "1cab48d2-6fd8-4bc2-aab8-6dbff034f4fe")]
        [Description("The VSE device's address.")]
        [DefaultValue("192.168.10.100")]
        public IRuntimeSetting<string> Address { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("fdf66cf7-1f5d-422e-a790-16a24bfe7e89", "a581a0b3-9545-4250-b55d-198c3ecd26cf", "85bd1849-d6be-48b8-85c7-a89b5baaaaeb")]
        [Description("The node path in the VSE device's data model to subscribe to.")]
        [DefaultValue("/Device/Variants")]
        public IRuntimeSetting<string> NodePath { get; internal set; }
    }
}
