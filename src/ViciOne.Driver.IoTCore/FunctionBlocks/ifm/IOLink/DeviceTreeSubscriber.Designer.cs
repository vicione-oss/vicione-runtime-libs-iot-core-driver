using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Driver.IoTCore;
using ViciOne.DeviceTree.Contracts;
using Description = System.ComponentModel.DescriptionAttribute;

namespace ifm.IOLink;

/// <summary>
/// Builds and publishes the IO-Link master's device tree, refreshing it whenever a port's connected device
/// changes or reconnects.
/// </summary>
[FunctionBlockDesign("d43d0507-5f31-4e56-aaed-4ca76a44d971", "102c8c74-6b78-48bb-ab78-c92eacb85f6a", "e34ecfd3-d1ff-431b-909d-e894343ff667")]
[Driver<IoTCoreDriver>]
[Description("Builds and publishes the IO-Link master's device tree, refreshing it whenever a port's connected device changes or reconnects.")]
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
        [InputConnectorDesign("0ae3d21f-0797-4949-bed9-2e885934753e")]
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
        /// The master's most recently published device tree.
        /// </summary>
        [OutputConnectorDesign("9105930c-9fb9-4a24-9db9-bebfd5ab1960")]
        [Description("The master's most recently published device tree.")]
        public IRuntimeConnector<DeviceTreeIoLinkMaster> DeviceTree { get; internal set; }

        /// <summary>
        /// The IODD directory currently in use.
        /// </summary>
        [OutputConnectorDesign("34e64b82-0a49-4ed6-9d97-01ab020b2ecc")]
        [Description("The IODD directory currently in use.")]
        public IRuntimeConnector<string> IoddDirectory { get; internal set; }
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
        /// The IO-Link master's address.
        /// </summary>
        [SettingDesign("588d363b-0823-47c9-adf7-37db039ac408", "d9247849-fe00-4abb-97db-8267ed227a3a", "4d3c8be5-e3a2-4a38-9e3e-3119c4958c8e")]
        [Description("The IO-Link master's address.")]
        public IRuntimeSetting<string> Url { get; internal set; }

        /// <summary>
        /// The directory IODD files are loaded from (and downloaded into, if <see cref="IoddAutoDownload"/> is set).
        /// </summary>
        [SettingDesign("dc73e1e1-1588-4269-88a4-1c06ec943c30", "def4c16f-98e4-4560-b5f9-c9e180f2169f", "94a48226-e3c6-4b5b-b756-b4e7c941d5de")]
        [Description("The directory IODD files are loaded from (and downloaded into, if IoddAutoDownload is set).")]
        public IRuntimeSetting<string> IoddDirectory { get; internal set; }

        /// <summary>
        /// Whether to automatically download a connected device's IODD if it isn't found in <see cref="IoddDirectory"/>.
        /// </summary>
        [SettingDesign("b556fca2-9ede-4a4b-a853-e58cb31acfcf", "ea05ac5a-4eac-4030-af2e-09995cb8cf34", "67b40d70-dd8d-48a3-8513-327d7b71bf24")]
        [Description("Whether to automatically download a connected device's IODD if it isn't found in IoddDirectory.")]
        public IRuntimeSetting<bool> IoddAutoDownload { get; internal set; }

        /// <summary>
        /// The username to authenticate with, or empty for no authentication.
        /// </summary>
        [SettingDesign("95edbd0b-68b9-4837-b843-5d3ef47e1391", "70c97b0a-d5a4-4ae3-b5e3-2a97a3cea510", "0251c6e4-19aa-4e18-9ebb-09b6dd428ef2")]
        [Description("The username to authenticate with, or empty for no authentication.")]
        public IRuntimeSetting<string> Username { get; internal set; }

        /// <summary>
        /// The password to authenticate with, ignored if <see cref="Username"/> is empty.
        /// </summary>
        [SettingDesign("ed4a3e98-09dd-4037-bf95-2765264c943f", "4ba1c935-88bc-45f7-b18a-48c2aba3c80f", "f7f7aafc-73e8-4114-92f0-c2cd2a46b188")]
        [Description("The password to authenticate with, ignored if Username is empty.")]
        public IRuntimeSetting<string> Password { get; internal set; }
    }
}
