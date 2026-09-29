namespace ViciOne.DeviceTree.Contracts;

/// <summary>
/// A device tree node that carries an alias in addition to its own <see cref="IDeviceTreeBase.Name"/>.
/// </summary>
/// <remarks>
/// Who owns the alias decides how it may be used, so nodes implement either
/// <see cref="IDeviceTreeUserAliasNode"/> or <see cref="IDeviceTreeDeviceAliasNode"/> rather than this
/// interface directly. Match on this one only to ask whether a node carries an alias at all.
/// </remarks>
public interface IDeviceTreeAliasNode : IDeviceTreeBase
{
    /// <summary>
    /// The alias for this node, if set.
    /// </summary>
    string? Alias { get; set; }
}
