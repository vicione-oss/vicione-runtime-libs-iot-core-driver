using System;
using ViciOne.Core.Runtime;

namespace ViciOne.Driver.IoTCore;

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

    internal IoLinkMaster GetOrAddIoLinkMaster(string address)
    {
        IoLinkMaster master = DeviceManager.GetOrAddIoLinkMaster(address, _requestingIdentifier, Logger);

        // TODO: wird das noch benötigt?
        if (_resetIoLinkMasterConditionValueCache)
        {
            _resetIoLinkMasterConditionValueCache = false;
            master.ClearConditionValueCache();
        }

        return master;
    }


    internal VseDevice GetOrAddVseDevice(string address)
        => DeviceManager.GetOrAddVseDevice(address, _requestingIdentifier, Logger);
}
