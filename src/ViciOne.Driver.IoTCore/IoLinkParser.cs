using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ifm.IoddParser.Contracts;
using ifm.IoddParser.Helpers;
using ifmIoTCore;
using ifmIoTCore.Common.Variant;
using ifmIoTCore.Elements;
using ifmIoTCore.Profiles.DeviceManagement.Elements;
using Microsoft.Extensions.Logging;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree.Extensions;
using ViciOne.Driver.IoTCore.Extensions;
using ViciOne.Driver.IoTCore.IoddManagement;

namespace ViciOne.Driver.IoTCore;

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

    private static DeviceTreeIoLinkMaster AddIoLinkMasterNode(IBaseElement ioLinkMaster, IDeviceTreeBase parentNode)
    {
        string remoteDeviceIdentifier = ioLinkMaster.Parent.Identifier;
        var node = new DeviceTreeIoLinkMaster()
        {
            Id = ioLinkMaster.Address,
            MacAddress = remoteDeviceIdentifier,
            Name = IoLinkMaster.DefaultName,
            Url = (ioLinkMaster as IProxyElement)!.RemoteContext.Uri,
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
            bool => DataType.BooleanT,
            float or double => DataType.Float32T,
            sbyte or short or int or long => DataType.IntegerT,
            byte or ushort or uint or ulong => DataType.UIntegerT,
            string => DataType.StringT,
            _ => DataType.Invalid
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
            logger.ErrorWhileReadingDataFromIoLinkMaster(elementAddress, ex.Message, ex);
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
            logger.ErrorWhileReadingDataFromIoLinkMaster($"{address}/productid", ex.Message, ex);
            return null;
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Toten bedingten Code vermeiden", Justification = "Das warten auf die Initialisierung muss nur gestartet werden, wenn das Gerät nicht bereits initialisiert ist.")]
    public static async Task<DeviceTreeIoLinkMaster> ParseDeviceAsync(IoLinkMaster ioLinkDevice, ILogger logger)
    {
        // Acquire exclusive access before waiting for initialization so the polling loop
        // (which starts immediately after Initialized=true) is blocked until we are done.
        using IDisposable exclusiveAccess = await ioLinkDevice.AcquireExclusiveAccessAsync();

        // TODO: Warten auf die Initialisierung in Erweiterungsmethode auslagern
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
                ParseIoTCoreTree(ioLinkDevice.Core, deviceTree);

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
                Dictionary<string, IoLinkMasterPortDeviceInfo> portInfosByAddress = ioLinkDevice.PortDeviceInfos.ToDictionary(p => p.Address);

                QueryDeviceInfo(ioLinkDevice.Core, ioLinkDevice, nodes, logger, reusedNodes, portInfosByAddress);
                await ReadIoddData(ioLinkDevice, nodes, logger, reusedNodes, portInfosByAddress);
                CacheFreshDeviceNodes(ioLinkDevice, nodes, reusedNodes);

                // Unconditional, not just when Timeout is true: a reused cached node (see
                // ReuseCachedDeviceNodes) is the exact same object stored in CachedPortDeviceNodes, so
                // if it was marked offline during an earlier parse that ran while Timeout was true, it
                // would otherwise stay stuck at IsOffline=true forever once Timeout clears again, even
                // though ReadAndDecodeProcessDataIn keeps decoding fresh values into it.
                deviceTree.SetIsOffline(ioLinkDevice.Timeout);

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
                    MacAddress = "00:00:00:00:00",
                    IsOffline = true,
                };
            }
        }
        catch (Exception ex)
        {
            logger.ErrorWhileParsingDevice(ioLinkDevice.Address, ex.Message, ex);
            throw;
        }
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
        Dictionary<string, DeviceTreeDevice> skeletonsByAddress = nodes.OfType<DeviceTreeDevice>().ToDictionary(d => d.Id);

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
        Dictionary<string, DeviceTreeDevice> devicesByAddress = nodes.OfType<DeviceTreeDevice>()
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
                    logger.ErrorWhileReadingAndDecodingProcessData(address, ex.Message, ex);
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
                    device = IoddParserManager.GetIoddDevice(deviceNode.VendorId, deviceNode.DeviceId);
                    if (device is null && IoddParserManager.IoddAutoDownload)
                    {
                        try { IoddParserManager.DownloadIodd(deviceNode.VendorId, deviceNode.DeviceId, false, false); }
                        catch (Exception ex) { logger.ErrorWhileDownloadingIodd(deviceNode.VendorId, deviceNode.DeviceId, ex.Message, ex); }
                        device = IoddParserManager.GetIoddDevice(deviceNode.VendorId, deviceNode.DeviceId);
                    }
                }
            }
            catch (Exception ex) { logger.ErrorWhileLoadingIodd(deviceNode.VendorId, deviceNode.DeviceId, ex.Message, ex); }

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

            // Prefer matching by ProductId: some IODDs define multiple DeviceVariants that share
            // the same ProductName text (see e.g. Pepperl-Fuchs DK12-11, where two variants both
            // resolve to "DK12-11-IO/92/136"), so Name alone can pick the wrong variant's image.
            // Fall back to Name when ProductId is unavailable (e.g. a still-cached node from
            // before this field existed) to avoid regressing devices that already match by name.
            IDeviceVariant? deviceInfo = device.DeviceIdentity?.DeviceVariants?.FirstOrDefault(d => d.ProductId == deviceNode.ProductId)
                ?? device.DeviceIdentity?.DeviceVariants?.FirstOrDefault(d => d.Name == deviceNode.Name);

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
                        HasUnit = dataType is DataType.Float32T or DataType.IntegerT,
                        Id = $"{deviceNode.Id}~{value.Name}~{value.SubIndex}",
                        Name = GenerateUniqueName(deviceNode.Children, value.Name),
                        ShowDetailButton = dataType == DataType.StringT,
                        StructureUnit = value.Unit ?? string.Empty,
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

    private static void ParseIoTCoreTree(IIoTCore core, IDeviceTreeBase deviceTree)
    {
        foreach (IBaseElement ioLinkMaster in IoddParserManager.GetIoLinkMasters(core.Root).ToArray())
        {
            IDeviceTreeBase masterDeviceNode = AddIoLinkMasterNode(ioLinkMaster, deviceTree);

            foreach (IBaseElement port in IoddParserManager.GetIoLinkMasterPorts(ioLinkMaster).ToArray())
            {
                IDeviceTreeBase portNode = AddIoLinkMasterPortNode(port, masterDeviceNode);

                foreach (IBaseElement device in IoddParserManager.GetIoLinkMasterDevices(port).ToArray())
                {
                    AddIoLinkDeviceNode(device, portNode);
                }
            }
        }
    }

    private static void QueryDeviceInfo(IIoTCore core, IoLinkMaster ioLinkDevice, List<IDeviceTreeBase> nodes, ILogger logger, HashSet<DeviceTreeDevice> reusedNodes,
        Dictionary<string, IoLinkMasterPortDeviceInfo> portInfosByAddress)
    {
        DeviceTreeDevice[] ioLinkMasterDevices = [.. nodes
            .OfType<DeviceTreeDevice>()
            .Where(d => reusedNodes.Contains(d) is false)
            .Where(d => core.GetElementByAddress(d.Id).Profiles.FirstOrDefault() == ProfileIoLinkDeviceFull)];

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

                // Not exposed as a simple mirrored address like ProductName/VendorId/DeviceId - read
                // via the standard ISDU index for Product ID instead. Used in ReadIoddData to find
                // the matching IODD DeviceVariant (e.g. for its product image) for devices whose
                // IODD defines multiple variants under the same vendor/device id.
                device.ProductId = ReadProductId(ioLinkDevice, device.Id.Split('~')[0], logger);
            }

            // Die interne ID des Devices wird nach dem Auslesen so geändert, dass man feststellen kann ob sich
            // am Port der Typ des Gerätes geändert hat.
            // Wenn der Typ gleich ist (Anwendungsfall z.B. Ersatzgerät), dann wird es im DeviceTree als dasselbe
            // Element erkannt und die vorhandene Konfiguration weiterhin verwendet.
            // Wenn der Typ anders ist (Anwendungsfall z.B. Tausch von Abstands- zu Temperatursensor), dann soll
            // die vorhandene Konfiguration verworfen werden.
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
