using System;
using ifmIoTCore.Common.Variant;
using Microsoft.Extensions.Logging;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Contracts;

namespace VseSample;

internal sealed partial class Program
{
    private const string VseAddress = "10.45.24.106:3321";

    public static void Main()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));

        VseDevice device = AddVseDevice(VseAddress, loggerFactory);
        RegisterVseAddresses(device, loggerFactory);

        while (true)
        {
            Console.ReadLine();

            device.GetRecordingRawData("Test", 1, 100000, 10000, true);
        }
    }

    private static VseDevice AddVseDevice(string remoteAddress, ILoggerFactory loggerFactory)
    {
        ILogger logger = loggerFactory.CreateLogger(nameof(VseDevice));
        VseDevice device = DeviceManager.GetOrAddVseDevice(remoteAddress, Guid.NewGuid(), logger);
        device.ParametersChanged += () => Log.ParametersChanged(logger);
        device.SystemModeChanged += () => Log.SystemModeChanged(logger);
        device.TimeoutChanged += () => Log.TimeoutChanged(logger, device.Timeout);
        return device;
    }

    private static DummySubscriberFunctionBlock RegisterVseAddresses(VseDevice device, ILoggerFactory loggerFactory)
    {
        ILogger blockLogger = loggerFactory.CreateLogger(nameof(DummySubscriberFunctionBlock));
        DummySubscriberFunctionBlock block = new(blockLogger);

        device.RegisterAddress("/Device/Variants/ActiveVariant", block);
        device.RegisterAddress("/Device/Alarms/Digital/Alarm1", block);
        device.RegisterAddress("/Device/Alarms/Digital/Alarm2", block);
        device.RegisterAddress("/Device/Alarms/Digital/Alarm3", block);
        device.RegisterAddress("/Device/Alarms/Digital/Alarm4", block);
        device.RegisterAddress("/Device/Alarms/Digital/Alarm5", block);
        device.RegisterAddress("/Device/Alarms/Digital/Alarm6", block);
        device.RegisterAddress("/Device/Alarms/Digital/Alarm7", block);
        device.RegisterAddress("/Device/Alarms/Digital/Alarm8", block);
        device.RegisterAddress("/Device/Counters/Counter1", block);
        device.RegisterAddress("/Device/Counters/Counter10", block);
        device.RegisterAddress("/Device/Counters/Counter11", block);
        device.RegisterAddress("/Device/Counters/Counter12", block);
        device.RegisterAddress("/Device/Counters/Counter13", block);
        device.RegisterAddress("/Device/Counters/Counter14", block);
        device.RegisterAddress("/Device/Counters/Counter15", block);
        device.RegisterAddress("/Device/Counters/Counter16", block);
        device.RegisterAddress("/Device/Counters/Counter17", block);
        device.RegisterAddress("/Device/Counters/Counter20", block);
        device.RegisterAddress("/Device/Counters/Counter21", block);
        device.RegisterAddress("/Device/Counters/Counter22", block);
        device.RegisterAddress("/Device/Counters/Counter23", block);
        device.RegisterAddress("/Device/Counters/Counter24", block);
        device.RegisterAddress("/Device/Counters/Counter25", block);
        device.RegisterAddress("/Device/Counters/Counter26", block);
        device.RegisterAddress("/Device/Counters/Counter27", block);
        device.RegisterAddress("/Device/Inputs/External/Input1", block);
        device.RegisterAddress("/Device/Inputs/External/Input2", block);
        device.RegisterAddress("/Device/Objects/Object1", block);
        device.RegisterAddress("/Device/Objects/Object2", block);
        device.RegisterAddress("/Device/Objects/Object3", block);
        device.RegisterAddress("/Device/Objects/Object4", block);
        device.RegisterAddress("/Device/Objects/Object5", block);
        device.RegisterAddress("/Device/Objects/Object6", block);
        device.RegisterAddress("/Device/Objects/Object7", block);
        device.RegisterAddress("/Device/Objects/Object8", block);
        device.RegisterAddress("/Device/Objects/Object9", block);
        device.RegisterAddress("/Device/Objects/Object10", block);
        device.RegisterAddress("/Device/Objects/Object11", block);
        device.RegisterAddress("/Device/Objects/Object12", block);
        device.RegisterAddress("/Device/Objects/Object13", block);
        device.RegisterAddress("/Device/Objects/Object14", block);
        device.RegisterAddress("/Device/Objects/Object15", block);
        device.RegisterAddress("/Device/Objects/Object16", block);
        device.RegisterAddress("/Device/Objects/Object17", block);
        device.RegisterAddress("/Device/Objects/Object18", block);
        device.RegisterAddress("/Device/Objects/Object19", block);
        device.RegisterAddress("/Device/Objects/Object20", block);

        return block;
    }

    private sealed partial class DummySubscriberFunctionBlock(ILogger logger) : IVseSubscriberFunctionBlock
    {
        void IVseSubscriberFunctionBlock.SetValue(VariantObject variant) => Log.VariantObjectReceived(logger, variant);

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "VariantObject received: {Variant}")]
            public static partial void VariantObjectReceived(ILogger logger, VariantObject variant);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Parameters changed.")]
        public static partial void ParametersChanged(ILogger logger);
        [LoggerMessage(Level = LogLevel.Information, Message = "System mode changed.")]
        public static partial void SystemModeChanged(ILogger logger);
        [LoggerMessage(Level = LogLevel.Warning, Message = "Timeout changed to {Timeout}.")]
        public static partial void TimeoutChanged(ILogger logger, bool timeout);
    }
}
