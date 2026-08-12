using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Driver.IoTCore.Interfaces;

/// <summary>
/// The configuration settings shared by every IO-Link process-data subscriber FunctionBlock
/// (<c>SubscriberBase&lt;TBlock&gt;</c>'s <c>Settings</c>).
/// </summary>
public interface IIoLinkSubscriberSettings
{
    /// <summary>
    /// The IO-Link master's address.
    /// </summary>
    IRuntimeSetting<string> Address { get; }

    /// <summary>
    /// An optional free-text tag identifying this specific configuration.
    /// </summary>
    IRuntimeSetting<string> ApplicationSpecificTag { get; }

    /// <summary>
    /// The expected connected device's device ID.
    /// </summary>
    IRuntimeSetting<uint> DeviceId { get; }

    /// <summary>
    /// The IO-Link master port the device is connected to.
    /// </summary>
    IRuntimeSetting<ushort> PortIndex { get; }

    /// <summary>
    /// The ISDU index of the process data to subscribe to.
    /// </summary>
    IRuntimeSetting<ushort> ProcessDataInIndex { get; }

    // TODO: check what this is relevant for
    IRuntimeSetting<string> ProductName { get; }

    /// <summary>
    /// The expected connected device's vendor ID.
    /// </summary>
    IRuntimeSetting<ushort> VendorId { get; }
}
