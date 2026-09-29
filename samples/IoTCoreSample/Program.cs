using ifm.DataStore;
using ifm.IoTCore;
using ifm.IoTCore.Common;
using ifm.IoTCore.Contracts;
using ifm.IoTCore.Device.Contracts;
using ifm.IoTCore.Device.IoTCore;
using ifm.IoTCore.ElementManager.Contracts.Elements;
using ifm.IoTCore.MessageConverter.Json;
using ifm.IoTCore.NetAdapter.Http.Client;
using ifm.IoTCore.Profile.DeviceManagement;
using ifm.IoTCore.Profile.DeviceManagement.ServiceData.Requests;
using ifm.Logger.Contracts;

namespace IoTCoreSample;

internal sealed class Program
{
    private const string IoLinkMasterAddress = "192.168.178.96";

    internal static void Main()
    {
        Uri uri = new UriBuilder(IoLinkMasterAddress).Uri;
        string iotCoreName = $"IoTCore-{uri.DnsSafeHost}-{uri.Port}";
        // The new SDK's element identifiers reject characters like the dots in an IP-address host - sanitize
        // before use as the IoTCore's own identifier.
        string sanitizedIotCoreName = AddressParser.ReplaceInvalidCharacters(iotCoreName, "_");
        ILogger logger = new ConsoleLogger();

        string dataStorePath = Path.Combine(Path.GetTempPath(), $"{iotCoreName}-datastore.json");
        DataStore dataStore = new(dataStorePath);

        IIoTCore core = IoTCoreFactory.Create(sanitizedIotCoreName, dataStore, logger);

        using HttpClientNetAdapterFactory httpClientNetAdapterFactory = new(
            new MessageConverter(), TimeSpan.FromSeconds(30), true, logger);
        core.ClientNetAdapterManager.RegisterClientNetAdapterFactory(httpClientNetAdapterFactory);

        using DeviceManager deviceManager = new();
        // Without a registered IoTCoreDeviceFactory, AddDevice below fails with "device type not supported" -
        // the new SDK requires this to be wired up explicitly, unlike the old SDK.
        using IoTCoreDeviceFactory ioTCoreDeviceFactory = new(
            core.ElementManager, core.PersistenceManager, core.EventSender, logger,
            core.ClientNetAdapterManager, TimeSpan.FromSeconds(30), new Config());
        deviceManager.RegisterDeviceFactory(ioTCoreDeviceFactory);

        ifm.IoTCore.Profile.DeviceManagement.Config.DeviceManagement settings = new(
            // The 3-parameter DeviceMonitor ctor throws internally - use the parameterless ctor instead.
            new ifm.IoTCore.Profile.DeviceManagement.Config.DeviceMonitor(), []);

        DeviceManagementProfileBuilder deviceProfileBuilder = new(
            core.ElementManager, core.ClientNetAdapterManager, core.DataStore, deviceManager, logger, settings);
        deviceProfileBuilder.Build();

        AddDeviceRequestServiceData addRequest = new(
            type: ioTCoreDeviceFactory.Type,
            remoteUri: uri.AbsoluteUri,
            callbackUrl: null,
            alias: null,
            persist: false,
            authentication: null,
            cacheTimeout: null,
            // Must be false - true validates connectivity but does not mount the device into the element tree.
            checkConnection: false);
        deviceProfileBuilder.AddDevice(core.Root, addRequest, acquireLock: true);

        IBaseElement remoteContainer = core.Root.Subs!.First(x => x.Identifier == "remote");
        IBaseElement deviceElement = remoteContainer.Subs![0];

        Console.WriteLine($"Device Identifier of {IoLinkMasterAddress}: {deviceElement.Identifier}");

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    private sealed class ConsoleLogger : ILogger
    {
        // Left silent - the SDK logs the full raw HTTP request/response body at Debug level on every call,
        // which would otherwise bury this sample's own (much shorter) output.
        public void Debug(string message) { }
        public void Info(string message) => Console.WriteLine($"[Info] {message}");
        public void Warning(string message) => Console.WriteLine($"[Warning] {message}");
        public void Error(string message) => Console.WriteLine($"[Error] {message}");
    }
}
