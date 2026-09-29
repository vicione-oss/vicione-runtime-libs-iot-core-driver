using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.DeviceTree.Contracts;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Rate-limits how often a passing <see cref="ErrorState"/> is forwarded, so that a chattering warning/damage
/// condition doesn't flood downstream consumers.
/// </summary>
[FunctionBlockDesign("5a60a3b1-c5b4-457a-8831-d535a383df4e", "901e86a1-379f-4ad8-a5d7-c81899e59914", "50f0a23a-0967-48cb-8344-ee0f3291af10")]
[Description("Rate-limits how often a passing ErrorState is forwarded, so that a chattering warning/damage condition doesn't flood downstream consumers.")]
public partial class ErrorStateGuard : RuntimeFunctionBlock<ErrorStateGuard>
{
    /// <summary>
    /// The FunctionBlock's process data inputs.
    /// </summary>
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();

    /// <summary>
    /// The process data inputs of <see cref="ErrorStateGuard"/>.
    /// </summary>
    [InputConnectorDesigns]
    public class Inputs_
    {
        /// <summary>
        /// The error state to filter.
        /// </summary>
        [InputConnectorDesign("6953eb6b-1b00-4b42-886e-c0180e49cb72")]
        [Description("The error state to filter.")]
        public IRuntimeConnector<ErrorState> ErrorState { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="ErrorStateGuard"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The most recently forwarded error state.
        /// </summary>
        [OutputConnectorDesign("c331f5e7-b689-4dcb-bdbf-2b7c2c484e16")]
        [Description("The most recently forwarded error state.")]
        public IRuntimeConnector<ErrorState> ErrorState { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's configuration settings.
    /// </summary>
    public new Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="ErrorStateGuard"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_
    {
        /// <summary>
        /// The minimum time, in hours, between two forwarded error states of the same kind.
        /// </summary>
        [SettingDesign("59ecc6ca-1b08-450d-afe6-bb72d5da2e73", "c763d0aa-9fe1-4dde-a0d8-eaa9a2439d61", "fef6a7aa-3ea3-4352-99f1-a08238aeaa46")]
        [Description("The minimum time, in hours, between two forwarded error states of the same kind.")]
        public IRuntimeSetting<short> Delay { get; internal set; }

        /// <summary>
        /// Whether to forward damage-level error states.
        /// </summary>
        [SettingDesign("57f2bcf3-cdb5-42c1-bb29-a1ae5329c964", "28d36064-d399-4e8a-9ee9-45fd01c2591b", "6ff3123f-61e9-45b4-8ad7-e61e81906d66")]
        [Description("Whether to forward damage-level error states.")]
        public IRuntimeSetting<bool> OnDamage { get; internal set; }

        /// <summary>
        /// Whether to forward warning-level error states.
        /// </summary>
        [SettingDesign("85659b86-73a0-4371-9c3d-3ba24c23f049", "fe235e68-2d2c-4282-80da-b930f0cd761e", "62023ffc-02a5-42bd-ba12-a24e00aea324")]
        [Description("Whether to forward warning-level error states.")]
        public IRuntimeSetting<bool> OnWarning { get; internal set; }
    }
}
