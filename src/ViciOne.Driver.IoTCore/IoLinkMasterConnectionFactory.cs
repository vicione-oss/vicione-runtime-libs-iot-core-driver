using System;
using System.IO;
using ifm.DataStore;
using ifm.IoTCore;
using ifm.IoTCore.Common;
using ifm.IoTCore.Contracts;
using ifm.IoTCore.Device.IoTCore;
using ifm.IoTCore.Profile.DeviceManagement;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.IoTCore;
using ViciOne.DeviceTree.Contracts;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// The new SDK types that make up one master connection, bundled together so building a connection is a single,
/// self-contained step in <see cref="IoLinkMaster"/>'s constructor instead of a dozen loose local variables.
/// </summary>
internal readonly record struct ConnectionComponents(
    IIoTCore Core,
    IDisposable ClientNetAdapterFactory,
    ifm.IoTCore.Device.Contracts.DeviceManager DeviceManager,
    string DeviceType,
    DeviceManagementProfileBuilder ProfileBuilder);

/// <summary>
/// Wires up an unmirrored <see cref="IIoTCore"/> connection to one IO-Link master.
/// </summary>
internal static class IoLinkMasterConnectionFactory
{
    /// <summary>
    /// Wires up a new, unmirrored <see cref="IIoTCore"/> connection: data store, HTTP net adapter, device
    /// factory registration, and device-management profile builder - everything needed before the master can
    /// call <c>AddDevice</c>.
    /// </summary>
    /// <param name="uri">The master's address, whose scheme decides the transport: an HTTPS address is reached over HTTPS accepting the master's self-signed certificate, any other over plain HTTP.</param>
    /// <param name="logger">The logger every SDK component is given, adapted to the new SDK's own logger type.</param>
    /// <param name="valueCacheTimeout">The device factory's fallback value-cache lifetime, used for devices added without one of their own.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership of the created disposables transfers to the returned ConnectionComponents; IoLinkMaster stores them in fields and disposes them in Dispose().")]
    public static ConnectionComponents Build(Uri uri, ILogger logger, TimeSpan valueCacheTimeout)
    {
        ifm.Logger.Contracts.ILogger ifmLogger = new MicrosoftLoggerAdapter(logger);
        bool useHttps = uri.Scheme == Uri.UriSchemeHttps;

        string iotCoreName = $"IoTCore-{uri.DnsSafeHost}-{uri.Port}";
        // The new SDK's element identifiers reject characters like the dots in an IP-address host (throws
        // ArgumentException from BaseElement's ctor) - sanitize before use as the IoTCore's own identifier.
        // The unsanitized name is still used below for the (unrelated) data store file name.
        string sanitizedIotCoreName = AddressParser.ReplaceInvalidCharacters(iotCoreName, "_");

        string dataStorePath = Path.Combine(Path.GetTempPath(), $"{iotCoreName}-datastore.json");
        DataStore dataStore = new(dataStorePath);

        IIoTCore core = IoTCoreFactory.Create(sanitizedIotCoreName, dataStore, ifmLogger);

        IDisposable clientNetAdapterFactory = ClientNetAdapters.Register(core, useHttps, TimeSpan.FromSeconds(10), ifmLogger);

        ifm.IoTCore.Device.Contracts.DeviceManager deviceManager = new();
        // Without a registered IoTCoreDeviceFactory, AddDevice fails with "device type not supported" - this is
        // the new SDK's replacement for what the old SDK handled internally.
        IoTCoreDeviceFactory ioTCoreDeviceFactory = new(
            core.ElementManager, core.PersistenceManager, core.EventSender, ifmLogger,
            core.ClientNetAdapterManager, valueCacheTimeout, new Config());
        deviceManager.RegisterDeviceFactory(ioTCoreDeviceFactory);

        ifm.IoTCore.Profile.DeviceManagement.Config.DeviceManagement settings = new(
            // The 3-parameter DeviceMonitor ctor (heartbeat, treeChanged, interval) throws
            // "Invalid value '0' for parameter 'interval'" internally - use the parameterless ctor instead.
            new ifm.IoTCore.Profile.DeviceManagement.Config.DeviceMonitor(), []);

        DeviceManagementProfileBuilder profileBuilder = new(
            core.ElementManager, core.ClientNetAdapterManager, core.DataStore, deviceManager, ifmLogger, settings);
        profileBuilder.Build();

        return new ConnectionComponents(core, clientNetAdapterFactory, deviceManager, ioTCoreDeviceFactory.Type, profileBuilder);
    }
}
