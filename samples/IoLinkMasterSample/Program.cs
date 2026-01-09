using Microsoft.Extensions.Logging;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace IoLinkMasterSample;

internal sealed partial class Program
{
    private static readonly string s_ioddDirectory = Path.GetFullPath(@"..\..\..\..\..\artifacts\IODDs");

    private const string MasterAddress = "10.45.24.141";
    private const ushort VendorId = 310;
    private const uint DeviceId = 582;
    private const int PortIndex = 2;
    private const int ProcessDataInIndex = 1;

    internal static void Main()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));

        using IoLinkMaster device = AddIoLinkMaster(MasterAddress, s_ioddDirectory, loggerFactory);
        RegisterProcessDataIn(device, VendorId, DeviceId, PortIndex, ProcessDataInIndex, loggerFactory);

        Console.ReadLine();
    }

    private static IoLinkMaster AddIoLinkMaster(string address, string ioddDirectory, ILoggerFactory loggerFactory)
    {
        ILogger ioLinkParserLogger = loggerFactory.CreateLogger(nameof(IoLinkParser));
        IoLinkParser.Initialize(ioddDirectory, true, ioLinkParserLogger);

        ILogger logger = loggerFactory.CreateLogger(nameof(IoLinkMaster));
        IoLinkMaster device = DeviceManager.GetOrAddIoLinkMaster(address, Guid.NewGuid(), logger);
        device.ChildDevicesChanged += () => Log.ChildDevicesChanged(logger);
        device.TimeoutChanged += () => Log.TimeoutChanged(logger, device.Timeout);
        return device;
    }

    private static DummySubscriberFunctionBlock RegisterProcessDataIn(IoLinkMaster device, ushort vendorId, uint deviceId, int portIndex, int processDataInIndex, ILoggerFactory loggerFactory)
    {
        ILogger blockLogger = loggerFactory.CreateLogger(nameof(DummySubscriberFunctionBlock));
        DummySubscriberFunctionBlock block = new(blockLogger);

        device.RegisterProcessDataIn(portIndex, vendorId, deviceId, processDataInIndex, block);

        return block;
    }

    private sealed partial class DummySubscriberFunctionBlock(ILogger logger) : IIoLinkSubscriberFunctionBlock
    {
        void IIoLinkSubscriberFunctionBlock.SetValue(object value, string displayFormat) => Log.ValueReceived(logger, value);
        void IIoLinkSubscriberFunctionBlock.SetUnit(string unit) => Log.UnitReceived(logger, unit);

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "Value received: {Value}")]
            public static partial void ValueReceived(ILogger logger, object value);
            [LoggerMessage(Level = LogLevel.Information, Message = "Unit received: {Unit}")]
            public static partial void UnitReceived(ILogger logger, string unit);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Child devices changed.")]
        public static partial void ChildDevicesChanged(ILogger logger);
        [LoggerMessage(Level = LogLevel.Warning, Message = "Timeout changed to {Timeout}.")]
        public static partial void TimeoutChanged(ILogger logger, bool timeout);
    }
}
