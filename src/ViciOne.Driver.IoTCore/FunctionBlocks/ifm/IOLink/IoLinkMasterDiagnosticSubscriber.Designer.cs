using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.IOLink;

/// <summary>
/// Exposes an IO-Link master's own diagnostic values (internal temperature, supply voltage, current draw and
/// overall supervision status) as live outputs, read through the <see cref="IoTCoreConfiguration"/> identified by
/// its <see cref="Settings_.Identifier"/> - nothing else needs configuring, the master's addresses are resolved
/// automatically.
/// </summary>
[FunctionBlockDesign("360b6eb4-d964-4e71-8757-634fa3a9dc36", "5cea50de-176f-4ce0-9245-81371406c04d", "62522266-d557-4aee-b5aa-2f9cafc40741")]
[Driver<IoTCoreDriver>]
[Description("Exposes an IO-Link master's own diagnostic values (temperature, supply voltage, current and supervision status) as live outputs, read through the IoTCoreConfiguration identified by its Identifier.")]
public partial class IoLinkMasterDiagnosticSubscriber : RuntimeFunctionBlock<IoLinkMasterDiagnosticSubscriber>
{
    /// <summary>
    /// The FunctionBlock's process data outputs.
    /// </summary>
    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();

    /// <summary>
    /// The process data outputs of <see cref="IoLinkMasterDiagnosticSubscriber"/>.
    /// </summary>
    [OutputConnectorDesigns]
    public class Outputs_
    {
        /// <summary>
        /// The master's total current draw.
        /// </summary>
        [OutputConnectorDesign("1d8253c4-5b02-4abb-b35f-b320b874b550")]
        [Description("The master's total current draw.")]
        public IRuntimeConnector<double> Current { get; internal set; }

        /// <summary>
        /// The unit of <see cref="Current"/>.
        /// </summary>
        [OutputConnectorDesign("52f8121f-21da-4321-98c4-1d6e870ce5d5")]
        [Description("The unit of the current value.")]
        [DefaultValue("")]
        public IRuntimeConnector<string> CurrentUnit { get; internal set; }
        
        /// <summary>
        /// The master's internal temperature.
        /// </summary>
        [OutputConnectorDesign("e4b3c64d-f566-457f-aca8-f7777256e938")]
        [Description("The master's internal temperature.")]
        public IRuntimeConnector<double> Temperature { get; internal set; }

        /// <summary>
        /// The unit of <see cref="Temperature"/>.
        /// </summary>
        [OutputConnectorDesign("f1b192cf-f295-4ac2-b106-1d6b167d45d4")]
        [Description("The unit of the temperature value.")]
        [DefaultValue("")]
        public IRuntimeConnector<string> TemperatureUnit { get; internal set; }

        /// <summary>
        /// The master's supply voltage.
        /// </summary>
        [OutputConnectorDesign("ec36ccc3-1ef6-48db-8b1b-f51f18bd77f1")]
        [Description("The master's supply voltage.")]
        public IRuntimeConnector<double> SupplyVoltage { get; internal set; }

        /// <summary>
        /// The unit of <see cref="SupplyVoltage"/>.
        /// </summary>
        [OutputConnectorDesign("bcaa2df0-3850-414c-9ca9-480c440d51c2")]
        [Description("The unit of the supply voltage value.")]
        [DefaultValue("")]
        public IRuntimeConnector<string> SupplyVoltageUnit { get; internal set; }

        /// <summary>
        /// The master's overall supervision status.
        /// </summary>
        [OutputConnectorDesign("9d43f3ba-db60-4ebf-9272-76a4acf7de4b")]
        [Description("The master's overall supervision status.")]
        public IRuntimeConnector<long> SupervisionStatus { get; internal set; }

        /// <summary>
        /// Whether the master is currently reachable.
        /// </summary>
        [OutputConnectorDesign("140200f7-0e85-4d74-95ef-f23a7e08cc81")]
        [Description("Whether the master is currently reachable.")]
        public IRuntimeConnector<bool> Available { get; internal set; }
    }

    /// <summary>
    /// The FunctionBlock's configuration settings.
    /// </summary>
    public new Settings_ Settings { get; } = new Settings_();

    /// <summary>
    /// The configuration settings of <see cref="IoLinkMasterDiagnosticSubscriber"/>.
    /// </summary>
    [SettingDesigns]
    public class Settings_
    {
        /// <summary>
        /// The identifier of the IoTCoreConfiguration whose master's diagnostics this block reads.
        /// </summary>
        [SettingDesign("94adb2d0-c229-47d6-99ef-30e1337c4313", "04e24eeb-ea1c-4706-93b3-6cb3fd4e558d", "46128e24-aa37-4b7c-bc56-6e2db3ce7440")]
        [Description("The identifier of the IoTCoreConfiguration whose master's diagnostics this block reads.")]
        public IRuntimeSetting<string> Identifier { get; internal set; }
    }
}
