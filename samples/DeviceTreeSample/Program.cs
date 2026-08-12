using System.Text.Json;
using Microsoft.Extensions.Logging;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.IoTCore;

namespace DeviceTreeSample;

internal sealed class Program
{
    private static readonly string s_ioddDirectory = Path.GetFullPath(@"..\..\..\..\..\artifacts\IODDs");
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    private const string DeviceAddress = "192.168.178.96";

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

        // Same call (type + options) DeviceTreeSubscriber's IRuntimeConnector<DeviceTreeIoLinkMaster> output uses
        // to serialize this tree for the engineering UI - i.e. exactly what that FunctionBlock would actually
        // send over the wire.
        Console.WriteLine("===== JSON =====");
        Console.WriteLine(JsonSerializer.Serialize(deviceTree, s_jsonOptions));
        Console.WriteLine("===== END JSON =====");
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
