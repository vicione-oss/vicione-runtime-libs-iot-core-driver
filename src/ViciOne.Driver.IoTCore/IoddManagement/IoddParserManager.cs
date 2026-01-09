using ifm.IoddParser;
using ifm.IoddParser.Contracts;
using ifmIoTCore.Elements;
using ifmIoTCore.IoddExtender.Utilities;
using ifmIoTCore.Profiles.DeviceManagement.Elements;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;

namespace ViciOne.Driver.IoTCore.IoddManagement;

public static partial class IoddParserManager
{
    private static readonly Lock s_ioddParserLock = new();

    public static string DefaultLocale { get; } = "en";
    public static bool IoddAutoDownload { get; set; }
    public static string IoddDirectory { get; private set; } = "<uninitialized>";
    public static IIoddParser? IoddParser { get; private set; }
    public static bool IsInitialized => IoddParser is not null;

    public static void DownloadIodd(ushort vendorId, uint deviceId, bool deleteImages, bool deleteLanguageFiles)
    {
        string vendorDirectory = GetVendorDirectory(vendorId);
        string destinationDirectory = GetDeviceDirectory(vendorId, deviceId);

        using (MemoryStream archiveStream = IoddDownloader.DownloadIoddArchiveFile(vendorId, deviceId))
            ZipFile.ExtractToDirectory(archiveStream, destinationDirectory);

        // Rechte setzen
        SetUnixFileMode(vendorDirectory);
        SetUnixFileMode(destinationDirectory);

        foreach (string file in Directory.GetFiles(destinationDirectory))
            SetUnixFileMode(file);

        // Vendor Logo verschieben
        if (Directory.GetFiles(vendorDirectory, "*logo.png").Length == 0)
        {
            foreach (string logoFile in Directory.GetFiles(destinationDirectory, "*logo.png"))
                File.Move(logoFile, Path.Combine(vendorDirectory, Path.GetFileName(logoFile)));
        }
        else
        {
            foreach (string logoFile in Directory.GetFiles(destinationDirectory, "*logo.png"))
                File.Delete(logoFile);
        }

        if (deleteImages)
            foreach (string file in Directory.GetFiles(destinationDirectory, "*.png"))
                File.Delete(file);

        if (deleteLanguageFiles)
            foreach (string xml in Directory.GetFiles(destinationDirectory, "*.xml"))
                if (xml.EndsWith("1.xml", StringComparison.OrdinalIgnoreCase) == false)
                    File.Delete(xml);
    }

    private static void SetUnixFileMode(string path, UnixFileMode mode = UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute | UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute)
    {
        if (OperatingSystem.IsLinux())
        {
            File.SetUnixFileMode(path, mode);
        }
    }

    public static string GetDeviceDirectory(ushort vendorId, uint deviceId)
        => Path.Combine(GetVendorDirectory(vendorId), deviceId.ToString(CultureInfo.InvariantCulture));

    public static IDevice? GetIoddDevice(ushort vendorId, uint deviceId)
    {
        if (IoddParser is null)
            throw new InvalidOperationException($"{nameof(IoddParserManager)} must be initialized before it can be used.");

        IDevice device = IoddParser.GetDevice(vendorId, deviceId);
        if (device is not null)
            return device;

        string deviceDirectory = GetDeviceDirectory(vendorId, deviceId);
        if (Directory.Exists(deviceDirectory) == false)
            return null;

        foreach (string file in Directory.GetFiles(deviceDirectory, "*-IODD1.1.xml", SearchOption.AllDirectories))
            IoddParser.AddDeviceFromFile(file);

        foreach (string file in Directory.GetFiles(deviceDirectory, "*-IODD1.0.1.xml", SearchOption.AllDirectories))
            IoddParser.AddDeviceFromFile(file);

        return IoddParser.GetDevice(vendorId, deviceId);
    }

    public static IEnumerable<IBaseElement> GetIoLinkMasters(IBaseElement element)
        => element.GetElementsByPredicate(IsIoLinkMaster);

    public static IEnumerable<IBaseElement> GetIoLinkMasterDevices(IBaseElement element)
        => element.GetElementsByPredicate(IsIoLinkMasterDevice);

    public static IEnumerable<IBaseElement> GetIoLinkMasterPorts(IBaseElement element)
        => element.GetElementsByPredicate(IsIoLinkMasterPort);

    public static int GetPortIndex(IBaseElement portElement)
    {
        Match match = PortRegex().Match(portElement.Identifier);
        return match.Success
            ? int.Parse(match.Groups["port"].Value, CultureInfo.InvariantCulture)
            : -1;
    }

    public static string GetVendorDirectory(ushort vendorId)
        => Path.Combine(IoddDirectory, vendorId.ToString(CultureInfo.InvariantCulture));

    public static void Initialize(string ioddDirectory, bool ioddAutoDownload = false)
    {
        if (IsInitialized)
            return;

        IoddAutoDownload = ioddAutoDownload;
        IoddDirectory = ioddDirectory;

        if (string.IsNullOrWhiteSpace(ioddDirectory))
            throw new ArgumentException("Directory cannot be empty.", nameof(ioddDirectory));

        if (Directory.Exists(ioddDirectory) == false)
            throw new DirectoryNotFoundException($"Directory '{ioddDirectory}' does not exist.");

        lock (s_ioddParserLock)
            IoddParser = IoddParserFactory.New(DefaultLocale);
    }

    public static bool IsIoLinkMaster(IBaseElement element)
        => element is IProxyElement &&
           element.Identifier == IoTCoreConstants.IoLinkMasterProfile;

    public static bool IsIoLinkMasterDevice(IBaseElement element)
        => element.Identifier == "iolinkdevice" &&
           element.Type == "structure" &&
           element.Parent.Type == "structure";

    public static bool IsIoLinkMasterPort(IBaseElement element)
        => PortRegex().IsMatch(element.Identifier) &&
           element.Type == "structure" &&
           element.Parent.Type == "structure";

    [GeneratedRegex("port\\[(?<port>\\d+)\\]")]
    private static partial Regex PortRegex();
}
