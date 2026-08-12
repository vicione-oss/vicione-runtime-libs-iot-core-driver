namespace ViciOne.DeviceTree.Contracts.Comparer;

/// <summary>
/// Sorts device tree nodes alphanumerically by display name, preferring an <see cref="IDeviceTreeAliasNode.Alias"/>
/// over the plain <see cref="IDeviceTreeBase.Name"/> when both sides support one.
/// </summary>
public sealed class DeviceTreeBaseComparer : IComparer<IDeviceTreeBase>
{
    /// <summary>
    /// The comparer instance.
    /// </summary>
    public static DeviceTreeBaseComparer Default { get; } = new DeviceTreeBaseComparer();

    private DeviceTreeBaseComparer() { }

    /// <inheritdoc/>
    public int Compare(IDeviceTreeBase? x, IDeviceTreeBase? y)
    {
        if (x is null && y is null)
            return 0;
        else if (x is null)
            return -1;
        else if (y is null)
            return 1;

        return x is IDeviceTreeAliasNode xItem && y is IDeviceTreeAliasNode yItem
            ? AlphaNumericComparer<string>.Default.Compare($"{xItem.Alias}.{x.Name}", $"{yItem.Alias}.{y.Name}")
            : AlphaNumericComparer<string>.Default.Compare(x.Name, y.Name);
    }
}
