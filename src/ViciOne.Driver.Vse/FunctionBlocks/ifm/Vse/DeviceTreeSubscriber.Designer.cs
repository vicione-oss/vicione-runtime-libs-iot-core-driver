using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Core.Runtime;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.Vse;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Builds and publishes a VSE device's device tree, refreshing it when the device's parameters, system mode, or
/// timeout state change.
/// </summary>
[FunctionBlockDesign("8a3246e7-c0a1-44df-8e1e-0acdad717432", "897607f9-1f26-4b0c-ac94-980717cb4dec", "c77f0993-38ed-40e0-922c-5991debd4c2b")]
[Driver<VseDriver>]
[Description("Builds and publishes a VSE device's device tree, refreshing it when the device's parameters, system mode, or timeout state change.")]
public partial class DeviceTreeSubscriber : RuntimeFunctionBlock<DeviceTreeSubscriber>
{
    /// <summary>
    /// The FunctionBlock's process data inputs.
    /// </summary>
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();

    /// <summary>
    /// The process data inputs of <see cref="DeviceTreeSubscriber"/>.
    /// </summary>
    [InputConnectorDesigns]
    public class Inputs_
    {
        /// <summary>
        /// Set to force a device tree rebuild.
        /// </summary>
        [InputConnectorDesign("c3f46bc9-2ce3-4169-a061-2af95dc50fc8")]
        [Description("Set to force a device tree rebuild.")]
        public IRuntimeConnector<bool> Trigger { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="DeviceTreeSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The device's most recently published device tree.
        /// </summary>
        [OutputConnectorDesign("74010ddc-7f1d-4bf4-895f-237225a77329")]
        [Description("The device's most recently published device tree.")]
        public IRuntimeConnector<DeviceTreeVseDevice> DeviceTree { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's configuration settings.
    /// </summary>
    public new Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="DeviceTreeSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_
    {
        /// <summary>
        /// The VSE device's address.
        /// </summary>
        [SettingDesign("64b80988-b76e-41e9-ba47-8f0884b0e90e", "5dcf4a09-b9f2-4e6a-9af2-1ac7d9f95de4", "3d9b635c-b8ad-43f0-b342-da14011719ed")]
        [Description("The VSE device's address.")]
        public IRuntimeSetting<string> Url { get; internal set; }
    }
}

