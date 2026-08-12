using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.IoTCore.IoddStore;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// Reference-counts <see cref="IoLinkMaster"/> instances shared across multiple driver instances that target
/// the same device address.
/// <para>A device is only actually disposed once every requesting instance has released it, and even then only
/// after a grace delay (<see cref="ReleaseDelayInSeconds"/>) - so a driver instance that re-requests the same
/// address shortly after releasing it (e.g. across an Engine restart) gets the same, still-connected device
/// back instead of paying to reconnect.</para>
/// </summary>
public static class DeviceManager
{
    private const int ReleaseDelayInSeconds = 30;

    private static readonly Lock s_lock = new();
    private static readonly Dictionary<IoLinkMaster, List<Guid>> s_releasableDevices = [];
    private static Task? s_releaseWorker;

    /// <summary>
    /// Gets the existing <see cref="IoLinkMaster"/> for <paramref name="address"/>, or creates a new one if
    /// none exists yet.
    /// <para>Either way, <paramref name="requestingInstance"/> is added to the device's requester list (undoing
    /// any pending release scheduled by <see cref="ReleaseDevices"/>) so it counts toward keeping the device
    /// alive.</para>
    /// </summary>
    /// <param name="address">The device's address.</param>
    /// <param name="requestingInstance">An identifier for the calling driver instance, later passed to <see cref="ReleaseDevices"/>.</param>
    /// <param name="logger">Logger for the device, if one has to be created.</param>
    /// <param name="username">The username to authenticate with, or <see langword="null"/>/empty for no authentication. Only used if a new device is created.</param>
    /// <param name="password">The password to authenticate with, ignored if <paramref name="username"/> is empty.</param>
    /// <returns>The shared <see cref="IoLinkMaster"/> instance for <paramref name="address"/>.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> hasn't been called yet.</exception>
    public static IoLinkMaster GetOrAddIoLinkMaster(string address, Guid requestingInstance, ILogger logger, string? username = null, string? password = null)
    {
        if (IoddCatalog.IsInitialized == false)
            throw new InvalidOperationException($"{nameof(DeviceManager)} is not yet initialized.");

        lock (s_lock)
        {
            foreach (KeyValuePair<IoLinkMaster, List<Guid>> pair in s_releasableDevices)
            {
                if (string.Equals(pair.Key.Address, address, StringComparison.OrdinalIgnoreCase))
                {
                    if (pair.Value.Contains(requestingInstance) == false)
                        pair.Value.Add(requestingInstance);

                    pair.Key.ReleaseDate = null;
                    return pair.Key;
                }
            }

            IoLinkMaster newDevice = new(address, logger,
                string.IsNullOrEmpty(username) ? null : username,
                string.IsNullOrEmpty(username) ? null : password);
            s_releasableDevices.Add(newDevice, [requestingInstance]);
            return newDevice;
        }
    }

    /// <summary>
    /// Initializes the shared <see cref="IoddCatalog"/> used to identify devices' IO-Link process data.
    /// </summary>
    /// <param name="ioddDirectory">The root directory IODDs are stored under.</param>
    /// <param name="ioddAutoDownload">Initial value for <see cref="IoddCatalog.IoddAutoDownload"/>.</param>
    /// <param name="logger">Logger for a failure to initialize.</param>
    public static void Initialize(string ioddDirectory, bool ioddAutoDownload, ILogger logger)
    {
        try { IoddCatalog.Initialize(ioddDirectory, ioddAutoDownload); }
        catch (Exception ex) { logger.IoddCatalogInitializationErrorOccurred(ex); }
    }

    /// <summary>
    /// Removes <paramref name="requestingInstance"/> from every device's requester list.
    /// <para>Any device left with no requesters is scheduled for release after <see cref="ReleaseDelayInSeconds"/>
    /// (see <see cref="ReleaseWorker"/>), and the release worker is started if it isn't already running.</para>
    /// </summary>
    /// <param name="requestingInstance">The driver instance identifier previously passed to <see cref="GetOrAddIoLinkMaster"/>.</param>
    public static void ReleaseDevices(Guid requestingInstance)
    {
        lock (s_lock)
        {
            foreach (KeyValuePair<IoLinkMaster, List<Guid>> pair in s_releasableDevices)
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

    /// <summary>
    /// Polls every 500 ms for devices whose <c>ReleaseDate</c> has passed, disposing and removing them.
    /// <para>Also subscribes to the assembly load context's <c>Unloading</c> event, so a process shutdown
    /// disposes every still-scheduled device immediately instead of waiting out its grace delay. Exits once no
    /// device has a pending release left.</para>
    /// </summary>
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
                foreach (IoLinkMaster device in s_releasableDevices.Keys.Where(x => x.ReleaseDate is not null).ToArray())
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
