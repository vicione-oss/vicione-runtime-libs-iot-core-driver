using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ifm.Common.Variant;
using ifm.IoddParser.Contracts;
using ifm.IoddParser.Helpers;
using ifm.IoTCore.Contracts;
using ifm.IoTCore.ElementManager.Contracts.Elements;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.IoTCore.Extensions;
using ViciOne.Communication.IoTCore.IoddStore;
using ViciOne.DeviceTree.Contracts;
using ViciOne.DeviceTree.Contracts.Extensions;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// Builds the device tree for one <see cref="IoLinkMaster"/>: walks its live element tree, reads each
/// connected device's identity and IODD-decoded process data, and reuses cached results for ports whose
/// device hasn't changed since the last full parse.
/// </summary>
public static partial class IoLinkParser
{
    private const string AddressApplicationSpecificTag = "/applicationspecifictag";
    private const string AddressBootLoaderRevision = "/deviceinfo/bootloaderrevision";
    private const string AddressDeviceFamily = "/deviceinfo/devicefamily";
    private const string AddressDeviceId = "/deviceid";
    private const string AddressDeviceVendor = "/deviceinfo/vendor";
    private const string AddressHardwareRevision = "/deviceinfo/hwrevision";
    private const string AddressProductName = "/productname";
    private const string AddressProductCode = "/deviceinfo/productcode";
    private const string AddressSerialNumber = "/deviceinfo/serialnumber";
    private const string AddressSoftwareRevision = "/deviceinfo/swrevision";
    private const string AddressStatus = "/status";
    private const string AddressVendorId = "/vendorid";
    private const string ProfileIoLinkDeviceFull = "iolinkdevice_full";
    private const string IoLinkMasterIdentifier = "iolinkmaster";

    private static DeviceTreeDevice AddIoLinkDeviceNode(IBaseElement ioLinkDevice, IDeviceTreeBase parentNode)
    {
        var node = new DeviceTreeDevice()
        {
            Id = ioLinkDevice.Address,
            Name = IoLinkMaster.DefaultDeviceName,
        };

        parentNode.Children.Add(node);
        return node;
    }

    private static DeviceTreeIoLinkMaster AddIoLinkMasterNode(IBaseElement ioLinkMaster, IDeviceTreeBase parentNode, IoLinkMaster masterDevice)
    {
        string remoteDeviceIdentifier = ioLinkMaster.Parent.Identifier;
        var node = new DeviceTreeIoLinkMaster()
        {
            Id = ioLinkMaster.Address,
            MacAddress = remoteDeviceIdentifier,
            Name = IoLinkMaster.DefaultName,
            Url = masterDevice.Uri,
            Username = masterDevice.Username,
            Password = masterDevice.Password,
            Security = masterDevice.Security,
        };

        parentNode.Children.Add(node);
        return node;
    }

    private static DeviceTreeIoLinkMasterPort AddIoLinkMasterPortNode(IBaseElement ioLinkMasterPort, IDeviceTreeBase parentNode)
    {
        string portNumber = PortNumberRegex().Match(ioLinkMasterPort.Identifier).Value;

        if (int.TryParse(portNumber, out int port) == false)
            throw new InvalidOperationException("Not able to convert port number.");

        var node = new DeviceTreeIoLinkMasterPort()
        {
            Id = ioLinkMasterPort.Address,
            Name = $"Port {portNumber}",
            SubIndex = port,
        };

        parentNode.Children.Add(node);
        return node;
    }

    private static string GenerateUniqueName(List<IDeviceTreeBase> children, string name)
    {
        int index = 0;
        string generatedName = name;
        while (children.Any(x => string.Equals(x.Name, generatedName, StringComparison.OrdinalIgnoreCase)))
            generatedName = $"{name}_{++index}";

        return generatedName;
    }

    private static DataType GetDataType(object value)
        => value switch
        {
            bool => DataType.Flag,
            float or double => DataType.Real,
            sbyte or short or int or long => DataType.Whole,
            byte or ushort or uint or ulong => DataType.UnsignedWhole,
            string => DataType.Text,
            _ => DataType.Unknown
        };

