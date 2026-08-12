using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.Vse;
using ViciOne.Engine.DefaultValidators;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Triggers and receives a raw-data recording from a VSE device's sensor.
/// </summary>
[FunctionBlockDesign("882057a8-2316-4bb4-8ca7-46840f44ddd6", "2ec4a300-e4b7-4e75-9bb5-1832346a78a3", "251a7d3a-8cf7-44af-8e71-ed8498662e83")]
[Driver<VseDriver>]
[Description("Triggers and receives a raw-data recording from a VSE device's sensor.")]
public partial class RawDataSubscriber : RuntimeFunctionBlock<RawDataSubscriber>
{
    /// <summary>
    /// The FunctionBlock's process data inputs.
    /// </summary>
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();

    /// <summary>
    /// The process data inputs of <see cref="RawDataSubscriber"/>.
    /// </summary>
    [InputConnectorDesigns]
    public class Inputs_
    {
        /// <summary>
        /// Set to start a recording.
        /// </summary>
        [InputConnectorDesign("e9e1860b-b551-4796-a35b-6218cfc889d2")]
        [Description("Set to start a recording.")]
        public IRuntimeConnector<bool> Trigger { get; internal set; }

        /// <summary>
        /// Error states from monitored objects that should also trigger a recording.
        /// </summary>
        [InputConnectorDesign("e17f1bc4-7c3a-4622-994b-4f3afc6a38c3")]
        [Description("Error states from monitored objects that should also trigger a recording.")]
        [DefaultCrossSourcePoolingStrategy(PoolingStrategy.Concat)]
        [DefaultPerSourcePoolingStrategy(PoolingStrategy.Concat)]
        public IRuntimeConnector<ErrorState[]> ObjectTrigger { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="RawDataSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The finished recording's result.
        /// </summary>
        [OutputConnectorDesign("514c891d-78c4-4e13-9990-44499323069a")]
        [Description("The finished recording's result.")]
        public IRuntimeConnector<Measurement> Data { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's configuration settings.
    /// </summary>
    public new Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="RawDataSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_
    {
        /// <summary>
        /// The VSE device's address.
        /// </summary>
        [SettingDesign("344d17fd-7d62-4341-b05c-7621a4af0dd1", "2474d831-68ac-4354-8ba8-bf68ef52c074", "8ba022b5-a663-4d0e-a245-22571b1ff506")]
        [Description("The VSE device's address.")]
        [DefaultValue("192.168.10.100")]
        public IRuntimeSetting<string> Address { get; internal set; }

        /// <summary>
        /// The sensor to record from.
        /// </summary>
        [SettingDesign("b0f3cc24-31ef-4b40-a9ce-5f04fff1cbcf", "2cc05b8f-579d-48a1-9547-8e389921eb5a", "af324d25-13b5-4c20-b6bf-7fb8881216e0")]
        [Description("The sensor to record from.")]
        [DefaultValue(1)]
        [RangeValidator<int>(1, 265)]
        public IRuntimeSetting<int> SensorIndex { get; internal set; }

        /// <summary>
        /// The recording's sample rate, in Hz.
        /// </summary>
        [SettingDesign("1b914f44-1dc6-4219-8b5c-e838befb9995", "3d6bcb71-42f7-44e6-ac54-b3f3fcc8be4b", "b3ac20c7-49d7-43df-8ce3-8cc3a4674d14")]
        [Description("The recording's sample rate, in Hz.")]
        [DefaultValue(100000)]
        [RangeValidator<int>(50000, 100000)]
        public IRuntimeSetting<int> SampleRate { get; internal set; }

        /// <summary>
        /// The recording's duration, in milliseconds.
        /// </summary>
        [SettingDesign("1fbb9658-e539-40b0-b584-ba9ac51e242a", "1186bb5e-cc7a-4f58-a2dd-6743710a31d6", "1f9d4a74-cfbf-4b18-beff-385da1298c42")]
        [Description("The recording's duration, in milliseconds.")]
        [DefaultValue(4000)]
        [RangeValidator<int>(100, 3600000)]
        public IRuntimeSetting<int> Duration { get; internal set; }

        /// <summary>
        /// Whether to skip decoding the recorded raw data into timed values.
        /// </summary>
        [SettingDesign("4cabd0ec-44a5-427a-838f-541703b516bd", "c8c9d241-c255-4985-91a7-92ecbb433881", "dc0fd4e0-126c-4a59-984d-620011b3475b")]
        [Description("Whether to skip decoding the recorded raw data into timed values.")]
        [DefaultValue(true)]
        public IRuntimeSetting<bool> IgnoreTimedValueArray { get; internal set; }
    }
}
