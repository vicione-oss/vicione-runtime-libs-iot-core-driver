using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Driver.IoTCore.IoddManagement;

namespace ViciOne.Driver.IoTCore;

public static class DeviceManager
{
    private const int ReleaseDelayInSeconds = 30;

    private static readonly Lock s_lock = new();
    private static readonly Dictionary<IManageableDevice, List<Guid>> s_releasableDevices = [];
    private static Task? s_releaseWorker;

    public static IoLinkMaster GetOrAddIoLinkMaster(string address, Guid requestingInstance, ILogger logger)
        => IoddParserManager.IsInitialized == false
            ? throw new InvalidOperationException($"{nameof(DeviceManager)} is not yet initialized.")
            : GetOrAddDevice(address, requestingInstance, () => new IoLinkMaster(address, logger));

    public static VseDevice GetOrAddVseDevice(string address, Guid requestingInstance, ILogger logger)
        => GetOrAddDevice(address, requestingInstance, () => new VseDevice(address, logger));

    private static TDevice GetOrAddDevice<TDevice>(string address, Guid requestingInstance, Func<TDevice> createDevice)
        where TDevice : IManageableDevice
    {
        lock (s_lock)
        {
            foreach (KeyValuePair<IManageableDevice, List<Guid>> pair in s_releasableDevices)
            {
                if (pair.Key is TDevice existingDevice && string.Equals(existingDevice.Address, address, StringComparison.OrdinalIgnoreCase))
                {
                    if (pair.Value.Contains(requestingInstance) == false)
                        pair.Value.Add(requestingInstance);

                    existingDevice.ReleaseDate = null;
                    return existingDevice;
                }
            }

            TDevice newDevice = createDevice();
            s_releasableDevices.Add(newDevice, [requestingInstance]);
            return newDevice;
        }
    }

    public static void Initialize(string ioddDirectory, bool ioddAutoDownload, ILogger logger)
    {
        try { IoddParserManager.Initialize(ioddDirectory, ioddAutoDownload); }
        catch (Exception ex) { logger.IoddParserManagerInitializationErrorOccurred(ex); }
    }

    public static void ReleaseDevices(Guid requestingInstance)
    {
        lock (s_lock)
        {
            foreach (KeyValuePair<IManageableDevice, List<Guid>> pair in s_releasableDevices)
            {
                pair.Value.Remove(requestingInstance);
                if (pair.Value.Count == 0)
                    pair.Key.ReleaseDate = DateTime.UtcNow.AddSeconds(ReleaseDelayInSeconds);
            }

            if (s_releaseWorker is not null)
                return;
            s_releaseWorker = Task.Run(ReleaseWorker);
        }
    }

    private static async Task ReleaseWorker()
    {
        AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(typeof(DeviceManager).Assembly)
            ?? throw new InvalidOperationException("Could not determine the assembly load context.");
        bool isUnloading = false;
        context.Unloading += OnUnloading;

        while (s_releasableDevices.Keys.Any(x => x.ReleaseDate is not null))
        {
            await Task.Delay(500);

            lock (s_lock)
            {
                foreach (IManageableDevice device in s_releasableDevices.Keys.Where(x => x.ReleaseDate is not null).ToArray())
                {
                    if (isUnloading || (device.ReleaseDate.HasValue && DateTime.UtcNow >= device.ReleaseDate))
                    {
                        device.Dispose();
                        s_releasableDevices.Remove(device);
                    }
                }
            }
        }

        context.Unloading -= OnUnloading;

        lock (s_lock)
            s_releaseWorker = null;

        void OnUnloading(AssemblyLoadContext context)
            => Interlocked.Exchange(ref isUnloading, true);
    }
}
