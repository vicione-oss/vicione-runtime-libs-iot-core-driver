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
            VariantValue? returnValue = ioLinkDevice.GetDataValueAsValue(elementAddress, true, out bool skippedBecauseOfTimeout);
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Toten bedingten Code vermeiden", Justification = "Das warten auf die Initialisierung muss nur gestartet werden, wenn das Gerät nicht bereits initialisiert ist.")]
    public static async Task<DeviceTreeIoLinkMaster> ParseDeviceAsync(IoLinkMaster ioLinkDevice, ILogger logger)
    {
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
                ioLinkDevice.ClearConditionValueCache();

                ParseIoTCoreTree(ioLinkDevice.Core, deviceTree);
                RemoveUnavailableDevices(ioLinkDevice, deviceTree, logger);
                QueryDeviceInfo(ioLinkDevice.Core, ioLinkDevice, deviceTree, logger);
                ReadIoddData(ioLinkDevice, deviceTree, logger);

                if (ioLinkDevice.Timeout)
                    deviceTree.SetIsOffline(true);

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
            logger.LogError($"Failed to scan IO-Link device: {ex.GetType().Name}, {ex.Message} {ex.StackTrace}");
            throw;
        }
    }

    public static void Initialize(string ioddDirectory, bool ioddAutoDownload, ILogger logger)
        => DeviceManager.Initialize(ioddDirectory, ioddAutoDownload, logger);

    private static void ReadIoddData(IoLinkMaster masterDevice, IDeviceTreeBase deviceTree, ILogger logger)
    {
        IEnumerable<DeviceTreeIoLinkMasterPort> portNodes = deviceTree.GetNodeAndDescendants().OfType<DeviceTreeIoLinkMasterPort>();
        IEnumerable<(DeviceTreeIoLinkMasterPort p, DeviceTreeDevice)> deviceNodes = portNodes.SelectMany(p => p.Children.Select(c => (p, (DeviceTreeDevice)c)));

        foreach ((DeviceTreeIoLinkMasterPort portNode, DeviceTreeDevice deviceNode) in deviceNodes)
        {
            IDevice? device = null;
            try
            {
                device = IoddParserManager.GetIoddDevice(deviceNode.VendorId, deviceNode.DeviceId);
                if (device is null && IoddParserManager.IoddAutoDownload)
                {
                    try { IoddParserManager.DownloadIodd(deviceNode.VendorId, deviceNode.DeviceId, false, false); }
                    catch (Exception ex) { logger.ErrorWhileDownloadingIodd(deviceNode.VendorId, deviceNode.DeviceId, ex.Message, ex); }
                    device = IoddParserManager.GetIoddDevice(deviceNode.VendorId, deviceNode.DeviceId);
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

            IDeviceVariant? deviceInfo = device.DeviceIdentity?.DeviceVariants?.FirstOrDefault(d => d.Name == deviceNode.Name);

            deviceNode.Description = new DeviceTreeNodeDescription()
            {
                Text = deviceInfo?.Description ?? string.Empty,
                Icon = deviceInfo?.DeviceSymbol,
            };

            string address = deviceNode.Id.Split('~')[0];
            if (string.IsNullOrWhiteSpace(address))
                continue;

            List<DisplayValue> decodedPdin;

            if (masterDevice.Timeout)
                decodedPdin = [];
            else
                try
                {
                    decodedPdin = masterDevice.ReadAndDecodeProcessDataIn(address, device);
                }
                catch (Exception ex)
                {
                    logger.ErrorWhileReadingAndDecodingProcessData(address, ex.Message, ex);
                    // Wenn das Lesen der Prozessdaten fehlschlägt, dann sollen (bzw. können) keine Prozessdaten-Knoten angelegt werden.
                    decodedPdin = [];
                }

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

    private static void QueryDeviceInfo(IIoTCore core, IoLinkMaster ioLinkDevice, IDeviceTreeBase deviceTree, ILogger logger)
    {
        DeviceTreeDevice[] ioLinkMasterDevices = [.. deviceTree
            .GetNodeAndDescendants()
            .OfType<DeviceTreeDevice>()
            .Where(d => core.GetElementByAddress(d.Id).Profiles.FirstOrDefault() == ProfileIoLinkDeviceFull)];

        foreach (DeviceTreeDevice device in ioLinkMasterDevices)
        {
            device.ApplicationSpecificTag = GetDataValueAsValue(ioLinkDevice, $"{device.Id}{AddressApplicationSpecificTag}", logger)?.GetString() ?? string.Empty;
            device.DeviceId = GetDataValueAsValue(ioLinkDevice, $"{device.Id}{AddressDeviceId}", logger)?.GetUInt32() ?? 0;
            device.Name = GetDataValueAsValue(ioLinkDevice, $"{device.Id}{AddressProductName}", logger)?.GetString() ?? string.Empty;
            device.VendorId = GetDataValueAsValue(ioLinkDevice, $"{device.Id}{AddressVendorId}", logger)?.GetUInt16() ?? 0;

            // Die interne ID des Devices wird nach dem Auslesen so geändert, dass man feststellen kann ob sich
            // am Port der Typ des Gerätes geändert hat.
            // Wenn der Typ gleich ist (Anwendungsfall z.B. Ersatzgerät), dann wird es im DeviceTree als dasselbe
            // Element erkannt und die vorhandene Konfiguration weiterhin verwendet.
            // Wenn der Typ anders ist (Anwendungsfall z.B. Tausch von Abstands- zu Temperatursensor), dann soll
            // die vorhandene Konfiguration verworfen werden.
            device.Id += $"~{device.VendorId}~{device.DeviceId}";
        }

        foreach (DeviceTreeIoLinkMaster device in deviceTree.GetNodeAndDescendants().OfType<DeviceTreeIoLinkMaster>())
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

    private static void RemoveUnavailableDevices(IoLinkMaster ioLinkDevice, IDeviceTreeBase deviceTree, ILogger logger)
    {
        IDeviceTreeBase[] nodes = [.. deviceTree.GetNodeAndDescendants()];
        foreach (DeviceTreeDevice device in nodes.OfType<DeviceTreeDevice>())
        {
            int status = GetDataValueAsValue(ioLinkDevice, $"{device.Id}{AddressStatus}", logger)?.GetInt32() ?? 0;
            if (status != 2)
            {
                IDeviceTreeBase? parent = nodes.FirstOrDefault(n => n.Children.Any(c => c.Id == device.Id));
                parent?.Children.Remove(device);
            }
        }
    }

    [GeneratedRegex(@"\d")]
    private static partial Regex PortNumberRegex();
}
