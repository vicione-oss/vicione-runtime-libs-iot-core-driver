using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.Vse;

/// <summary>
/// Shares one <see cref="VseDevice"/> per network address across every <see cref="VseDriver"/> instance that
/// requests it, disposing it immediately once none of them still need it.
/// </summary>
internal static class DeviceManager
{
    private static readonly Lock s_lock = new();
    private static readonly Dictionary<VseDevice, List<Guid>> s_devices = [];

    /// <summary>
    /// Gets the shared <see cref="VseDevice"/> for <paramref name="address"/>, creating it if none exists yet, and
    /// registers <paramref name="requestingInstance"/> as one of its current requesters.
    /// </summary>
    /// <param name="address">The Vse device's address.</param>
    /// <param name="requestingInstance">An identifier for the requesting <see cref="VseDriver"/> instance.</param>
    /// <param name="logger">The logger to pass to a newly created device.</param>
    /// <returns>The shared <see cref="VseDevice"/> instance.</returns>
    public static VseDevice GetOrAddVseDevice(string address, Guid requestingInstance, ILogger logger)
    {
        lock (s_lock)
        {
            foreach (KeyValuePair<VseDevice, List<Guid>> pair in s_devices)
            {
                if (string.Equals(pair.Key.DeviceAddress, address, StringComparison.OrdinalIgnoreCase))
                {
                    if (pair.Value.Contains(requestingInstance) == false)
                        pair.Value.Add(requestingInstance);

                    return pair.Key;
                }
            }

            VseDevice newDevice = new(address, logger);
            s_devices.Add(newDevice, [requestingInstance]);
            return newDevice;
        }
    }

    /// <summary>
    /// Unregisters <paramref name="requestingInstance"/> from every device it requested, disposing and removing
    /// any device left with no requesters. Release is immediate (no grace delay), matching how the VSE device was
    /// always treated.
    /// </summary>
    /// <param name="requestingInstance">The requester identifier to unregister, as passed to <see cref="GetOrAddVseDevice"/>.</param>
    public static void ReleaseDevices(Guid requestingInstance)
    {
        lock (s_lock)
        {
            foreach (VseDevice device in s_devices.Keys.ToArray())
            {
                List<Guid> requesters = s_devices[device];
                requesters.Remove(requestingInstance);
                if (requesters.Count == 0)
                {
                    device.Dispose();
                    s_devices.Remove(device);
                }
            }
        }
    }
}
