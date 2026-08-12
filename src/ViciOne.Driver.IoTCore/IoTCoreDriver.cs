using System;
using ViciOne.Core.Runtime;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// The Engine runtime driver for IO-Link masters - owns no device state itself, only the identity used to
/// share/release <see cref="IoLinkMaster"/> instances through <see cref="DeviceManager"/>.
/// </summary>
public sealed class IoTCoreDriver : RuntimeDriver<IoTCoreDriver>
{
    private bool _disposed;
    private readonly Guid _requestingIdentifier = Guid.NewGuid();
    private bool _resetIoLinkMasterConditionValueCache = true;

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_disposed)
            return;

        DeviceManager.ReleaseDevices(_requestingIdentifier);
        _disposed = true;
    }

    /// <summary>
    /// Gets or creates the shared <see cref="IoLinkMaster"/> for <paramref name="address"/> via
    /// <see cref="DeviceManager"/>.
    /// </summary>
    /// <param name="address">The IO-Link master's address.</param>
    /// <param name="username">The username to authenticate with, or <see langword="null"/>/empty for no authentication. Only used if the master isn't already shared from a previous call.</param>
    /// <param name="password">The password to authenticate with, ignored if <paramref name="username"/> is empty.</param>
    /// <returns>The shared <see cref="IoLinkMaster"/> instance.</returns>
    internal IoLinkMaster GetOrAddIoLinkMaster(string address, string? username = null, string? password = null)
    {
        IoLinkMaster master = DeviceManager.GetOrAddIoLinkMaster(address, _requestingIdentifier, Logger, username, password);

        // The condition cache is keyed by (address, index, subIndex, vendorId, deviceId), so a device swap
        // already invalidates it on its own - but a condition variable changed on the *same* still-connected
        // device (e.g. a mode switch on the sensor itself, not a reconnect) has no other invalidation path.
        // Since IoLinkMaster can outlive this driver instance (see DeviceManager's reuse-with-grace-period),
        // force one fresh read the first time this (possibly new) driver instance uses it, so a redeploy is
        // enough to pick up such a change instead of it being stuck until the device is physically reconnected.
        if (_resetIoLinkMasterConditionValueCache)
        {
            _resetIoLinkMasterConditionValueCache = false;
            master.ClearConditionValueCache();
        }

        return master;
    }
}
