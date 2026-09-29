namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A human-readable description shown for a device tree node.
/// </summary>
public sealed class DeviceTreeNodeDescription
{
    /// <summary>
    /// The icon to display for this node, if any.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// The description's text.
    /// </summary>
    public required string Text { get; set; }
}
