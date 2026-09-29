using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;
using Moq;
using ViciOne.Communication.Vse;

namespace ViciOne.Driver.Vse.Tests;

/// <summary>
/// Shared plumbing for tests that need a <see cref="VseDevice"/> wired to a mocked <see cref="IVSEDevice"/>
/// instead of real hardware - used by both <see cref="VseDeviceTests"/> and <see cref="VsePushLatencyBenchmark"/>.
/// </summary>
internal static class VseTestDevice
{
    public const string Address = "http://10.100.11.132:3321";

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership transfers into the returned VseDevice, which disposes its Connection - the caller disposes the VseDevice.")]
    public static (VseDevice Device, Mock<IVSEDevice> Sdk, Mock<ICommands> Commands, Action<ICounterDataSample> PushCounter) CreateDevice(
        ConnectionStatus connectionStatus = ConnectionStatus.Connected,
        SystemMode systemMode = SystemMode.Supervise,
        IEnumerable<IVariant>? variants = null)
    {
        Mock<ICounter> counter = new();
        counter.Setup(c => c.Id).Returns(0);
        counter.Setup(c => c.Name).Returns("Uptime");

        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Counters).Returns([counter.Object]);
        parameters.Setup(p => p.Objects).Returns([]);
        parameters.Setup(p => p.Inputs).Returns([]);
        parameters.Setup(p => p.DigitalAlarms).Returns([]);
        parameters.Setup(p => p.AnalogAlarms).Returns([]);
        parameters.Setup(p => p.Variants).Returns([.. variants ?? []]);

        Mock<IMonitoringChannel<ICounterDataSample>> counterChannel = new();
        Action<ICounterDataSample>? pushCounter = null;
        counterChannel.Setup(c => c.Start(It.IsAny<Action<ICounterDataSample>>()))
            .Callback<Action<ICounterDataSample>>(cb => pushCounter = cb);

        Mock<IMonitoringChannel<IObjectDataSample>> objectChannel = new();
        objectChannel.Setup(c => c.ChannelInfo).Returns([new ObjectChannelInfo([])]);

        Mock<IDataLink> dataLink = new();
        dataLink.Setup(d => d.CreateMonitoringChannel<ICounterDataSample>(It.IsAny<DataChannelInfo>())).Returns(counterChannel.Object);
        dataLink.Setup(d => d.CreateMonitoringChannel<IObjectDataSample>(It.IsAny<DataChannelInfo>())).Returns(objectChannel.Object);
        dataLink.Setup(d => d.CreateMonitoringChannel<IIODataSample>(It.IsAny<DataChannelInfo>()))
            .Returns(() => new Mock<IMonitoringChannel<IIODataSample>>().Object);

        Mock<IHardwareVersion> hardwareVersion = new();
        hardwareVersion.Setup(h => h.BoardVersion).Returns("A1");
        Mock<IDeviceInfo> deviceInfo = new();
        deviceInfo.Setup(d => d.DeviceType).Returns(DeviceType.VSE150);
        deviceInfo.Setup(d => d.SerialNumber).Returns("12345");
        deviceInfo.Setup(d => d.MacAddress).Returns("aa:bb:cc:dd:ee:ff");
        deviceInfo.Setup(d => d.FirmwareVersion).Returns(new IFM.SDK.VSE.Version(1, 0, 0));
        deviceInfo.Setup(d => d.HardwareVersion).Returns(hardwareVersion.Object);

        Mock<ICommands> commands = new();

        Mock<IVSEDevice> sdk = new();
        sdk.Setup(d => d.Parameters).Returns(parameters.Object);
        sdk.Setup(d => d.DataLink).Returns(dataLink.Object);
        sdk.Setup(d => d.DeviceInfo).Returns(deviceInfo.Object);
        sdk.Setup(d => d.Commands).Returns(commands.Object);
        sdk.Setup(d => d.ConnectionStatus).Returns(connectionStatus);
        sdk.Setup(d => d.SystemMode).Returns(systemMode);

        VseConnection connection = new(sdk.Object, logger: null);
        VseDevice device = new(Address, connection, Mock.Of<ILogger>());

        return (device, sdk, commands, sample => pushCounter!(sample));
    }

    public static void RaiseConnected(Mock<IVSEDevice> sdk)
        => sdk.Raise(d => d.ConnectionStatusChanged += null, sdk.Object, ConnectionStatus.Connected);

    public static void RaiseParametersChanged(Mock<IVSEDevice> sdk)
        => sdk.Raise(d => d.ParametersChanged += null, sdk.Object);

    public static string CounterAddress()
        => "/Device/Counters/Counter01";

    public static string ActiveVariantAddress()
        => "/Device/Variants/ActiveVariant";

    public static void WaitUntil(Func<bool> condition, int timeoutMs = 3000)
    {
        DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.Now < deadline)
            Thread.Sleep(10);
    }
}
