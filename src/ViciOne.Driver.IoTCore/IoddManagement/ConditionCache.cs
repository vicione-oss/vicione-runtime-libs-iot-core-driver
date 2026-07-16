using System;
using System.Collections.Concurrent;

namespace ViciOne.Driver.IoTCore.IoddManagement;

internal class ConditionCache
{
    // Keyed by device identity (vendorId/deviceId) in addition to the port address: a device swap at
    // the same port naturally produces a cache miss instead of requiring the whole cache to be cleared.
    private class Key(string address, ushort index, byte? subIndex, ushort vendorId, uint deviceId) : Tuple<string, ushort, byte?, ushort, uint>(address, index, subIndex, vendorId, deviceId);

    private readonly ConcurrentDictionary<Key, byte> _cache = new();

    public void Add(string address, ushort index, byte? subIndex, ushort vendorId, uint deviceId, byte value)
        => _cache.TryAdd(new Key(address, index, subIndex, vendorId, deviceId), value);

    public bool Get(string address, ushort index, byte? subIndex, ushort vendorId, uint deviceId, out byte value)
        => _cache.TryGetValue(new Key(address, index, subIndex, vendorId, deviceId), out value);

    public void Clear()
        => _cache.Clear();
}
