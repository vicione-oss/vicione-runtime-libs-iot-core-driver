using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using IFM.SDK.Scanner;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.Vse.Data;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.Vse;
using ViciOne.Driver.Vse.Interfaces;

namespace VseSample;

internal sealed partial class Program
{
    private const string VseAddress = "192.168.178.102:3321";
    private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

    public static async Task Main()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));
        ILogger logger = loggerFactory.CreateLogger(nameof(VseDevice));

        await ScanForVseDevicesAsync();

        var stopwatch = Stopwatch.StartNew();
        using VseDevice device = AddVseDevice(VseAddress, logger);

        Console.WriteLine("Waiting for the VSE device to connect and report its device tree...");
        DeviceTreeVseDevice tree = await VseParser.ParseDeviceAsync(device, logger);
        Console.WriteLine($"Tree ready after {stopwatch.ElapsedMilliseconds} ms.");
        Console.WriteLine(JsonSerializer.Serialize(tree, s_jsonOptions));

        PushLatencyMonitor monitor = new(loggerFactory.CreateLogger(nameof(PushLatencyMonitor)));
        int registered = RegisterAllLiveAddresses(device, tree, monitor);
        Console.WriteLine($"Registered {registered} live address(es) - watch the values below arrive as changes happen, not once a second like the old polling loop did.");

        device.RawDataReceived += measurement => Console.WriteLine(
            $"Raw data received: [{measurement.Identifier}] Start={measurement.Start:O} Duration={measurement.Duration}s Frequency={measurement.Frequency}Hz RawBytes={measurement.RawData.Length}");

        Console.WriteLine("Press Enter to trigger a 10s/100kHz raw data recording on sensors 1 and 2, or Ctrl+C to exit.");
        while (true)
        {
            Console.ReadLine();
            device.GetRecordingRawData("Sensor1", 1, 100000, 10000, true);
            device.GetRecordingRawData("Sensor2", 2, 100000, 10000, true);
        }
    }

    /// <summary>
    /// Scans the local LAN for VSE devices (same API the VseFinder FunctionBlock uses) and prints what it finds -
    /// purely informational, doesn't affect which device <see cref="VseAddress"/> connects to below.
    /// </summary>
    private static async Task ScanForVseDevicesAsync()
    {
        Console.WriteLine("Scanning the local LAN for VSE devices...");
        IDeviceScanner scanner = new DeviceScannerBuilder().VSEDeviceScanner().WithLocalLan().Build();
        List<IFM.SDK.VSE.Scanner.DeviceInfo> found = await scanner.ScanAsync(new Progress<int>());

        Console.WriteLine($"Found {found.Count} device(s):");
        foreach (IFM.SDK.VSE.Scanner.DeviceInfo device in found)
            Console.WriteLine($"  {device.IPAddress}:{device.Port} {device.DeviceType} FW={device.FirmwareVersion} HW={device.Hardware} MAC={device.MACAddress} Host={device.HostName}");
    }

    private static VseDevice AddVseDevice(string remoteAddress, ILogger logger)
    {
        VseDevice device = new(remoteAddress, logger);
        device.ParametersChanged += () => Log.ParametersChanged(logger);
        device.SystemModeChanged += () => Log.SystemModeChanged(logger, DescribeSystemMode(device));
        device.TimeoutChanged += () => Log.TimeoutChanged(logger, device.Timeout);
        return device;
    }

    private static string DescribeSystemMode(VseDevice device)
        => device.Supervising ? "Supervise" : device.Measuring ? "Measure" : "Other";

    /// <summary>
    /// Walks the parsed device tree and registers <paramref name="subscriber"/> under every node's own live
    /// address (<see cref="IDeviceTreeVseDataParent.Path"/> - the same address a real FunctionBlock's NodePath
    /// setting would be configured with). RawData nodes don't implement <see cref="IDeviceTreeVseDataParent"/> -
    /// they aren't pushed through IVseSubscriberFunctionBlock at all - so they're naturally excluded here.
    /// </summary>
    private static int RegisterAllLiveAddresses(VseDevice device, IDeviceTreeBase node, IVseSubscriberFunctionBlock subscriber)
    {
        int count = 0;
        if (node is IDeviceTreeVseDataParent dataParent)
        {
            device.RegisterAddress(dataParent.Path, subscriber);
            count++;
        }

        foreach (IDeviceTreeBase child in node.Children)
            count += RegisterAllLiveAddresses(device, child, subscriber);

        return count;
    }

    /// <summary>
    /// Logs how long it's been since each item last pushed a change, plus its current values - demonstrates that
    /// updates arrive as the VSE-SDK's monitoring channels push them, rather than on a fixed polling cadence.
    /// </summary>
    private sealed partial class PushLatencyMonitor(ILogger logger) : IVseSubscriberFunctionBlock
    {
        private readonly Dictionary<object, Stopwatch> _sinceLastPush = [];

        void IVseSubscriberFunctionBlock.SetValue(object item)
        {
            string values = Describe(item);
            if (_sinceLastPush.TryGetValue(item, out Stopwatch? stopwatch))
            {
                Log.PushReceived(logger, values, stopwatch.ElapsedMilliseconds);
                stopwatch.Restart();
            }
            else
            {
                Log.FirstPushReceived(logger, values);
                _sinceLastPush[item] = Stopwatch.StartNew();
            }
        }

        private static string Describe(object item) => item switch
        {
            DeviceObject o => $"{o.Name}: Average={o.Average} Maximum={o.Maximum} Minimum={o.Minimum} RotSpeed={o.RotSpeed} RefValue={o.RefValue} State={o.State} Flags={o.Flags}",
            Counter c => $"{c.Name}: Value={c.Value} State={c.State}",
            Input i => $"{i.Name}: Value={i.Value}",
            DigitalAlarm a => $"{a.Name}: Value={a.Value}",
            Variants v => $"ActiveVariantId={v.ActiveVariantId}",
            _ => item.ToString() ?? item.GetType().Name,
        };

        private static partial class Log
        {
            [LoggerMessage(Level = LogLevel.Information, Message = "First push: {Values}")]
            public static partial void FirstPushReceived(ILogger logger, string values);
            [LoggerMessage(Level = LogLevel.Information, Message = "Push ({ElapsedMs} ms since last): {Values}")]
            public static partial void PushReceived(ILogger logger, string values, long elapsedMs);
        }
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Parameters changed.")]
        public static partial void ParametersChanged(ILogger logger);
        [LoggerMessage(Level = LogLevel.Information, Message = "System mode changed to {SystemMode}.")]
        public static partial void SystemModeChanged(ILogger logger, string systemMode);
        [LoggerMessage(Level = LogLevel.Warning, Message = "Timeout changed to {Timeout}.")]
        public static partial void TimeoutChanged(ILogger logger, bool timeout);
    }
}
