using System.Reflection;

namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

public static class DeviceTreeBaseExtensions
{
    public static IDeviceTreeBase Clone(this IDeviceTreeBase tree)
    {
        object? instance = Activator.CreateInstance(tree.GetType());

        IEnumerable<PropertyInfo> properties
            = tree.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite);

        foreach (PropertyInfo property in properties)
            property.SetValue(instance, property.GetValue(tree));

        return (instance as IDeviceTreeBase)!;
    }

    public static IEnumerable<IDeviceTreeBase> GetNodeAndDescendants(this IDeviceTreeBase tree)
        => new[] { tree }.Concat(tree.Children.SelectMany(child => child.GetNodeAndDescendants()));

    public static string GetRoot(this IDeviceTreeBase tree)
    {
        string[] elements = tree.Id.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return elements.Length >= 3
            ? $"{elements[0]}/{elements[1]}/{elements[2]}"
            : string.Empty;
    }

    public static void SetIsOffline(this IDeviceTreeBase tree, bool isOffline)
    {
        tree.IsOffline = isOffline;
        foreach (IDeviceTreeBase child in tree.Children)
            child.SetIsOffline(isOffline);
    }
}
