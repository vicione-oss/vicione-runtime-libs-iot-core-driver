using ifmIoTCore.Common.Variant;

namespace ViciOne.Driver.IoTCore.Contracts;

public interface IVseSubscriberFunctionBlock
{
    void SetValue(VariantObject variant);
}
