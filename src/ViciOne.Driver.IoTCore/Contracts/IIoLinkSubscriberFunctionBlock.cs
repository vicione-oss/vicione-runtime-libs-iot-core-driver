namespace ViciOne.Driver.IoTCore.Contracts;

public interface IIoLinkSubscriberFunctionBlock
{
    void SetValue(object value, string displayFormat);
    void SetUnit(string unit);
}
