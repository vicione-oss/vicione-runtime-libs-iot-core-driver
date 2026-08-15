using System;
using System.Collections.Generic;
using ifm.Common.Variant;
using ifm.IoTCore.Contracts;
using ViciOne.Communication.IoTCore.Extensions;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// Reads a set of a mirrored master's element values in one "getdatamulti" request, translating between the
/// mirrored address space its callers use and the master's own address space a "getdatamulti" request carries.
/// </summary>
internal static class PortDataBatchReader
{
    /// <summary>
    /// Reads <paramref name="elementAddresses"/> from the master mirrored under <paramref name="mirroredDeviceAddress"/>
    /// in a single request.
    /// </summary>
    /// <param name="core">The IoTCore the master is mirrored in.</param>
    /// <param name="mirroredDeviceAddress">The mirrored master's device address, i.e. the prefix its element addresses carry.</param>
    /// <param name="elementAddresses">The element addresses to read, in the mirrored address space.</param>
    /// <returns>The read values keyed by their (mirrored) element address; an address the master did not answer with a value is absent.</returns>
    public static Dictionary<string, VariantValue> Read(IIoTCore core, string mirroredDeviceAddress, IReadOnlyCollection<string> elementAddresses)
    {
        Dictionary<string, string> fullByRemote = new(StringComparer.Ordinal);
        foreach (string elementAddress in elementAddresses)
            fullByRemote[ToRemoteRelativeAddress(mirroredDeviceAddress, elementAddress)] = elementAddress;

        Dictionary<string, VariantValue> byRemote = core.GetDataMulti(mirroredDeviceAddress, fullByRemote.Keys);

        Dictionary<string, VariantValue> values = new(StringComparer.Ordinal);
        foreach (KeyValuePair<string, VariantValue> entry in byRemote)
            values[fullByRemote[entry.Key]] = entry.Value;
        return values;
    }

    private static string ToRemoteRelativeAddress(string mirroredDeviceAddress, string elementAddress)
        => elementAddress.StartsWith(mirroredDeviceAddress, StringComparison.Ordinal)
            ? elementAddress[mirroredDeviceAddress.Length..]
            : elementAddress;
}
