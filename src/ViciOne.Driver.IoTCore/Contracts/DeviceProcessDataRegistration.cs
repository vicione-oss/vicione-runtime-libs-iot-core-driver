using System.Collections.Concurrent;
using ifm.IoddParser.Contracts;

namespace ViciOne.Driver.IoTCore.Contracts;

public sealed class DeviceProcessDataRegistration
{
    public IDevice? DeviceDefinition { get; set; }
    public required uint DeviceId { get; init; }
    public ConcurrentDictionary<int, ConcurrentDictionary<IIoLinkSubscriberFunctionBlock, byte>> SubIndexRegistrations { get; } = [];
    public required ushort VendorId { get; init; }
}
