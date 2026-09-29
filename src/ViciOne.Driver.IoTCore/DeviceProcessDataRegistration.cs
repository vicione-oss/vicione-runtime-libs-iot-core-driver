using System.Collections.Concurrent;
using ifm.IoddParser.Contracts;
using ViciOne.Driver.IoTCore.Interfaces;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// Tracks which <see cref="IIoLinkSubscriberFunctionBlock"/>s are registered for process data at a given port,
/// keyed by ISDU index, for one connected device.
/// </summary>
internal sealed class DeviceProcessDataRegistration
{
    /// <summary>
    /// The connected device's parsed IODD, once known.
    /// </summary>
    public IDevice? DeviceDefinition { get; set; }

    /// <summary>
    /// The connected device's device ID.
    /// </summary>
    public required uint DeviceId { get; init; }

    /// <summary>
    /// The function blocks registered for process data, keyed by ISDU index.
    /// </summary>
    public ConcurrentDictionary<int, ConcurrentDictionary<IIoLinkSubscriberFunctionBlock, byte>> SubIndexRegistrations { get; } = [];

    /// <summary>
    /// The connected device's vendor ID.
    /// </summary>
    public required ushort VendorId { get; init; }
}
