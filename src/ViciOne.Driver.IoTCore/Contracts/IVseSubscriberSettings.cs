using ViciOne.Core.Contracts.Runtime;

namespace ViciOne.Driver.IoTCore.Contracts;

public interface IVseSubscriberSettings
{
    IRuntimeSetting<string> Address { get; }
    IRuntimeSetting<string> NodePath { get; }
}
