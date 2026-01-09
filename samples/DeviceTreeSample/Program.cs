using Microsoft.Extensions.Logging;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace DeviceTreeSample;

internal class Program
{
    private static readonly string s_ioddDirectory = Path.GetFullPath(@"..\..\..\..\..\artifacts\IODDs");

    private const string DeviceAddress = "10.45.24.141";

    internal static async Task Main()
    {
        DeviceTreeIoLinkMaster deviceTree = await GetDeviceTree(DeviceAddress, s_ioddDirectory);

        Console.WriteLine();
        Console.WriteLine($"{DeviceAddress}: {deviceTree.DeviceFamily} {deviceTree.Name}");
        Console.WriteLine();
        foreach (IDeviceTreeBase port in deviceTree.Children)
        {
            Console.WriteLine($"  {port.Name}");
            foreach (IDeviceTreeBase device in port.Children)
            {
                Console.WriteLine($"    {device.Name}{GetDescription(device.Description)}");
                foreach (IDeviceTreeBase dataEntry in device.Children)
                    Console.WriteLine($"      {dataEntry.Name}{GetDescription(dataEntry.Description)}");
            }
            Console.WriteLine();
        }
    }

    private static string GetDescription(DeviceTreeNodeDescription? description)
        => string.IsNullOrWhiteSpace(description?.Text) == false
            ? $" ({description?.Text})"
            : string.Empty;

    private static async Task<DeviceTreeIoLinkMaster> GetDeviceTree(string deviceAddress, string ioddDirectory)
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));
        ILogger logger = loggerFactory.CreateLogger(nameof(IoLinkParser));

        IoLinkParser.Initialize(ioddDirectory, true, logger);

        var instanceHandle = Guid.NewGuid();
        IoLinkMaster device = DeviceManager.GetOrAddIoLinkMaster(deviceAddress, instanceHandle, logger);
        DeviceTreeIoLinkMaster deviceTree = await IoLinkParser.ParseDeviceAsync(device, logger);
        DeviceManager.ReleaseDevices(instanceHandle);

        return deviceTree;
    }
}
