using System.Reflection;

namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;

// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class DeviceTreeBaseExtensions
{
    extension(IDeviceTreeBase tree)
    {
        public IDeviceTreeBase Clone()
        {
            object? instance = Activator.CreateInstance(tree.GetType());

            IEnumerable<PropertyInfo> properties = tree.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite);

            foreach (PropertyInfo property in properties)
                property.SetValue(instance, property.GetValue(tree));

            return (instance as IDeviceTreeBase)!;
        }

        public IEnumerable<IDeviceTreeBase> GetNodeAndDescendants()
            => new[] { tree }.Concat(tree.Children.SelectMany(child => child.GetNodeAndDescendants()));

        public string GetRoot()
        {
            string[] elements = tree.Id.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return elements.Length >= 3
                ? $"{elements[0]}/{elements[1]}/{elements[2]}"
                : string.Empty;
        }

        public void SetIsOffline(bool isOffline)
        {
            tree.IsOffline = isOffline;
            foreach (IDeviceTreeBase child in tree.Children)
                child.SetIsOffline(isOffline);
        }
    }
}
