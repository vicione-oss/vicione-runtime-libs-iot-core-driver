using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using ViciOne.Driver.IoTCore.Contracts.Dcp;

namespace ViciOne.Driver.IoTCore.DcpScan;

internal sealed class DcpScanner(DcpScannerOptions? options = null, ILogger? logger = null)
{
    private static readonly SemaphoreSlim s_scanGate = new(1, 1);
    private static readonly TimeSpan s_noPacketDelay = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan s_errorDelay = TimeSpan.FromMilliseconds(5);

    private static NetworkInterface[] s_allNics = [];

    private readonly DcpScannerOptions _opt = NormalizeOptions(options ?? new DcpScannerOptions());
    private readonly ILogger _logger = logger ?? new NullLogger<DcpScanner>();

    private static DcpScannerOptions NormalizeOptions(DcpScannerOptions src)
        => new()
        {
            IncludeLoopbacks = src.IncludeLoopbacks,
            IncludeVlanFrame = src.IncludeVlanFrame,
            NarrowFilter = src.NarrowFilter,
            SendRetries = src.SendRetries < 1 ? 1 : src.SendRetries,
            AttemptWindow = src.AttemptWindow <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(400) : src.AttemptWindow,
            StragglerWindow = src.StragglerWindow <= TimeSpan.Zero ? TimeSpan.FromMilliseconds(1200) : src.StragglerWindow,
            MaxDegreeOfParallelism = src.MaxDegreeOfParallelism <= 0 ? Environment.ProcessorCount : src.MaxDegreeOfParallelism
        };

    public async Task<List<DcpDevice>> Scan(CancellationToken cancellationToken = default)
    {
        await s_scanGate.WaitAsync(cancellationToken);
        try
        {
            return await ScanInternal(cancellationToken);
        }
        finally
        {
            s_scanGate.Release();
        }
    }

