using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.Vse;

/// <summary>
/// Shares one <see cref="VseDevice"/> per network address across every <see cref="VseDriver"/> instance that
/// requests it, releasing it once none of them still need it.
/// </summary>
internal static class DeviceManager
{
    private const int ReleaseDelayInSeconds = 30;

    private static readonly Lock s_lock = new();
    private static readonly Dictionary<VseDevice, List<Guid>> s_releasableDevices = [];
    private static Task? s_releaseWorker;

    /// <summary>
    /// Gets the shared <see cref="VseDevice"/> for <paramref name="address"/>, creating it if none exists yet.
    /// <para>Cancels a pending release for an existing device and registers <paramref name="requestingInstance"/>
    /// as one of its current requesters, so a later <see cref="ReleaseDevices"/> call for a different requester
    /// does not tear it down while this one is still using it.</para>
    /// </summary>
    /// <param name="address">The VSE device's address.</param>
    /// <param name="requestingInstance">An identifier for the requesting <see cref="VseDriver"/> instance.</param>
    /// <param name="logger">The logger to pass to a newly created device.</param>
    /// <returns>The shared <see cref="VseDevice"/> instance.</returns>
    public static VseDevice GetOrAddVseDevice(string address, Guid requestingInstance, ILogger logger)
    {
        lock (s_lock)
        {
            foreach (KeyValuePair<VseDevice, List<Guid>> pair in s_releasableDevices)
            {
                if (string.Equals(pair.Key.DeviceAddress, address, StringComparison.OrdinalIgnoreCase))
                {
                    if (pair.Value.Contains(requestingInstance) == false)
                        pair.Value.Add(requestingInstance);

                    pair.Key.ReleaseDate = null;
                    return pair.Key;
                }
            }

            VseDevice newDevice = new(address, logger);
            s_releasableDevices.Add(newDevice, [requestingInstance]);
            return newDevice;
        }
    }

    /// <summary>
    /// Unregisters <paramref name="requestingInstance"/> from every device it requested, scheduling a device for
    /// disposal once none of its requesters remain.
    /// <para>Disposal is delayed by <see cref="ReleaseDelayInSeconds"/> rather than immediate, so a device isn't
    /// torn down and immediately recreated when driver instances reconfigure in quick succession.</para>
    /// </summary>
    /// <param name="requestingInstance">The requester identifier to unregister, as passed to <see cref="GetOrAddVseDevice"/>.</param>
    public static void ReleaseDevices(Guid requestingInstance)
    {
        lock (s_lock)
        {
            foreach (KeyValuePair<VseDevice, List<Guid>> pair in s_releasableDevices)
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
    /// Periodically disposes and removes every device whose <see cref="VseDevice.ReleaseDate"/> is due, until
    /// none remain scheduled for release.
    /// </summary>
    private static async Task ReleaseWorker()
    {
        while (s_releasableDevices.Keys.Any(x => x.ReleaseDate is not null))
        {
            await Task.Delay(500);

            lock (s_lock)
            {
                foreach (VseDevice device in s_releasableDevices.Keys.Where(x => x.ReleaseDate is not null).ToArray())
                {
                    device.Dispose();
                    s_releasableDevices.Remove(device);
                }
            }
        }

        lock (s_lock)
            s_releaseWorker = null;
    }
}
