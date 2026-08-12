using ViciOne.Communication.Vse.Data;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.Vse;
using ViciOne.Driver.Vse.Interfaces;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Subscribes to a VSE device's counter.
/// </summary>
[FunctionBlockDesign("6312e770-3c86-45ee-90d5-1e417929dbbc", "2f85af17-bc7a-4f32-bbc5-77507baecf01", "f9b53f5e-706d-45e1-ace1-0f64cae76a97")]
[Driver<VseDriver>]
[Description("Subscribes to a VSE device's counter.")]
public partial class CounterSubscriber : SubscriberBase<CounterSubscriber, Counter>
{
    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="CounterSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The counter's current value.
        /// </summary>
        [OutputConnectorDesign("c76fc6bb-1150-490d-8f91-b33f293353ef")]
        [Description("The counter's current value.")]
        public IRuntimeConnector<double> Value { get; internal set; }

        /// <summary>
        /// The counter's unit.
        /// </summary>
        [OutputConnectorDesign("b64dd41e-a568-4e29-b3b5-2169abaee054")]
        [Description("The counter's unit.")]
        public IRuntimeConnector<string> Unit { get; internal set; }

        /// <summary>
        /// The counter's configured limit.
        /// </summary>
        [OutputConnectorDesign("54f1023c-929f-4cdf-baba-5c324e1dd85a")]
        [Description("The counter's configured limit.")]
        public IRuntimeConnector<double> Limit { get; internal set; }

        /// <summary>
        /// The counter's state ("Elapsed" if the limit was reached, empty otherwise).
        /// </summary>
        [OutputConnectorDesign("9642a53a-77a3-4695-bdc8-c8e648d703de")]
        [Description("The counter's state (\"Elapsed\" if the limit was reached, empty otherwise).")]
        public IRuntimeConnector<string> State { get; internal set; }

        /// <summary>
        /// Whether the subscribed device is currently available.
        /// </summary>
        [OutputConnectorDesign("0f266bcf-c00b-4f5a-8406-e5fb586b3c91")]
        [Description("Whether the subscribed device is currently available.")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    /// <inheritdoc/>
    public override Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="CounterSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_ : IVseSubscriberSettings
    {
        /// <inheritdoc/>
        [SettingDesign("1cdf45d6-808a-4862-850e-e451da25a7ed", "a1368c1e-f61f-438a-8ac5-d16a1867379d", "af0188b3-0b72-4c10-acf2-cd6195b67f0c")]
        [Description("The VSE device's address.")]
        [DefaultValue("192.168.10.100")]
        public IRuntimeSetting<string> Address { get; internal set; }

        /// <inheritdoc/>
        [SettingDesign("eb8fc77f-af96-45ba-968e-b45ed585477c", "76f80d5c-ff68-4e53-8036-1d13c48850d9", "96296f20-9f03-47c5-9cfc-57e87e85fea1")]
        [Description("The node path in the VSE device's data model to subscribe to.")]
        [DefaultValue("/Device/Counters/Counter01")]
        public IRuntimeSetting<string> NodePath { get; internal set; }
    }
}
