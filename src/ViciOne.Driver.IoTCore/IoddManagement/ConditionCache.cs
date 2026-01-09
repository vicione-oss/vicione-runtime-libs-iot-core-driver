using System;
using System.Collections.Concurrent;

namespace ViciOne.Driver.IoTCore.IoddManagement;

internal class ConditionCache
{
    private class Key(string address, ushort index, byte? subIndex) : Tuple<string, ushort, byte?>(address, index, subIndex);

    private readonly ConcurrentDictionary<Key, byte> _cache = new();

    public void Add(string address, ushort index, byte? subIndex, byte value)
        => _cache.TryAdd(new Key(address, index, subIndex), value);

    public bool Get(string address, ushort index, byte? subIndex, out byte value)
        => _cache.TryGetValue(new Key(address, index, subIndex), out value);

    public void Clear()
        => _cache.Clear();
}
