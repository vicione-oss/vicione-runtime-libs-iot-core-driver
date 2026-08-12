using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.Vse;

/// <summary>
/// Tracks the rotational speed and reference value observed at the moment a value reached its minimum/maximum
/// within the current averaging period, alongside that period's own aggregated values.
/// </summary>
[FunctionBlockDesign("8b01c744-167d-4ba3-af3e-66050790ea57", "584503ed-0446-4461-a53d-3493ef3ec086", "c5a218d3-c3ed-4101-8de6-d85b3647eced")]
[Description("Tracks the rotational speed and reference value observed at the moment a value reached its minimum/maximum within the current averaging period, alongside that period's own aggregated values.")]
public partial class RpmAtMinMaxTracker : RuntimeFunctionBlock<RpmAtMinMaxTracker>
{
    /// <summary>
    /// The FunctionBlock's process data inputs.
    /// </summary>
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();

    /// <summary>
    /// The process data inputs of <see cref="RpmAtMinMaxTracker"/>.
    /// </summary>
    [InputConnectorDesigns]
    public class Inputs_
    {
        /// <summary>
        /// The current averaging period's average value.
        /// </summary>
        [InputConnectorDesign("9f0d4c0c-b45e-4294-b1b0-c6f8eed0f103")]
        [Description("The current averaging period's average value.")]
        public IRuntimeConnector<double> Average { get; internal set; }

        /// <summary>
        /// The current averaging period's difference between maximum and minimum value.
        /// </summary>
        [InputConnectorDesign("291d3f55-6970-4f13-9a1a-008adcc124d0")]
        [Description("The current averaging period's difference between maximum and minimum value.")]
        public IRuntimeConnector<double> Difference { get; internal set; }

        /// <summary>
        /// The current averaging period's maximum value.
        /// </summary>
        [InputConnectorDesign("ecdac43a-8ed4-48f6-8e99-91bb85dfcd3c")]
        [Description("The current averaging period's maximum value.")]
        public IRuntimeConnector<double> Maximum { get; internal set; }

        /// <summary>
        /// The current averaging period's minimum value.
        /// </summary>
        [InputConnectorDesign("864724a2-729f-4df0-83b5-18032ca9098b")]
        [Description("The current averaging period's minimum value.")]
        public IRuntimeConnector<double> Minimum { get; internal set; }

        /// <summary>
        /// The current averaging period's sum of values.
        /// </summary>
        [InputConnectorDesign("8d4a8f9b-a901-408b-a61d-16a68fa2a78b")]
        [Description("The current averaging period's sum of values.")]
        public IRuntimeConnector<double> Sum { get; internal set; }

        /// <summary>
        /// The value tracked for its minimum/maximum.
        /// </summary>
        [InputConnectorDesign("aaa824a8-3192-422e-9d8f-8b3c34558791")]
        [Description("The value tracked for its minimum/maximum.")]
        public IRuntimeConnector<double> Value { get; internal set; }

        /// <summary>
        /// The reference value observed alongside <see cref="Value"/>.
        /// </summary>
        [InputConnectorDesign("6d8de9e0-a7be-4553-a04b-496eb7fe8c4d")]
        [Description("The reference value observed alongside Value.")]
        public IRuntimeConnector<double> RefValue { get; internal set; }

        /// <summary>
        /// The rotational speed observed alongside <see cref="Value"/>.
        /// </summary>
        [InputConnectorDesign("0d46c06e-20be-4b70-82e4-c1771fa9569f")]
        [Description("The rotational speed observed alongside Value.")]
        public IRuntimeConnector<double> RotSpeed { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="RpmAtMinMaxTracker"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <inheritdoc cref="Inputs_.Average"/>
        [OutputConnectorDesign("1aa4398b-3a45-4433-9b79-cefd281ea3b8")]
        [Description("The current averaging period's average value.")]
        public IRuntimeConnector<double> Average { get; internal set; }

        /// <inheritdoc cref="Inputs_.Difference"/>
        [OutputConnectorDesign("8e20fb20-05a4-460f-914d-9fa0abc2f394")]
        [Description("The current averaging period's difference between maximum and minimum value.")]
        public IRuntimeConnector<double> Difference { get; internal set; }

        /// <inheritdoc cref="Inputs_.Maximum"/>
        [OutputConnectorDesign("214febc9-7e9d-4157-846c-b668d423ecc4")]
        [Description("The current averaging period's maximum value.")]
        public IRuntimeConnector<double> Maximum { get; internal set; }

        /// <inheritdoc cref="Inputs_.Minimum"/>
        [OutputConnectorDesign("c263ff39-962a-4ff5-942a-ecb7160696b8")]
        [Description("The current averaging period's minimum value.")]
        public IRuntimeConnector<double> Minimum { get; internal set; }

        /// <inheritdoc cref="Inputs_.Sum"/>
        [OutputConnectorDesign("58f91954-2e71-4d3b-ab54-0b7a12c83271")]
        [Description("The current averaging period's sum of values.")]
        public IRuntimeConnector<double> Sum { get; internal set; }

        /// <summary>
        /// The reference value observed when <see cref="Maximum"/> was reached.
        /// </summary>
        [OutputConnectorDesign("cbdda594-4f8d-4da9-a664-9aa45001fadf")]
        [Description("The reference value observed when Maximum was reached.")]
        public IRuntimeConnector<double> RefValueAtMaximum { get; internal set; }

        /// <summary>
        /// The reference value observed when <see cref="Minimum"/> was reached.
        /// </summary>
        [OutputConnectorDesign("8414706a-0b94-4efc-87af-4e10ed3c1299")]
        [Description("The reference value observed when Minimum was reached.")]
        public IRuntimeConnector<double> RefValueAtMinimum { get; internal set; }

        /// <summary>
        /// The rotational speed observed when <see cref="Maximum"/> was reached.
        /// </summary>
        [OutputConnectorDesign("dfba1ff7-924b-47af-a753-63ebf973653a")]
        [Description("The rotational speed observed when Maximum was reached.")]
        public IRuntimeConnector<double> RotSpeedAtMaximum { get; internal set; }

        /// <summary>
        /// The rotational speed observed when <see cref="Minimum"/> was reached.
        /// </summary>
        [OutputConnectorDesign("ff567407-9948-4c4f-a86a-d775421f660d")]
        [Description("The rotational speed observed when Minimum was reached.")]
        public IRuntimeConnector<double> RotSpeedAtMinimum { get; internal set; }
    }
}