    private static VariantValue? GetDataValueAsValue(IoLinkMaster ioLinkDevice, string elementAddress, ILogger logger)
    {
        try
        {
            VariantValue? returnValue = ioLinkDevice.GetDataValueAsValue(elementAddress, false, out bool skippedBecauseOfTimeout);
            if (skippedBecauseOfTimeout)
                logger.CannotReadDataFromIoLinkMasterWhileInTimeout(elementAddress);
            return returnValue;
        }
        catch (Exception ex)
        {
            logger.ErrorWhileReadingDataFromIoLinkMaster(elementAddress, ex);
            return null;
        }
    }

    private const int ProductIdIsduIndex = 19;

    private static string? ReadProductId(IoLinkMaster ioLinkDevice, string address, ILogger logger)
    {
        try
        {
            byte[]? raw = ioLinkDevice.Core.ReadAcyclic(address, ProductIdIsduIndex, null);
            return raw is null ? null : System.Text.Encoding.ASCII.GetString(raw).TrimEnd('\0', ' ');
        }
        catch (Exception ex)
        {
            logger.ErrorWhileReadingDataFromIoLinkMaster($"{address}/productid", ex);
            return null;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Toten bedingten Code vermeiden", Justification = "Waiting for initialization only needs to be started if the device isn't already initialized.")]
    public static async Task<DeviceTreeIoLinkMaster> ParseDeviceAsync(IoLinkMaster ioLinkDevice, ILogger logger)
    {
        // Acquire exclusive access before waiting for initialization so the polling loop
        // (which starts immediately after Initialized=true) is blocked until we are done.
        using IDisposable exclusiveAccess = await ioLinkDevice.AcquireExclusiveAccessAsync();

        // TODO: move waiting for initialization out into an extension method
        if (ioLinkDevice.Initialized == false)
        {
            const int MaxWaitTime = 30_000;
            const int RetryInterval = 100;
            const int MaxRetries = MaxWaitTime / RetryInterval;
            int retries = 0;
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(RetryInterval));
            do
            {
                await timer.WaitForNextTickAsync();
                retries++;
            } while (ioLinkDevice.Initialized == false && retries < MaxRetries);
        }

        try
        {
            var deviceTree = new DeviceTreeRoot();

            if (ioLinkDevice.Initialized)
            {
                // Not needed to clear the condition cache here: it is keyed by vendor/device id in
                // addition to the port address, so a device change naturally produces a cache miss
                // instead of requiring every full parse to re-attempt already-known-bad reads.
                ParseIoTCoreTree(ioLinkDevice, deviceTree);

                // Ports whose availability/identity has not changed since the last successful full
                // parse (IoLinkMasterPortDeviceInfo.NeedsFullRefresh is false) reuse their previously
                // read device node instead of re-reading IODD data and product info again - that is
                // the expensive part of a full parse, and re-reading it for a device that is known to
                // still be the same one connected the whole time would not learn anything new (aside
                // from a possible live reconfiguration of an otherwise still-connected device, which
                // cannot be detected this way - the same limitation the condition cache already has).
                (List<IDeviceTreeBase> nodesBeforeReuse, Dictionary<IDeviceTreeBase, IDeviceTreeBase> parentOfBeforeReuse) = BuildNodeIndex(deviceTree);
                HashSet<DeviceTreeDevice> reusedNodes = ReuseCachedDeviceNodes(ioLinkDevice, nodesBeforeReuse, parentOfBeforeReuse);

                // ReuseCachedDeviceNodes replaced some nodes in the tree, so the index above is stale
                // for those - rebuild it once here. Everything from this point on either only removes
                // nodes (RemoveUnavailableDevices, which keeps this same list in sync as it does so) or
                // writes fields / adds children, never replaces a DeviceTreeDevice node, so this single
                // shared index (instead of each step recomputing its own via GetNodeAndDescendants())
                // covers the rest of the parse.
                (List<IDeviceTreeBase> nodes, Dictionary<IDeviceTreeBase, IDeviceTreeBase> parentOf) = BuildNodeIndex(deviceTree);
                RemoveUnavailableDevices(ioLinkDevice, nodes, parentOf, logger);

                // Shared by QueryDeviceInfo and ReadIoddData, both of which look up a device's
                // PortDeviceInfo by its (suffix-less) address.
                var portInfosByAddress = ioLinkDevice.PortDeviceInfos.ToDictionary(p => p.Address);

                QueryDeviceInfo(ioLinkDevice.Core, ioLinkDevice, nodes, logger, reusedNodes, portInfosByAddress);
                await ReadIoddData(ioLinkDevice, nodes, logger, reusedNodes, portInfosByAddress);
                CacheFreshDeviceNodes(ioLinkDevice, nodes, reusedNodes);

                // Unconditional, not just when Timeout is true: a reused cached node (see
                // ReuseCachedDeviceNodes) is the exact same object stored in CachedPortDeviceNodes, so
                // if it was marked offline during an earlier parse that ran while Timeout was true, it
                // would otherwise stay stuck at Offline forever once Timeout clears again, even
                // though ReadAndDecodeProcessDataIn keeps decoding fresh values into it.
                deviceTree.SetStatus(GetConnectionStatus(ioLinkDevice));

                return deviceTree.Children.OfType<DeviceTreeIoLinkMaster>().First();
            }
            else
            {
                logger.FailedToConnectToIoLinkMaster(ioLinkDevice.Address);

                return new DeviceTreeIoLinkMaster
                {
                    Id = $"IoLink@{ioLinkDevice.Uri.DnsSafeHost}:{ioLinkDevice.Uri.Port}",
                    Description = new DeviceTreeNodeDescription
                    {
                        Text = "an ifm IO-Link device",
                    },
                    Name = IoLinkMaster.DefaultName,
                    Url = ioLinkDevice.Uri,
                    Username = ioLinkDevice.Username,
                    Password = ioLinkDevice.Password,
                    MacAddress = "00:00:00:00:00",
                    Status = ConnectionStatus.Offline,
                };
            }
        }
        catch (Exception ex)
        {
            logger.ErrorWhileParsingDevice(ioLinkDevice.Address, ex);
            throw;
        }
    }

    /// <summary>
    /// Determines the master's overall connection status for the published device tree.
    /// </summary>
    /// <param name="ioLinkDevice">The master to inspect.</param>
    /// <returns>
    /// <see cref="ConnectionStatus.Offline"/> if the master itself is unreachable; <see cref="ConnectionStatus.AuthenticationRequired"/>
    /// if it reported needing authentication (see <see cref="IoLinkMaster.Security"/>) and no username was
    /// configured to try; <see cref="ConnectionStatus.Online"/> otherwise.
    /// <para>This can't (yet) distinguish "no credentials configured" from "wrong credentials configured" -
    /// the latter currently still just looks like a normal <see cref="ConnectionStatus.Offline"/>/timeout, since
    /// telling them apart would require classifying the actual read failures as authentication errors rather
    /// than network errors, which hasn't been implemented.</para>
    /// </returns>
    internal static ConnectionStatus GetConnectionStatus(IoLinkMaster ioLinkDevice)
    {
        if (ioLinkDevice.Timeout)
            return ConnectionStatus.Offline;

        if (ioLinkDevice.Security.RequiresAuthentication && string.IsNullOrEmpty(ioLinkDevice.Username))
            return ConnectionStatus.AuthenticationRequired;

        return ConnectionStatus.Online;
    }

    public static void Initialize(string ioddDirectory, bool ioddAutoDownload, ILogger logger)
        => DeviceManager.Initialize(ioddDirectory, ioddAutoDownload, logger);

    // Single-pass, imperative index of every node and its parent. Built fresh whenever the tree
    // structure changes (a node gets added, removed, or replaced) and shared across the parse steps
    // that follow until the next structural change, instead of each of them independently walking the
    // tree again via the recursive DeviceTreeBaseExtensions.GetNodeAndDescendants().
    private static (List<IDeviceTreeBase> Nodes, Dictionary<IDeviceTreeBase, IDeviceTreeBase> ParentOf) BuildNodeIndex(IDeviceTreeBase root)
    {
        var nodes = new List<IDeviceTreeBase>();
        var parentOf = new Dictionary<IDeviceTreeBase, IDeviceTreeBase>();

        void Visit(IDeviceTreeBase node)
        {
            nodes.Add(node);
            foreach (IDeviceTreeBase child in node.Children)
            {
                parentOf[child] = node;
                Visit(child);
            }
        }

        Visit(root);
        return (nodes, parentOf);
    }

    // Replaces the freshly-built skeleton device node for each port whose availability/identity has not
    // changed since the last successful full parse (IoLinkMasterPortDeviceInfo.NeedsFullRefresh is
    // false) with the node cached from that last successful parse. QueryDeviceInfo and ReadIoddData
    // skip every node returned here, so its expensive per-device reads (product info, IODD data,
    // process data) are not repeated for a device that is known to still be the same one.
    private static HashSet<DeviceTreeDevice> ReuseCachedDeviceNodes(IoLinkMaster ioLinkDevice, List<IDeviceTreeBase> nodes, Dictionary<IDeviceTreeBase, IDeviceTreeBase> parentOf)
    {
        var reusedNodes = new HashSet<DeviceTreeDevice>();
        var skeletonsByAddress = nodes.OfType<DeviceTreeDevice>().ToDictionary(d => d.Id);

        foreach (IoLinkMasterPortDeviceInfo portInfo in ioLinkDevice.PortDeviceInfos)
        {
            if (portInfo.NeedsFullRefresh ||
                ioLinkDevice.CachedPortDeviceNodes.TryGetValue(portInfo.PortIndex, out DeviceTreeDevice? cachedNode) is false ||
                skeletonsByAddress.TryGetValue(portInfo.Address, out DeviceTreeDevice? skeletonNode) is false ||
                parentOf.TryGetValue(skeletonNode, out IDeviceTreeBase? parent) is false)
                continue;

            parent.Children.Remove(skeletonNode);
            parent.Children.Add(cachedNode);
            reusedNodes.Add(cachedNode);
        }

        return reusedNodes;
    }

    // Caches every device node that was freshly (re-)read this parse - i.e. not one of the reusedNodes
    // spliced in above - so the next full parse can reuse it for as long as
    // IoLinkMasterPortDeviceInfo.NeedsFullRefresh stays false for that port.
    private static void CacheFreshDeviceNodes(IoLinkMaster ioLinkDevice, List<IDeviceTreeBase> nodes, HashSet<DeviceTreeDevice> reusedNodes)
    {
        var devicesByAddress = nodes.OfType<DeviceTreeDevice>()
            .ToDictionary(d => d.Id.Split('~')[0]);

        foreach (IoLinkMasterPortDeviceInfo portInfo in ioLinkDevice.PortDeviceInfos)
        {
            if (portInfo.Available is not true)
            {
                // No (known-good) device at this port - nothing to cache, and make sure a device that
                // reappears later always gets a full, fresh read rather than reusing stale data.
                ioLinkDevice.CachedPortDeviceNodes.Remove(portInfo.PortIndex);
                continue;
            }

            // A reused node is already the cached one from a previous parse; nothing changed. A missing
            // node means RemoveUnavailableDevices just removed it (bad status) - leave NeedsFullRefresh
            // set so the next parse tries a full read again instead of wrongly marking this "done".
            if (devicesByAddress.TryGetValue(portInfo.Address, out DeviceTreeDevice? deviceNode) is false || reusedNodes.Contains(deviceNode))
                continue;

            // QueryDeviceInfo reads this device's vendor/device id again independently (and can fall
            // back to 0 if that read fails, e.g. transient errors on a flaky master) - only cache the
            // result once it actually matches what PollingAsync already confirmed live. Caching a
            // degraded/incomplete read (wrongly marked IsUnknown, wrong vendor/device id) would get
            // stuck forever, since nothing else would trigger another refresh for this port.
            if (deviceNode.VendorId != portInfo.VendorId || deviceNode.DeviceId != portInfo.DeviceId)
                continue;

            ioLinkDevice.CachedPortDeviceNodes[portInfo.PortIndex] = deviceNode;
            portInfo.NeedsFullRefresh = false;
        }
    }

    private const int ProcessDataReadRetries = 2;
    private const int ProcessDataReadRetryDelayMilliseconds = 300;

    // A PDIN read can fail transiently right after a reconnect (e.g. IO-Link negotiation still
    // settling on the master), even though the device itself is already confirmed available -
    // retrying a couple of times a short moment later resolves that without having to treat the
    // device's process data as gone.
    private static async Task<List<DisplayValue>> ReadProcessDataWithRetryAsync(IoLinkMaster masterDevice, string address, IDevice device, DeviceTreeDevice deviceNode, ILogger logger)
    {
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                return masterDevice.ReadAndDecodeProcessDataIn(address, device, deviceNode.VendorId, deviceNode.DeviceId, skipIfInTimeout: false);
            }
            catch (Exception ex)
            {
                if (attempt >= ProcessDataReadRetries)
                {
                    logger.ErrorWhileReadingAndDecodingProcessData(address, ex);
                    return [];
                }

                await Task.Delay(ProcessDataReadRetryDelayMilliseconds);
            }
        }
    }

    private static async Task ReadIoddData(IoLinkMaster masterDevice, List<IDeviceTreeBase> nodes, ILogger logger, HashSet<DeviceTreeDevice> reusedNodes,
        Dictionary<string, IoLinkMasterPortDeviceInfo> portInfosByAddress)
    {
        IEnumerable<DeviceTreeDevice> deviceNodes = nodes
            .OfType<DeviceTreeIoLinkMasterPort>()
            .SelectMany(p => p.Children.OfType<DeviceTreeDevice>())
            .Where(d => reusedNodes.Contains(d) is false);

        foreach (DeviceTreeDevice deviceNode in deviceNodes)
        {
            IDevice? device = null;
            try
            {
                if (deviceNode.VendorId > 0 && deviceNode.DeviceId > 0)
                {
                    device = IoddCatalog.GetIoddDevice(deviceNode.VendorId, deviceNode.DeviceId);
                    if (device is null && IoddCatalog.IoddAutoDownload)
                    {
                        try { IoddCatalog.DownloadIodd(deviceNode.VendorId, deviceNode.DeviceId, false, false); }
                        catch (Exception ex) { logger.ErrorWhileDownloadingIodd(masterDevice.Address, deviceNode.VendorId, deviceNode.DeviceId, ex); }
                        device = IoddCatalog.GetIoddDevice(deviceNode.VendorId, deviceNode.DeviceId);
                    }
                }
            }
            catch (Exception ex) { logger.ErrorWhileLoadingIodd(masterDevice.Address, deviceNode.VendorId, deviceNode.DeviceId, ex); }

            if (device is null)
            {
                deviceNode.IsUnknown = true;
                deviceNode.Description = new DeviceTreeNodeDescription()
                {
                    Text = $"IODD not found. To add IODD's, ask your system administrator. The following information is required to identify the IODD's needed: Vendor ID: {deviceNode.VendorId}, Device ID: {deviceNode.DeviceId}",
                    Icon = "alert-box-outline.svg",
                };

                continue;
            }

            // Match the IODD variant by ProductName; only when several share it (e.g. Pepperl-Fuchs
            // DK12-11) read the slow ISDU ProductId to disambiguate. It's used for nothing else.
            List<IDeviceVariant> variants = device.DeviceIdentity?.DeviceVariants?.ToList() ?? [];
            List<IDeviceVariant> byName = variants.Where(d => d.Name == deviceNode.Name).ToList();

            IDeviceVariant? deviceInfo;
            if (byName.Count == 1)
            {
                // Unique name match - skip the slow ISDU ProductId read.
                deviceInfo = byName[0];
            }
            else
            {
                // Zero or several name matches - read the ISDU ProductId to pick the right one.
                deviceNode.ProductId ??= ReadProductId(masterDevice, deviceNode.Id.Split('~')[0], logger);
                deviceInfo = variants.FirstOrDefault(d => d.ProductId == deviceNode.ProductId)
                    ?? byName.FirstOrDefault();
            }

            deviceNode.Description = new DeviceTreeNodeDescription()
            {
                Text = deviceInfo?.Description ?? string.Empty,
                Icon = deviceInfo?.DeviceSymbol,
            };

            string address = deviceNode.Id.Split('~')[0];
            if (string.IsNullOrWhiteSpace(address))
                continue;

            List<DisplayValue> decodedPdin = masterDevice.Timeout
                ? []
                : await ReadProcessDataWithRetryAsync(masterDevice, address, device, deviceNode, logger);

            if (decodedPdin.Count > 0)
            {
                foreach (DisplayValue value in decodedPdin)
                {
                    DataType dataType = GetDataType(value.Value);
                    var processDataNode = new DeviceTreeProcessData()
                    {
                        DataType = dataType,
                        Description = new DeviceTreeNodeDescription() { Text = value.Description ?? string.Empty },
                        HasUnit = dataType is DataType.Real or DataType.Whole,
                        Id = $"{deviceNode.Id}~{value.Name}~{value.SubIndex}",
                        Name = GenerateUniqueName(deviceNode.Children, value.Name),
                        Unit = value.Unit ?? string.Empty,
                        SubIndex = value.SubIndex ?? 0,
                    };

                    deviceNode.Children.Add(processDataNode);
                }
            }
            // decodedPdin is still empty even after ReadProcessDataWithRetryAsync's retries (or the
            // master is in Timeout). This deviceNode is freshly built and has no children yet, so
            // leaving it as-is would publish a device that looks online but has lost all of its
            // process data nodes for this one parse, indistinguishable from them having genuinely
            // gone offline. Fall back to the previously cached children for the same vendor/device
            // id instead of publishing an empty set.
            else if (portInfosByAddress.TryGetValue(address, out IoLinkMasterPortDeviceInfo? portInfo) &&
                masterDevice.CachedPortDeviceNodes.TryGetValue(portInfo.PortIndex, out DeviceTreeDevice? cachedDeviceNode) &&
                cachedDeviceNode.VendorId == deviceNode.VendorId && cachedDeviceNode.DeviceId == deviceNode.DeviceId)
            {
                foreach (IDeviceTreeBase cachedChild in cachedDeviceNode.Children)
                    deviceNode.Children.Add(cachedChild);
            }

            if (deviceNode.IsRecordingDevice)
            {
                deviceNode.Children.Add(new DeviceTreeBlobData()
                {
                    Description = new DeviceTreeNodeDescription() { Text = "Data recording" },
                    Id = $"{deviceNode.Id}~{int.MaxValue}",
                    Name = DeviceTreeBlobData.DefaultProcessName,
                });
            }
        }
    }

    private static void ParseIoTCoreTree(IoLinkMaster ioLinkDevice, IDeviceTreeBase deviceTree)
    {
        foreach (IBaseElement ioLinkMaster in GetIoLinkMasters(ioLinkDevice.Core.Root).ToArray())
        {
            IDeviceTreeBase masterDeviceNode = AddIoLinkMasterNode(ioLinkMaster, deviceTree, ioLinkDevice);

            foreach (IBaseElement port in GetIoLinkMasterPorts(ioLinkMaster).ToArray())
            {
                IDeviceTreeBase portNode = AddIoLinkMasterPortNode(port, masterDeviceNode);

                foreach (IBaseElement device in GetIoLinkMasterDevices(port).ToArray())
                {
                    AddIoLinkDeviceNode(device, portNode);
                }
            }
        }
    }

    // Navigates the live ifm.IoTCore element tree of the master itself (which masters/ports/devices are
    // currently present) - unrelated to IoddCatalog's job of loading/caching/downloading IODD file
    // definitions from disk, which is why these live here instead.
    internal static IEnumerable<IBaseElement> GetIoLinkMasters(IBaseElement element)
        => element.GetElementsByPredicate(IsIoLinkMaster, recurse: true, includeSelf: false);

    internal static IEnumerable<IBaseElement> GetIoLinkMasterDevices(IBaseElement element)
        => element.GetElementsByPredicate(IsIoLinkMasterDevice, recurse: true, includeSelf: false);

    private static IEnumerable<IBaseElement> GetIoLinkMasterPorts(IBaseElement element)
        => element.GetElementsByPredicate(IsIoLinkMasterPort, recurse: true, includeSelf: false);

    internal static int GetPortIndex(IBaseElement portElement)
    {
        Match match = PortRegex().Match(portElement.Identifier);
        return match.Success
            ? int.Parse(match.Groups["port"].Value, CultureInfo.InvariantCulture)
            : -1;
    }

    private static bool IsIoLinkMaster(IBaseElement element)
        => element.Identifier == IoLinkMasterIdentifier &&
           element.Type == "structure";

    private static bool IsIoLinkMasterDevice(IBaseElement element)
        => element.Identifier == "iolinkdevice" &&
           element.Type == "structure" &&
           element.Parent.Type == "structure";

    private static bool IsIoLinkMasterPort(IBaseElement element)
        => PortRegex().IsMatch(element.Identifier) &&
           element.Type == "structure" &&
           element.Parent.Type == "structure";

    [GeneratedRegex("port\\[(?<port>\\d+)\\]")]
    private static partial Regex PortRegex();

    private static void QueryDeviceInfo(IIoTCore core, IoLinkMaster ioLinkDevice, List<IDeviceTreeBase> nodes, ILogger logger, HashSet<DeviceTreeDevice> reusedNodes,
        Dictionary<string, IoLinkMasterPortDeviceInfo> portInfosByAddress)
    {
        DeviceTreeDevice[] ioLinkMasterDevices = [.. nodes
            .OfType<DeviceTreeDevice>()
            .Where(d => reusedNodes.Contains(d) is false)
            .Where(d => core.ElementManager.GetElementByAddress(d.Id, acquireLock: true).Profiles.FirstOrDefault() == ProfileIoLinkDeviceFull)];

        foreach (DeviceTreeDevice device in ioLinkMasterDevices)
        {
            device.ApplicationSpecificTag = GetDataValueAsValue(ioLinkDevice, $"{device.Id}{AddressApplicationSpecificTag}", logger)?.GetString() ?? string.Empty;

            // Prefer VendorId/DeviceId from PollingAsync's already-confirmed live read (portInfo)
            // over an independent re-read here: a re-read can fail on a flaky master and silently
            // fall back to 0, producing a node Id whose "~vendorId~deviceId" suffix no longer
            // matches this same device's Id from any other parse. portInfo's values can still be
            // null on the very first parse right after startup, before any polling cycle has run -
            // fall back to reading independently only in that case.
            if (portInfosByAddress.TryGetValue(device.Id, out IoLinkMasterPortDeviceInfo? portInfo) &&
                portInfo.VendorId.HasValue && portInfo.DeviceId.HasValue)
            {
                device.VendorId = portInfo.VendorId.Value;
                device.DeviceId = portInfo.DeviceId.Value;
            }
            else
            {
                device.DeviceId = GetDataValueAsValue(ioLinkDevice, $"{device.Id}{AddressDeviceId}", logger)?.GetUInt32() ?? 0;
                device.VendorId = GetDataValueAsValue(ioLinkDevice, $"{device.Id}{AddressVendorId}", logger)?.GetUInt16() ?? 0;
            }

            // Name and ProductId are static per physical device - if the same vendor/device id is
            // already cached for this port, this full refresh was triggered by something other than
            // an actual device swap (e.g. a brief timeout), so reuse the cached Name/ProductId
            // instead of reading them again. This avoids capturing a transient placeholder the
            // master can report right after a reconnect, before identification has fully settled
            // (e.g. "unknown" for /productname) - such a value would otherwise get cached as if it
            // were permanent, since nothing else would trigger another refresh for this port.
            if (portInfo is not null &&
                ioLinkDevice.CachedPortDeviceNodes.TryGetValue(portInfo.PortIndex, out DeviceTreeDevice? cachedDeviceNode) &&
                cachedDeviceNode.VendorId == device.VendorId && cachedDeviceNode.DeviceId == device.DeviceId)
            {
                device.Name = cachedDeviceNode.Name;
                device.ProductId = cachedDeviceNode.ProductId;
            }
            else
            {
                device.Name = GetDataValueAsValue(ioLinkDevice, $"{device.Id}{AddressProductName}", logger)?.GetString() ?? string.Empty;

                // ProductId (a slow ISDU read) is only needed to disambiguate same-named IODD variants;
                // ReadIoddData reads it lazily there, so most devices never pay for it.
            }

            // After reading, the device's internal ID is amended so it's possible to detect whether the type of
            // device on the port has changed.
            // If the type is the same (e.g. a replacement device), it's recognized as the same element in the
            // DeviceTree and the existing configuration continues to be used.
            // If the type is different (e.g. swapping a distance sensor for a temperature sensor), the existing
            // configuration should be discarded.
            device.Id += $"~{device.VendorId}~{device.DeviceId}";
        }

        foreach (DeviceTreeIoLinkMaster device in nodes.OfType<DeviceTreeIoLinkMaster>())
        {
            //{IoTCore-10.45.24.143/remote/00-02-01-0F-90-EB/deviceinfo/productcode}
            string root = device.GetRoot();
            device.Name = GetDataValueAsValue(ioLinkDevice, $"{root}{AddressProductCode}", logger)?.GetString() ?? IoLinkMaster.DefaultName;
            device.BootLoaderRevision = GetDataValueAsValue(ioLinkDevice, $"{root}{AddressBootLoaderRevision}", logger)?.GetString() ?? string.Empty;
            device.SoftwareRevision = GetDataValueAsValue(ioLinkDevice, $"{root}{AddressSoftwareRevision}", logger)?.GetString() ?? string.Empty;
            device.HardwareRevision = GetDataValueAsValue(ioLinkDevice, $"{root}{AddressHardwareRevision}", logger)?.GetString() ?? string.Empty;
            device.SerialNumber = GetDataValueAsValue(ioLinkDevice, $"{root}{AddressSerialNumber}", logger)?.GetString() ?? string.Empty;
            device.ProductName = GetDataValueAsValue(ioLinkDevice, $"{root}{AddressProductName}", logger)?.GetString() ?? string.Empty;
            device.Manufacturer = GetDataValueAsValue(ioLinkDevice, $"{root}{AddressDeviceVendor}", logger)?.GetString() ?? string.Empty;
            device.DeviceFamily = GetDataValueAsValue(ioLinkDevice, $"{root}{AddressDeviceFamily}", logger)?.GetString() ?? string.Empty;
        }
    }

    private static void RemoveUnavailableDevices(IoLinkMaster ioLinkDevice, List<IDeviceTreeBase> nodes, Dictionary<IDeviceTreeBase, IDeviceTreeBase> parentOf, ILogger logger)
    {
        // Snapshot first: the loop below removes entries from nodes as it goes, which would otherwise
        // disrupt the enumeration.
        foreach (DeviceTreeDevice device in nodes.OfType<DeviceTreeDevice>().ToArray())
        {
            // device.Id may already carry the "~vendorId~deviceId" suffix appended by QueryDeviceInfo
            // if this node was reused from the cache (see ReuseCachedDeviceNodes) - strip it back off
            // to get the real element address for this status read.
            string address = device.Id.Split('~')[0];
            int status = GetDataValueAsValue(ioLinkDevice, $"{address}{AddressStatus}", logger)?.GetInt32() ?? 0;
            if (status != 2)
            {
                if (parentOf.TryGetValue(device, out IDeviceTreeBase? parent))
                    parent.Children.Remove(device);
                // Keep the shared node index in sync: QueryDeviceInfo, ReadIoddData and
                // CacheFreshDeviceNodes reuse this same list and must not see a device just removed here.
                nodes.Remove(device);
            }
        }
    }

    [GeneratedRegex(@"\d")]
    private static partial Regex PortNumberRegex();
}
