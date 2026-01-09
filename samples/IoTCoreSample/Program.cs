using ifmIoTCore;
using ifmIoTCore.Elements;
using ifmIoTCore.NetAdapter.Http;
using ifmIoTCore.Profiles.Base;
using ifmIoTCore.Profiles.DeviceManagement;
using ifmIoTCore.Profiles.DeviceManagement.ServiceData.Requests;

namespace IoTCoreSample;

internal sealed class Program
{
    private const string IoLinkMasterAddress = "10.45.24.141";

    internal static void Main()
    {
        Uri uri = new UriBuilder(IoLinkMasterAddress).Uri;
        string iotCoreName = $"IoTCore-{uri.DnsSafeHost}-{uri.Port}";
        using IIoTCore core = IoTCoreFactory.Create(iotCoreName);

        using var httpClientNetAdapterFactory = new HttpClientNetAdapterFactory(
            new ifmIoTCore.MessageConverter.Json.Microsoft.MessageConverter(),
            TimeSpan.FromSeconds(30), true);
        core.RegisterClientNetAdapterFactory(httpClientNetAdapterFactory);

        ProfileBuilderConfiguration configuration = new() { Address = iotCoreName, IoTCore = core };
        var deviceProfileBuilder = new DeviceManagementProfileBuilder(configuration);
        deviceProfileBuilder.Build();
        IBaseElement deviceElement = deviceProfileBuilder.Mirror(new MirrorRequestServiceData(uri.AbsoluteUri, null));

        Console.WriteLine($"Device Identifier of {IoLinkMasterAddress}: {deviceElement.Identifier}");
    }
}
