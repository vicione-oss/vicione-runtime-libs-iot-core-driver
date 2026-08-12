using System.Globalization;
using System.IO.Compression;
using ifm.IoddParser;
using ifm.IoddParser.Contracts;

namespace ViciOne.Communication.IoTCore.IoddStore;

/// <summary>
/// Loads, caches, and (if enabled) downloads IODD device descriptions from a local directory tree.
/// <para>Laid out as <c>{IoddDirectory}/{vendorId}/{deviceId}/*.xml</c>.</para>
/// </summary>
public static class IoddCatalog
{
    private static readonly Lock s_ioddParserLock = new();

    /// <summary>
    /// The locale used when parsing IODD text elements.
    /// </summary>
    public static string DefaultLocale { get; } = "en";

    /// <summary>
    /// Whether <see cref="GetIoddDevice"/>'s callers should fall back to <see cref="DownloadIodd"/> for a
    /// device not found locally.
    /// </summary>
    public static bool IoddAutoDownload { get; set; }

    /// <summary>
    /// The root directory IODDs are stored under, as passed to <see cref="Initialize"/>.
    /// </summary>
    public static string IoddDirectory { get; private set; } = "<uninitialized>";

    /// <summary>
    /// The underlying parser, populated with whichever devices have been loaded so far via
    /// <see cref="GetIoddDevice"/>.
    /// </summary>
    public static IIoddParser? IoddParser { get; private set; }

    /// <summary>
    /// Whether <see cref="Initialize"/> has been called.
    /// </summary>
    public static bool IsInitialized => IoddParser is not null;

    /// <summary>
    /// Downloads a device's IODD archive from the IODDfinder API and extracts it into
    /// <see cref="GetDeviceDirectory"/>.
    /// <para>Moves its vendor logo up into <see cref="GetVendorDirectory"/> if none is there yet.</para>
    /// </summary>
    /// <param name="vendorId">The device's vendor ID.</param>
    /// <param name="deviceId">The device's device ID.</param>
    /// <param name="deleteImages">Whether to delete the extracted PNG images after moving the vendor logo.</param>
    /// <param name="deleteLanguageFiles">Whether to keep only the English (*1.xml) IODD file, deleting the other language variants.</param>
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

        // Move vendor logo
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

    /// <summary>
    /// The directory a specific device's IODD files live (or should be extracted) in.
    /// </summary>
    /// <param name="vendorId">The device's vendor ID.</param>
    /// <param name="deviceId">The device's device ID.</param>
    /// <returns>The device's IODD directory, under <see cref="GetVendorDirectory"/>.</returns>
    public static string GetDeviceDirectory(ushort vendorId, uint deviceId)
        => Path.Combine(GetVendorDirectory(vendorId), deviceId.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Gets a device's parsed IODD.
    /// <para>Loads it from <see cref="GetDeviceDirectory"/> on first access if it isn't already cached in
    /// <see cref="IoddParser"/>.</para>
    /// </summary>
    /// <param name="vendorId">The device's vendor ID.</param>
    /// <param name="deviceId">The device's device ID.</param>
    /// <returns>The device's IODD, or <see langword="null"/> if none is cached or found on disk.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Initialize"/> hasn't been called yet.</exception>
    public static IDevice? GetIoddDevice(ushort vendorId, uint deviceId)
    {
        if (IoddParser is null)
            throw new InvalidOperationException($"{nameof(IoddCatalog)} must be initialized before it can be used.");

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

    /// <summary>
    /// The directory a vendor's IODD files (across all of its devices) live under.
    /// </summary>
    /// <param name="vendorId">The vendor ID.</param>
    /// <returns>The vendor's directory, under <see cref="IoddDirectory"/>.</returns>
    public static string GetVendorDirectory(ushort vendorId)
        => Path.Combine(IoddDirectory, vendorId.ToString(CultureInfo.InvariantCulture));

    /// <summary>
    /// Sets up the catalog to load IODDs from <paramref name="ioddDirectory"/>.
    /// <para>Only takes effect the first time it's called - subsequent calls are ignored.</para>
    /// </summary>
    /// <param name="ioddDirectory">The root directory IODDs are stored under.</param>
    /// <param name="ioddAutoDownload">Initial value for <see cref="IoddAutoDownload"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="ioddDirectory"/> is empty.</exception>
    /// <exception cref="DirectoryNotFoundException"><paramref name="ioddDirectory"/> doesn't exist.</exception>
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
}
