using System;
using ViciOne.Core.Runtime;

namespace ViciOne.Driver.Vse;

/// <summary>
/// The Engine runtime driver for VSE devices - owns no device state itself, only the identity used to
/// share/release <see cref="VseDevice"/> instances through <see cref="DeviceManager"/>.
/// </summary>
public sealed class VseDriver : RuntimeDriver<VseDriver>
{
    private bool _disposed;
    private readonly Guid _requestingIdentifier = Guid.NewGuid();

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (_disposed)
            return;

        DeviceManager.ReleaseDevices(_requestingIdentifier);
        _disposed = true;
    }

    /// <summary>
    /// Gets or creates the shared <see cref="VseDevice"/> for <paramref name="address"/> via <see cref="DeviceManager"/>.
    /// </summary>
    internal VseDevice GetOrAddVseDevice(string address)
        => DeviceManager.GetOrAddVseDevice(address, _requestingIdentifier, Logger);
}
