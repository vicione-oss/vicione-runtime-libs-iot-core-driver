using ifm.IoddParser.Contracts;
using ViciOne.Driver.IoTCore.IoddManagement;

namespace IoddSample;

internal sealed class Program
{
    private static readonly string s_ioddDirectory = Path.GetFullPath(@"..\..\..\..\..\artifacts\IODDs");

    private const ushort DeviceVendorId = 888;
    private const uint DeviceDeviceId = 917762;

    internal static void Main()
    {
        IoddParserManager.Initialize(s_ioddDirectory, true);

        IDevice? device = null;
        try
        {
            device = IoddParserManager.GetIoddDevice(DeviceVendorId, DeviceDeviceId);
            if (device is null && IoddParserManager.IoddAutoDownload)
            {
                Console.WriteLine("IODD not found locally, attempting to download...");
                try { IoddParserManager.DownloadIodd(DeviceVendorId, DeviceDeviceId, false, false); }
                catch (Exception ex) { Console.WriteLine($"Error downloading IODD: {ex.Message}"); }
                device = IoddParserManager.GetIoddDevice(DeviceVendorId, DeviceDeviceId);
            }
        }
        catch (Exception ex) { Console.WriteLine($"Error retrieving IODD: {ex.Message}"); }

        Console.WriteLine(device != null
            ? $"Found IODD for device '{device.DeviceIdentity.VendorName}' '{device.DeviceIdentity.DeviceName}'"
            : $"No IODD found for device with VendorId={DeviceVendorId} and DeviceId={DeviceDeviceId}");
    }
}
