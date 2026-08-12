using Microsoft.Extensions.Logging;
using ViciOne.Communication.IoTCore.Extensions;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.IoTCore;
using ViciOne.Driver.IoTCore.Interfaces;

namespace IoLinkMasterSample;

internal sealed partial class Program
{
    private static readonly string s_ioddDirectory = Path.GetFullPath(@"..\..\..\..\..\artifacts\IODDs");

    private const string MasterAddress = "192.168.178.100";
    private const ushort VendorId = 310;
    private const uint DeviceId = 569;
    private const int PortIndex = 2;
    private const int ProcessDataInIndex = 1;

    internal static void Main()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));

        using IoLinkMaster device = AddIoLinkMaster(MasterAddress, s_ioddDirectory, loggerFactory);
        RegisterProcessDataIn(device, VendorId, DeviceId, PortIndex, ProcessDataInIndex, loggerFactory);

        ILogger diagnosticsLogger = loggerFactory.CreateLogger("PortDiagnostics");
        while (device.Initialized == false)
            Thread.Sleep(200);
        DiagnosePorts(device, diagnosticsLogger).GetAwaiter().GetResult();

        Console.WriteLine();
        Console.WriteLine(">>> Unplug/replug the sensor on port 2 now - staying alive for 120s to observe. <<<");
        Console.WriteLine();
        Thread.Sleep(TimeSpan.FromSeconds(120));
    }

    /// <summary>
    /// Reads every port's mode directly, and - only where mode reports IO-Link (3) - also attempts a vendorid
    /// read, to distinguish "port has no device and correctly reports a non-IO-Link mode" from "port reports
    /// IO-Link mode but doesn't actually answer" (the latter is what throws in PollingAsync).
    /// </summary>
    private static async Task DiagnosePorts(IoLinkMaster device, ILogger logger)
    {
        DeviceTreeIoLinkMaster tree = await IoLinkParser.ParseDeviceAsync(device, logger);
        Console.WriteLine();
        Console.WriteLine("===== PORT DIAGNOSTICS =====");
        foreach (IDeviceTreeBase node in tree.Children)
        {
            if (node is not DeviceTreeIoLinkMasterPort port)
                continue;

            byte? mode = null;
            try { mode = device.Core.GetDataValueAsValue($"{port.Id}/mode")?.GetUInt8(); }
            catch (Exception ex) { Console.WriteLine($"{port.Name}: mode read FAILED: {ex.Message}"); }

            string vendorIdResult;
            if (mode == 3)
            {
                try
                {
                    ushort? vendorId = device.Core.GetDataValueAsValue($"{port.Id}/iolinkdevice/vendorid")?.GetUInt16();
                    vendorIdResult = $"vendorid={vendorId}";
                }
                catch (Exception ex)
                {
                    vendorIdResult = $"vendorid FAILED: {ex.Message}";
                }
            }
            else
            {
                vendorIdResult = "(skipped, mode != 3)";
            }

            Console.WriteLine($"{port.Name}: mode={mode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "(failed)"}, {vendorIdResult}");
        }
        Console.WriteLine("===== END PORT DIAGNOSTICS =====");
        Console.WriteLine();
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