    private async Task<List<DcpDevice>> ScanInternal(CancellationToken cancellationToken = default)
    {
        RefreshNics();
        await SafeNativeCall(CaptureDeviceList.Instance.Refresh);

        List<(int index, ILiveDevice dev)> adapters = EnumerateEthernetAdapters(CaptureDeviceList.Instance, _opt.IncludeLoopbacks);

        if (adapters.Count == 0)
        {
            _logger.LogInformation("No suitable live devices found.");
            return [];
        }

        _logger.LogInformation($"Adapters found: {adapters.Count}");

        var results = new ConcurrentBag<DcpDevice>();
        var po = new ParallelOptions
        {
            MaxDegreeOfParallelism = _opt.MaxDegreeOfParallelism,
            CancellationToken = cancellationToken,
        };

        try
        {
            await Parallel.ForEachAsync(adapters, po, async (entry, token) =>
            {
                (int adapterId, ILiveDevice dev) = entry;
                await ScanAdapterAsync(adapterId, dev, results, token);
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Scan canceled");
        }

        return [.. results
            .GroupBy(r => r.MacAddress ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())];
    }

    private static List<(int index, ILiveDevice dev)> EnumerateEthernetAdapters(CaptureDeviceList liveDevices, bool includeLoopbacks)
    {
        List<(int index, ILiveDevice dev)> ethernetAdapters = [];

        foreach (NetworkInterface network in s_allNics)
        {
            if (network.NetworkInterfaceType != NetworkInterfaceType.Ethernet || network.OperationalStatus != OperationalStatus.Up)
                continue;

            foreach (UnicastIPAddressInformation unicastAddress in network.GetIPProperties().UnicastAddresses)
            {
                ILiveDevice? adapter = liveDevices.FirstOrDefault(d => d.Name == network.Name || d.Description == network.Description);

                if (adapter is null)
                    continue;

                if (!includeLoopbacks && IsLoopback(adapter))
                    continue;

                if (unicastAddress.Address is not null && unicastAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                    ethernetAdapters.Add((liveDevices.IndexOf(adapter), adapter));
            }
        }

        return ethernetAdapters;
    }

    private async Task ScanAdapterAsync(int adapterId, ILiveDevice dev, ConcurrentBag<DcpDevice> sink, CancellationToken ct)
    {
        string displayName = DisplayName(dev);
        string tag = $"{adapterId}:{displayName}";
        bool debugEnabled = _logger.IsEnabled(LogLevel.Debug);

        void LogInfo(string m) => _logger.LogInformation($"[{tag}] {m}");
        void LogDebug(string m)
        {
            if (debugEnabled) _logger.LogDebug($"[{tag}] {m}");
        }
        void LogDebugWithError(string m, Exception? ex)
        {
            if (debugEnabled) _logger.LogDebug(ex, $"[{tag}] {m}");
        }
        void LogError(string m, Exception ex) => _logger.LogError(ex, $"[{tag}] {m}");

        (IPAddress? localIp, IPAddress? _) = GetIPv4AndMask(dev);
        PhysicalAddress? localMac = GetAdapterMac(dev);
        LogInfo($"Start scan, MAC={localMac}, IP={localIp}");

        if (localMac is null)
        {
            LogDebug("Skipped (no MAC)");
            return;
        }

        List<DcpDevice> perAdapterFound = [];
        byte[] xidBytes = RandomNumberGenerator.GetBytes(4);
        uint xidBe = BinaryPrimitives.ReadUInt32BigEndian(xidBytes);
        bool narrowFilterActive = false;

        try
        {
            await OpenBestEffort(dev, LogDebug);
            LogDebug($"Opened, LinkType={dev.LinkType}");

            if (dev.LinkType != LinkLayers.Ethernet)
            {
                LogDebug("Not an Ethernet link type. Skipping.");
                try { await SafeNativeCall(dev.Close); } catch { }
                return;
            }

            narrowFilterActive = await SetFilterDefensive(dev, xidBe, LogDebug);

            for (int attempt = 1; attempt <= _opt.SendRetries; attempt++)
            {
                if (ct.IsCancellationRequested)
                    break;

                await SendIdentify(dev, localMac, xidBe, _opt.IncludeVlanFrame, LogInfo);
                await CaptureLoop(dev, _opt.AttemptWindow, LogDebugWithError, cap =>
                {
                    DcpPacketParser.TryHandlePacket(cap, xidBytes, adapterId, displayName, perAdapterFound, LogInfo,
                        msg => ConditionalPacketDebug(narrowFilterActive, msg, LogDebug), narrowFilterActive);
                    return false;
                }, ct);
            }

            LogDebug("Collecting stragglers…");
            await CaptureLoop(dev, _opt.StragglerWindow, LogDebugWithError, cap =>
            {
                DcpPacketParser.TryHandlePacket(cap, xidBytes, adapterId, displayName, perAdapterFound, LogInfo,
                    msg => ConditionalPacketDebug(narrowFilterActive, msg, LogDebug), narrowFilterActive);
                return false;
            }, ct);
        }
        catch (OperationCanceledException)
        {
            LogDebug("Canceled.");
        }
        catch (Exception ex)
        {
            LogError($"Scan failed: {ex.Message}", ex);
        }
        finally
        {
            try { await SafeNativeCall(dev.Close); } catch { }
        }

        foreach (DcpDevice it in perAdapterFound)
            sink.Add(it);

        LogInfo($"Devices reported: {perAdapterFound.Count}");
    }

    private async Task<bool> SetFilterDefensive(ILiveDevice dev, uint xidBe, Action<string> logDebug)
    {
        bool narrowFilterActive;
        try
        {
            (string filter, bool isNarrow) = DcpFilterBuilder.Build(_opt.IncludeVlanFrame, _opt.NarrowFilter, xidBe);
            await SafeNativeCall(() => dev.Filter = filter);
            narrowFilterActive = isNarrow;

            logDebug(isNarrow ? "BPF narrow IdentifyRes+XID set" : (_opt.IncludeVlanFrame ? "BPF set (broad with VLAN)" : "BPF set (broad)"));
        }
        catch (Exception ex)
        {
            logDebug($"BPF set failed ({ex.Message}); falling back to broad filter");
            try
            {
                (string filter, bool _) = DcpFilterBuilder.Build(_opt.IncludeVlanFrame, false, xidBe);
                await SafeNativeCall(() => dev.Filter = filter);
                logDebug(_opt.IncludeVlanFrame ? "BPF broad fallback set" : "BPF broad fallback set");
            }
            catch (Exception ex2)
            {
                logDebug($"BPF disable failed: {ex2.Message}");
            }
            narrowFilterActive = false;
        }

        return narrowFilterActive;
    }

    private static void ConditionalPacketDebug(bool narrowFilterActive, string msg, Action<string> logDebug)
    {
        if (narrowFilterActive || !IsNonDcpNoise(msg))
            logDebug(msg);
    }

    private static async Task OpenBestEffort(ILiveDevice dev, Action<string> debug)
    {
        try
        {
            if (dev is LibPcapLiveDevice)
            {
                await SafeNativeCall(() => dev.Open(new DeviceConfiguration
                {
                    Mode = DeviceModes.NoCaptureLocal | DeviceModes.MaxResponsiveness,
                    LinkLayerType = LinkLayers.Ethernet,
                    Immediate = true,
                    ReadTimeout = 100
                }));
                debug("open(libpcap) OK");
                return;
            }
        }
        catch (Exception ex)
        {
            debug($"open(libpcap) failed: {ex.Message}, falling back");
        }

        await SafeNativeCall(() => dev.Open(new DeviceConfiguration
        {
            Mode = DeviceModes.None,
            LinkLayerType = LinkLayers.Ethernet,
            Immediate = true,
            ReadTimeout = 100
        }));
        debug("open(generic) OK");
    }

    private static async Task SendIdentify(ILiveDevice dev, PhysicalAddress srcMac, uint xidBe, bool vlanFrame, Action<string> logInfo)
    {
        byte[] buffer = DcpFrameBuilder.BuildIdentifyFrame(srcMac, xidBe, vlanFrame);
        await SafeNativeCall(() => dev.SendPacket(buffer));
        logInfo($"Sent DCP Identify (len={buffer.Length}, xid=0x{xidBe:X8})");
    }

    private static async Task CaptureLoop(ILiveDevice dev, TimeSpan duration, Action<string, Exception?> logDebug, Func<RawCapture, bool> onPacket, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < duration && !ct.IsCancellationRequested)
        {
            try
            {
                if (dev.GetNextPacket(out PacketCapture cap) == GetPacketStatus.PacketRead)
                {
                    RawCapture pkt = cap.GetPacket();
                    sw.Restart();
                    if (onPacket(pkt)) return;
                }
                else
                {
                    await Task.Delay(s_noPacketDelay, ct);
                }
            }
            catch (ObjectDisposedException ex)
            {
                logDebug("device disposed during capture", ex);
                return;
            }
            catch (Exception ex)
            {
                logDebug($"capture error: {ex.Message}", ex);
                await Task.Delay(s_errorDelay, ct);
            }
        }
    }

    private static bool IsNonDcpNoise(string msg)
        => msg.StartsWith("Ignore FrameId", StringComparison.Ordinal) ||
           msg.StartsWith("ServiceId/Type mismatch", StringComparison.Ordinal) ||
           msg.StartsWith("XID mismatch", StringComparison.Ordinal) ||
           msg.StartsWith("Not PN-DCP", StringComparison.Ordinal);

    private static bool IsLoopback(ILiveDevice d)
    {
        try
        {
            if (d is LibPcapLiveDevice lcd && lcd.Interface is not null)
            {
                if (string.Equals(lcd.Interface.Name, "lo", StringComparison.OrdinalIgnoreCase)) return true;
                string f = lcd.Interface.FriendlyName ?? string.Empty;
                string desc = lcd.Interface.Description ?? string.Empty;
                if (f.Contains("loopback", StringComparison.OrdinalIgnoreCase) || desc.Contains("loopback", StringComparison.OrdinalIgnoreCase)) return true;
            }
        }
        catch { }
        string name = d.Name ?? string.Empty;
        string descr = d.Description ?? string.Empty;
        return name.Equals("lo", StringComparison.OrdinalIgnoreCase) || name.Contains("loopback", StringComparison.OrdinalIgnoreCase) || descr.Contains("loopback", StringComparison.OrdinalIgnoreCase);
    }

    private static NetworkInterface? GetNicForDevice(ILiveDevice dev)
    {
        string desc = dev.Description ?? string.Empty;
        string name = dev.Name ?? string.Empty;
        try
        {
            return s_allNics.FirstOrDefault(n => n.Description == desc || n.Name == name);
        }
        catch { return null; }
    }

    private static string DisplayName(ILiveDevice dev)
    {
        try
        {
            if (dev is LibPcapLiveDevice lcd && lcd.Interface != null)
            {
                if (!string.IsNullOrWhiteSpace(lcd.Interface.FriendlyName)) return lcd.Interface.FriendlyName;
                if (!string.IsNullOrWhiteSpace(lcd.Interface.Description)) return lcd.Interface.Description;
                if (!string.IsNullOrWhiteSpace(lcd.Interface.Name)) return lcd.Interface.Name;
            }
        }
        catch { }
        if (!string.IsNullOrWhiteSpace(dev.Description)) return dev.Description;
        if (!string.IsNullOrWhiteSpace(dev.Name)) return dev.Name;
        NetworkInterface? ni = GetNicForDevice(dev);
        if (ni != null) return string.IsNullOrWhiteSpace(ni.Name) ? ni.Description : ni.Name;
        return "(unnamed)";
    }

    private static (IPAddress? ip, IPAddress? mask) GetIPv4AndMask(ILiveDevice dev)
    {
        try
        {
            NetworkInterface? ni = GetNicForDevice(dev);
            if (ni == null) return (null, null);
            UnicastIPAddressInformation? uni = ni.GetIPProperties().UnicastAddresses.FirstOrDefault(u => u.Address.AddressFamily == AddressFamily.InterNetwork);
            if (uni == null) return (null, null);
            return (uni.Address, uni.IPv4Mask);
        }
        catch { return (null, null); }
    }

    private static PhysicalAddress? GetAdapterMac(ILiveDevice dev)
    {
        NetworkInterface? ni = GetNicForDevice(dev);
        if (ni == null) return null;
        try
        {
            PhysicalAddress mac = ni.GetPhysicalAddress();
            if (mac != null && mac.GetAddressBytes().Length == 6) return mac;
        }
        catch { }
        return null;
    }

    private static NetworkInterface[] RefreshNics()
    {
        try
        {
            s_allNics = NetworkInterface.GetAllNetworkInterfaces();
        }
        catch
        {
            s_allNics = [];
        }

        return s_allNics;
    }

    private static async Task<T> SafeNativeCall<T>(Func<T> func)
    {
        T result = func();

        await Task.Delay(100, CancellationToken.None);

        return result;
    }

    private static async Task SafeNativeCall(Action action)
    {
        action();
        await Task.Delay(100, CancellationToken.None);
    }
}
