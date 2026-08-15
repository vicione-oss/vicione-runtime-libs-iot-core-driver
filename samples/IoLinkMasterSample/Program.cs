using System.Diagnostics;
using System.Globalization;
using System.Text;
using ifm.Common.Variant;
using ifm.IoTCore.ElementManager.Contracts.Elements;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.IoTCore.Extensions;
using ViciOne.DeviceTree.Contracts;
using ViciOne.Driver.IoTCore;

namespace IoLinkMasterSample;

internal sealed partial class Program
{
    private static readonly string s_ioddDirectory = Path.GetFullPath(@"..\..\..\..\..\artifacts\IODDs");

    private const string MasterAddress = "192.168.178.100";

    internal static void Main()
    {
        using ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddSimpleConsole(options => options.SingleLine = true));

        using IoLinkMaster device = AddIoLinkMaster(MasterAddress, s_ioddDirectory, loggerFactory);

        ILogger diagnosticsLogger = loggerFactory.CreateLogger("PortDiagnostics");
        while (device.Initialized == false)
            Thread.Sleep(200);
        DiagnosePorts(device, diagnosticsLogger).GetAwaiter().GetResult();

        MeasureGetDataMulti(device, diagnosticsLogger).GetAwaiter().GetResult();

        MonitorLive(device, diagnosticsLogger, 60).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Repeatedly reads every port's device status and process-data-in via a single "getdatamulti" request and
    /// prints one line per snapshot, so unplugging and replugging sensors is visible live: a port with a device
    /// shows its raw process data, an empty (or unplugged) one shows "----". One absent port never disturbs the
    /// shared request or the others.
    /// </summary>
    private static async Task MonitorLive(IoLinkMaster device, ILogger logger, int seconds)
    {
        DeviceTreeIoLinkMaster tree = await IoLinkParser.ParseDeviceAsync(device, logger);
        IBaseElement remote = device.Core.Root.Subs!.Single(x => x.Identifier == "remote");
        string deviceRoot = remote.Subs!.Single().Address;

        List<(string Name, string PdInAddress)> ports = [];
        List<string> addressesToSend = [];
        foreach (IDeviceTreeBase node in tree.Children)
        {
            if (node is not DeviceTreeIoLinkMasterPort port)
                continue;
            string pdInAddress = $"{port.Id[deviceRoot.Length..]}/iolinkdevice/pdin";
            ports.Add((port.Name.Replace("Port ", "P", StringComparison.Ordinal), pdInAddress));
            addressesToSend.Add(pdInAddress);
        }

        Console.WriteLine();
        Console.WriteLine($">>> LIVE getdatamulti monitor for {seconds}s - unplug / replug sensors now <<<");
        Console.WriteLine("    (each line = one getdatamulti snapshot; '----' = no device on that port)");
        Console.WriteLine();

        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed.TotalSeconds < seconds)
        {
            string elapsed = ((int)stopwatch.Elapsed.TotalSeconds).ToString(CultureInfo.InvariantCulture).PadLeft(3);
            try
            {
                Dictionary<string, VariantValue> values = device.Core.GetDataMulti(deviceRoot, addressesToSend);
                StringBuilder line = new();
                line.Append("t+").Append(elapsed).Append("s  ");
                foreach ((string name, string pdInAddress) in ports)
                {
                    string cell = values.TryGetValue(pdInAddress, out VariantValue? pdIn) ? pdIn.ToString() : "----";
                    line.Append(name).Append('=').Append(cell.PadRight(10)).Append(' ');
                }
                Console.WriteLine(line.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"t+{elapsed}s  getdatamulti failed: {ex.Message}");
            }

            Thread.Sleep(1000);
        }
    }

    /// <summary>
    /// Reads every port's cyclic data (mode, device status, vendor/device id and process-data-in) both the way
    /// the per-element polling path does - one request per address - and in a single "getdatamulti" request,
    /// printing the values read and comparing how long each way takes.
    /// </summary>
    private static async Task MeasureGetDataMulti(IoLinkMaster device, ILogger logger)
    {
        DeviceTreeIoLinkMaster tree = await IoLinkParser.ParseDeviceAsync(device, logger);

        // The mirrored master is the sole child under "remote"; its address is the prefix every port address
        // carries and, stripped off, turns a mirrored address into the one a getdatamulti request must send.
        IBaseElement remote = device.Core.Root.Subs!.Single(x => x.Identifier == "remote");
        string deviceRoot = remote.Subs!.Single().Address;

        List<string> mirroredAddresses = [];
        foreach (IDeviceTreeBase node in tree.Children)
        {
            if (node is not DeviceTreeIoLinkMasterPort port)
                continue;
            mirroredAddresses.Add($"{port.Id}/mode");
            mirroredAddresses.Add($"{port.Id}/iolinkdevice/status");
            mirroredAddresses.Add($"{port.Id}/iolinkdevice/vendorid");
            mirroredAddresses.Add($"{port.Id}/iolinkdevice/deviceid");
            mirroredAddresses.Add($"{port.Id}/iolinkdevice/pdin");
        }
        List<string> addressesToSend = mirroredAddresses.Select(address => address[deviceRoot.Length..]).ToList();

        // Warm up both paths so the timings below don't include one-off costs (element resolution, JIT).
        foreach (string address in mirroredAddresses)
            TryRead(device, address);
        _ = device.Core.GetDataMulti(deviceRoot, addressesToSend);

        const int Rounds = 6;
        Stopwatch stopwatch = new();

        List<double> sequential = [];
        for (int round = 0; round < Rounds; round++)
        {
            stopwatch.Restart();
            foreach (string address in mirroredAddresses)
                TryRead(device, address);
            sequential.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        List<double> batched = [];
        Dictionary<string, VariantValue> values = [];
        for (int round = 0; round < Rounds; round++)
        {
            stopwatch.Restart();
            values = device.Core.GetDataMulti(deviceRoot, addressesToSend);
            batched.Add(stopwatch.Elapsed.TotalMilliseconds);
        }

        Console.WriteLine();
        Console.WriteLine("===== GETDATAMULTI MEASUREMENT =====");
        Console.WriteLine($"addresses per cycle: {mirroredAddresses.Count}");
        Console.WriteLine($"per-element (sequential): avg {sequential.Average():F0} ms  (min {sequential.Min():F0}, max {sequential.Max():F0})");
        Console.WriteLine($"one getdatamulti:         avg {batched.Average():F0} ms  (min {batched.Min():F0}, max {batched.Max():F0})");
        Console.WriteLine($"speedup: {sequential.Average() / batched.Average():F1}x   ({mirroredAddresses.Count} requests -> 1)");
        Console.WriteLine();
        Console.WriteLine("values read via getdatamulti:");
        foreach (string address in addressesToSend)
            Console.WriteLine($"  {address} = {(values.TryGetValue(address, out VariantValue? value) ? value.ToString() : "(no value)")}");

        // How many requests per cycle the per-element path actually issues for THIS master, replicating what
        // the driver's polling does: mode for every port, then (only in IO-Link mode) status, then (only with a
        // device present) vendorid + deviceid, plus one process-data-in read per active port. getdatamulti does
        // this in a single request instead. The polling loop ticks once per second (see IoLinkMaster).
        const int CyclesPerMinute = 60000 / 1000;
        int perElementPerCycle = 0;
        int activePorts = 0;
        foreach (IDeviceTreeBase node in tree.Children)
        {
            if (node is not DeviceTreeIoLinkMasterPort port)
                continue;
            string portRoot = port.Id[deviceRoot.Length..];
            perElementPerCycle++; // mode - always read
            if (ReadByte(values, $"{portRoot}/mode") != 3)
                continue;
            perElementPerCycle++; // status - only in IO-Link mode
            if (ReadByte(values, $"{portRoot}/iolinkdevice/status") == 0)
                continue;
            perElementPerCycle += 2; // vendorid + deviceid - only with a device present
            perElementPerCycle++;    // process-data-in - one subscribed value per active port
            activePorts++;
        }

        int perElementPerMinute = perElementPerCycle * CyclesPerMinute;
        int getDataMultiPerMinute = CyclesPerMinute; // exactly one request per cycle
        Console.WriteLine();
        Console.WriteLine($"process values (active IO-Link ports delivering process data): {activePorts}");
        Console.WriteLine("requests per minute for this master (polling once per second):");
        Console.WriteLine($"  per-element:   {perElementPerCycle}/cycle  ->  {perElementPerMinute}/min");
        Console.WriteLine($"  getdatamulti:  1/cycle  ->  {getDataMultiPerMinute}/min");
        Console.WriteLine($"  saved:         {perElementPerMinute - getDataMultiPerMinute}/min");
        Console.WriteLine("===== END =====");
    }

    private static byte? ReadByte(Dictionary<string, VariantValue> values, string address)
        => values.TryGetValue(address, out VariantValue? value) ? value.GetUInt8() : null;

    private static void TryRead(IoLinkMaster device, string mirroredAddress)
    {
        try { _ = device.Core.GetDataValueAsValue(mirroredAddress); }
        catch (Exception) { /* an empty port answers identity/pdin reads with an error - expected here */ }
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
        IoLinkMaster device = DeviceManager.GetOrAddIoLinkMaster(address, address, Guid.NewGuid(), logger, useGetDataMulti: true);
        device.ChildDevicesChanged += () => Log.ChildDevicesChanged(logger);
        device.TimeoutChanged += () => Log.TimeoutChanged(logger, device.Timeout);
        return device;
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Child devices changed.")]
        public static partial void ChildDevicesChanged(ILogger logger);
        [LoggerMessage(Level = LogLevel.Warning, Message = "Timeout changed to {Timeout}.")]
        public static partial void TimeoutChanged(ILogger logger, bool timeout);
    }
}
