using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Driver.Vse.Interfaces;

/// <summary>
/// The configuration settings shared by every Vse process-data subscriber FunctionBlock
/// (<c>SubscriberBase&lt;TBlock, TItem&gt;</c>'s <c>Settings</c>).
/// </summary>
public interface IVseSubscriberSettings
{
    /// <summary>
    /// The Vse device's address.
    /// </summary>
    IRuntimeSetting<string> Address { get; }

    /// <summary>
    /// The node path in the Vse device's data model to subscribe to.
    /// </summary>
    IRuntimeSetting<string> NodePath { get; }
}
