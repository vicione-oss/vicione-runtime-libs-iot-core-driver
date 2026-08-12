using System;
using System.Collections.Concurrent;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// Caches the values IODD <c>Condition</c> elements read from a device while decoding process data.
/// <para>A condition variable's value never changes at runtime, so once read it can be reused for every later
/// decode instead of being re-read from the device each time.</para>
/// </summary>
internal sealed class ConditionCache
{
    // Keyed by device identity (vendorId/deviceId) in addition to the port address: a device swap at
    // the same port naturally produces a cache miss instead of requiring the whole cache to be cleared.
    private class Key(string address, ushort index, byte? subIndex, ushort vendorId, uint deviceId) : Tuple<string, ushort, byte?, ushort, uint>(address, index, subIndex, vendorId, deviceId);

    private readonly ConcurrentDictionary<Key, byte> _cache = new();

    /// <summary>
    /// Caches a condition variable's value.
    /// </summary>
    /// <param name="address">The port's element address.</param>
    /// <param name="index">The condition variable's ISDU index.</param>
    /// <param name="subIndex">The condition variable's ISDU sub-index, if any.</param>
    /// <param name="vendorId">The connected device's vendor ID.</param>
    /// <param name="deviceId">The connected device's device ID.</param>
    /// <param name="value">The value read from the device.</param>
    public void Add(string address, ushort index, byte? subIndex, ushort vendorId, uint deviceId, byte value)
        => _cache.TryAdd(new Key(address, index, subIndex, vendorId, deviceId), value);

    /// <summary>
    /// Looks up a previously cached condition variable's value.
    /// </summary>
    /// <param name="address">The port's element address.</param>
    /// <param name="index">The condition variable's ISDU index.</param>
    /// <param name="subIndex">The condition variable's ISDU sub-index, if any.</param>
    /// <param name="vendorId">The connected device's vendor ID.</param>
    /// <param name="deviceId">The connected device's device ID.</param>
    /// <param name="value">The cached value, if this method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> if a value was cached for this exact address/index/sub-index/vendor/device combination.</returns>
    public bool Get(string address, ushort index, byte? subIndex, ushort vendorId, uint deviceId, out byte value)
        => _cache.TryGetValue(new Key(address, index, subIndex, vendorId, deviceId), out value);

    /// <summary>
    /// Clears every cached value.
    /// </summary>
    public void Clear()
        => _cache.Clear();
}
