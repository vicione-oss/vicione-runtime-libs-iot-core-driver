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
/// Reference-counts <see cref="IoLinkMaster"/> instances shared - across the FunctionBlocks and driver
/// instances that read through them - under the connection identifier they were registered by.
/// <para>A master is only actually disposed once every requesting instance has released it, and even then only
/// after a grace delay (<see cref="ReleaseDelayInSeconds"/>) - so a driver instance that re-requests the same
/// identifier shortly after releasing it (e.g. across an Engine restart) gets the same, still-connected master
/// back instead of paying to reconnect.</para>
/// </summary>
public static class DeviceManager
{
    private const int ReleaseDelayInSeconds = 30;

    private static readonly Lock s_lock = new();
    private static readonly Dictionary<string, Registration> s_connections = new(StringComparer.OrdinalIgnoreCase);
    private static Task? s_releaseWorker;

    /// <summary>
    /// One shared master together with the driver instances currently keeping it alive.
    /// </summary>
    private sealed class Registration(IoLinkMaster master)
    {
        public IoLinkMaster Master { get; } = master;
        public List<Guid> Requesters { get; } = [];
    }

    /// <summary>
    /// Gets the master registered under <paramref name="identifier"/>, or creates one for
    /// <paramref name="address"/> and registers it if none is registered under that identifier yet.
    /// <para>Either way, <paramref name="requestingInstance"/> is added to the master's requester list (undoing
    /// any pending release scheduled by <see cref="ReleaseDevices"/>) so it counts toward keeping the master
    /// alive. If the identifier is already registered but now points at a different <paramref name="address"/>,
    /// the stale master is dropped and replaced.</para>
    /// </summary>
    /// <param name="identifier">The label the connection is known by, under which the master is registered so FunctionBlocks reach it through <see cref="GetIoLinkMasterByIdentifier"/>.</param>
    /// <param name="address">The master's address, used to create the master when the identifier isn't registered yet.</param>
    /// <param name="requestingInstance">An identifier for the calling driver instance, later passed to <see cref="ReleaseDevices"/>.</param>
    /// <param name="logger">Logger for the master, if one has to be created.</param>
    /// <param name="username">The username to authenticate with, or <see langword="null"/>/empty for no authentication. Only used if a new master is created.</param>
    /// <param name="password">The password to authenticate with, ignored if <paramref name="username"/> is empty.</param>
    /// <param name="useGetDataMulti">Whether the master should read every port's cyclic data in one batched request per cycle. Only used if a new master is created.</param>
    /// <returns>The shared <see cref="IoLinkMaster"/> registered under <paramref name="identifier"/>.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> hasn't been called yet.</exception>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The master's lifetime is owned by the registry and it is disposed by the release worker.")]
    public static IoLinkMaster GetOrAddIoLinkMaster(string identifier, string address, Guid requestingInstance, ILogger logger, string? username = null, string? password = null, bool useGetDataMulti = false)
    {
        if (IoddCatalog.IsInitialized == false)
            throw new InvalidOperationException($"{nameof(DeviceManager)} is not yet initialized.");

        lock (s_lock)
        {
            if (s_connections.TryGetValue(identifier, out Registration? registration) &&
                string.Equals(registration.Master.Address, address, StringComparison.OrdinalIgnoreCase) == false)
            {
                // The connection now points at a different address - the master under this identifier is stale.
                registration.Master.Dispose();
                s_connections.Remove(identifier);
                registration = null;
            }

            if (registration is null)
            {
                IoLinkMaster master = new(address, logger,
                    string.IsNullOrEmpty(username) ? null : username,
                    string.IsNullOrEmpty(username) ? null : password,
                    useGetDataMulti);
                registration = new Registration(master);
                s_connections[identifier] = registration;
            }

            AttachRequester(registration, requestingInstance);
            return registration.Master;
        }
    }

    /// <summary>
    /// Gets the master registered under <paramref name="identifier"/>, counting
    /// <paramref name="requestingInstance"/> toward keeping it alive as <see cref="GetOrAddIoLinkMaster"/> does.
    /// </summary>
    /// <param name="identifier">The label the master's connection is known by.</param>
    /// <param name="requestingInstance">An identifier for the calling driver instance, later passed to <see cref="ReleaseDevices"/>.</param>
    /// <returns>The master registered under the label.</returns>
    /// <exception cref="InvalidOperationException">No master is registered under <paramref name="identifier"/>.</exception>
    public static IoLinkMaster GetIoLinkMasterByIdentifier(string identifier, Guid requestingInstance)
    {
        lock (s_lock)
        {
            if (s_connections.TryGetValue(identifier, out Registration? registration) == false)
                throw new InvalidOperationException($"No IO-Link master is configured for identifier '{identifier}'.");

            AttachRequester(registration, requestingInstance);
            return registration.Master;
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
    /// Removes <paramref name="requestingInstance"/> from every master's requester list.
    /// <para>Any master left with no requesters is scheduled for release after <see cref="ReleaseDelayInSeconds"/>
    /// (see <see cref="ReleaseWorker"/>), and the release worker is started if it isn't already running.</para>
    /// </summary>
    /// <param name="requestingInstance">The driver instance identifier previously passed to <see cref="GetOrAddIoLinkMaster"/>.</param>
    public static void ReleaseDevices(Guid requestingInstance)
    {
        lock (s_lock)
        {
            foreach (Registration registration in s_connections.Values)
            {
                registration.Requesters.Remove(requestingInstance);
                if (registration.Requesters.Count == 0)
                    registration.Master.ReleaseDate = DateTime.UtcNow.AddSeconds(ReleaseDelayInSeconds);
            }

            if (s_releaseWorker is not null)
                return;
            s_releaseWorker = Task.Run(ReleaseWorker);
        }
    }

    private static void AttachRequester(Registration registration, Guid requestingInstance)
    {
        if (registration.Requesters.Contains(requestingInstance) == false)
            registration.Requesters.Add(requestingInstance);

        registration.Master.ReleaseDate = null;
    }

    /// <summary>
    /// Polls every 500 ms for masters whose <c>ReleaseDate</c> has passed, disposing and removing them.
    /// <para>Also subscribes to the assembly load context's <c>Unloading</c> event, so a process shutdown
    /// disposes every still-scheduled master immediately instead of waiting out its grace delay. Exits once no
    /// master has a pending release left.</para>
    /// </summary>
    private static async Task ReleaseWorker()
    {
        AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(typeof(DeviceManager).Assembly)
            ?? throw new InvalidOperationException("Could not determine the assembly load context.");
        bool isUnloading = false;
        context.Unloading += OnUnloading;

        while (s_connections.Values.Any(x => x.Master.ReleaseDate is not null))
        {
            await Task.Delay(500);

            lock (s_lock)
            {
                foreach (KeyValuePair<string, Registration> pair in s_connections.Where(x => x.Value.Master.ReleaseDate is not null).ToArray())
                {
                    if (isUnloading || (pair.Value.Master.ReleaseDate.HasValue && DateTime.UtcNow >= pair.Value.Master.ReleaseDate))
                    {
                        pair.Value.Master.Dispose();
                        s_connections.Remove(pair.Key);
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
