using System.Reflection;

namespace ViciOne.DeviceTree.Contracts.Extensions;

/// <summary>
/// Extension members for <see cref="IDeviceTreeBase"/>.
/// </summary>
// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class DeviceTreeBaseExtensions
{
    extension(IDeviceTreeBase tree)
    {
        /// <summary>
        /// Creates a shallow copy of this node via reflection over its readable/writable properties.
        /// <para>The clone's <see cref="IDeviceTreeBase.Children"/> list is a new list, but its elements are the
        /// same child instances - children are not themselves cloned.</para>
        /// </summary>
        public IDeviceTreeBase Clone()
        {
            object? instance = Activator.CreateInstance(tree.GetType());

            IEnumerable<PropertyInfo> properties = tree.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite);

            foreach (PropertyInfo property in properties)
                property.SetValue(instance, property.GetValue(tree));

            return (instance as IDeviceTreeBase)!;
        }

        /// <summary>
        /// Returns this node followed by every descendant, depth-first.
        /// </summary>
        public IEnumerable<IDeviceTreeBase> GetNodeAndDescendants()
            => new[] { tree }.Concat(tree.Children.SelectMany(child => child.GetNodeAndDescendants()));

        /// <summary>
        /// Returns the first three '/'-separated segments of this node's <see cref="IDeviceTreeBase.Id"/>, or an
        /// empty string if it has fewer than three segments.
        /// </summary>
        public string GetRoot()
        {
            string[] elements = tree.Id.Split('/', StringSplitOptions.RemoveEmptyEntries);
            return elements.Length >= 3
                ? $"{elements[0]}/{elements[1]}/{elements[2]}"
                : string.Empty;
        }

        /// <summary>
        /// Sets <see cref="IDeviceTreeBase.Status"/> on this node and recursively on every descendant.
        /// </summary>
        public void SetStatus(ConnectionStatus status)
        {
            tree.Status = status;
            foreach (IDeviceTreeBase child in tree.Children)
                child.SetStatus(status);
        }
    }
}
