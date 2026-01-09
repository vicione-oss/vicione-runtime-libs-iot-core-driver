namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public interface IDeviceTreeParser
{
    Task<IEnumerable<IDeviceTreeBase>> ParseDeviceTreeAsync(IDeviceTreeBase currentDeviceTree);
}
