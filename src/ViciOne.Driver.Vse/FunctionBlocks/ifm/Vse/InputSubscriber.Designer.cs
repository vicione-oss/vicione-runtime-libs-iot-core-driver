using ViciOne.Communication.Vse.Data;
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.Vse;
using ViciOne.Driver.Vse.Interfaces;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Subscribes to a VSE device's external input, allowing its value to be written from the runtime as well as read.
/// </summary>
[FunctionBlockDesign("bda228ee-19ca-4293-a94a-49ffab71c76d", "c453173b-73bd-4653-9c6b-6b01b800a2c2", "fa6cb1c7-050e-4f24-ba7c-3fe83b4153e8")]
[Driver<VseDriver>]
[Description("Subscribes to a VSE device's external input, allowing its value to be written from the runtime as well as read.")]
public partial class InputSubscriber : SubscriberBase<InputSubscriber, Input>
{
    /// <summary>
    /// The FunctionBlock's process data inputs.
    /// </summary>
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();

    /// <summary>
    /// The process data inputs of <see cref="InputSubscriber"/>.
    /// </summary>
    [InputConnectorDesigns]
    public class Inputs_
    {
        /// <summary>
        /// The value to write to the input when it changes.
        /// </summary>
        [InputConnectorDesign("6c627fd6-fe8f-45f1-8de8-5549784f3518")]
        [Description("The value to write to the input when it changes.")]
        public IRuntimeConnector<double> Value { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="InputSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The input's current value.
        /// </summary>
        [OutputConnectorDesign("49ffeaa7-9913-4c40-94cf-4d7d7a473b41")]
        [Description("The input's current value.")]
        public IRuntimeConnector<double> Value { get; internal set; }

        /// <summary>
        /// The input's unit.
        /// </summary>
        [OutputConnectorDesign("9ec34ae4-84b7-4c69-b30c-b25c83388886")]
        [Description("The input's unit.")]
        public IRuntimeConnector<string> Unit { get; internal set; }

        /// <summary>
        /// Whether the subscribed device is currently available.
        /// </summary>
        [OutputConnectorDesign("1aedcdab-1eb8-42f7-a350-a8e32254aea5")]
        [Description("Whether the subscribed device is currently available.")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    /// <inheritdoc/>
    public override Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="InputSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_ : IVseSubscriberSettings
    {
        /// <inheritdoc/>
        [SettingDesign("66655db9-fab0-4f7a-8e29-aacbe93d1f7a", "42f60289-87cb-4f6b-8bcf-a28bb78691a9", "a059886e-d145-4c3b-8667-34daf980b828")]
        [Description("The VSE device's address.")]
        [DefaultValue("192.168.10.100")]
        public IRuntimeSetting<string> Address { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("8474a1da-5b33-492c-bba7-7946b83ccdb8", "214227d5-f6ef-46ee-b5a7-b8983ae169a3", "f65dea7b-eba0-428b-8aad-3d62237967eb")]
        [Description("The node path in the VSE device's data model to subscribe to.")]
        [DefaultValue("/Device/Inputs/External/Input01")]
        public IRuntimeSetting<string> NodePath { get; internal set; }
    }
}
