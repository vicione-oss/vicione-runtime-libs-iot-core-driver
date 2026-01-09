using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Driver.IoTCore.Contracts;

public interface IIoLinkSubscriberSettings
{
    IRuntimeSetting<string> Address { get; }
    IRuntimeSetting<string> ApplicationSpecificTag { get; }
    IRuntimeSetting<uint> DeviceId { get; }
    IRuntimeSetting<ushort> PortIndex { get; }
    IRuntimeSetting<ushort> ProcessDataInIndex { get; }
    // TODO: prüfen, für was das relevant ist
    IRuntimeSetting<string> ProductName { get; }
    IRuntimeSetting<ushort> VendorId { get; }
}
