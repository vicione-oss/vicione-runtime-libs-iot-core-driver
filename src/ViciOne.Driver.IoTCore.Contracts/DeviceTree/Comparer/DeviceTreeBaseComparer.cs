namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree.Comparer;

public sealed class DeviceTreeBaseComparer : IComparer<IDeviceTreeBase>
{
    public static DeviceTreeBaseComparer Default { get; } = new DeviceTreeBaseComparer();

    private DeviceTreeBaseComparer() { }

    public int Compare(IDeviceTreeBase? x, IDeviceTreeBase? y)
    {
        if (x is null && y is null)
            return 0;
        else if (x is null)
            return -1;
        else if (y is null)
            return 1;

        return x is IDeviceTreeAliasNode xItem && y is IDeviceTreeAliasNode yItem
            ? AlphaNumericComparer<string>.Default.Compare($"{xItem.NameAlias}.{x.Name}", $"{yItem.NameAlias}.{y.Name}")
            : AlphaNumericComparer<string>.Default.Compare(x.Name, y.Name);
    }
}
