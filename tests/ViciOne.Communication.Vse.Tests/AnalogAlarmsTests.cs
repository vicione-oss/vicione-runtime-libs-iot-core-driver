using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class AnalogAlarmsTests
{
    private static (Mock<IVSEDevice> Device, List<Mock<IMonitoringChannel<IIODataSample>>> Channels, List<Action<IIODataSample>> Receivers) CreateDevice(params IAnalogAlarm[] alarms)
    {
        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.AnalogAlarms).Returns(alarms);

        List<Mock<IMonitoringChannel<IIODataSample>>> channels = [];
        List<Action<IIODataSample>> receivers = [];
        Mock<IDataLink> dataLink = new();
        dataLink.Setup(d => d.CreateMonitoringChannel<IIODataSample>(It.IsAny<DataChannelInfo>()))
            .Returns(() =>
            {
                Mock<IMonitoringChannel<IIODataSample>> channel = new();
                channel.Setup(c => c.Start(It.IsAny<Action<IIODataSample>>()))
                    .Callback<Action<IIODataSample>>(receivers.Add);
                channels.Add(channel);
                return channel.Object;
            });

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);
        device.Setup(d => d.DataLink).Returns(dataLink.Object);

        return (device, channels, receivers);
    }

    private static Mock<IAnalogAlarm> CreateInstance(int id)
    {
        Mock<IAnalogAlarm> instance = new();
        instance.Setup(a => a.Id).Returns(id);
        instance.Setup(a => a.Name).Returns($"Alarm{id:D2}");
        return instance;
    }

    [Fact]
    public void Constructor_CreatesOneChannelPerAlarm()
    {
        Mock<IAnalogAlarm> alarm1 = CreateInstance(1);
        Mock<IAnalogAlarm> alarm2 = CreateInstance(2);
        (Mock<IVSEDevice> device, List<Mock<IMonitoringChannel<IIODataSample>>> channels, _) = CreateDevice(alarm1.Object, alarm2.Object);

        using AnalogAlarms sut = new(device.Object, logger: null);

        Assert.Equal(2, sut.All.Count);
        Assert.Equal(2, channels.Count);
    }

    [Fact]
    public void OnReceived_DispatchesTheSampleToTheMatchingAlarmById()
    {
        Mock<IAnalogAlarm> alarm1 = CreateInstance(1);
        Mock<IAnalogAlarm> alarm2 = CreateInstance(2);
        (Mock<IVSEDevice> device, _, List<Action<IIODataSample>> receivers) = CreateDevice(alarm1.Object, alarm2.Object);
        using AnalogAlarms sut = new(device.Object, logger: null);
        AnalogAlarm alarm2Data = sut.All.Single(a => a.Id == 2);

        Mock<IIODataSample> sample = new();
        sample.Setup(s => s.Id).Returns(2);
        sample.Setup(s => s.GetValue()).Returns(12.5f);
        receivers[1](sample.Object);

        Assert.Equal(12.5f, alarm2Data.Value);
    }

    [Fact]
    public void Dispose_StopsAndDisposesEveryChannel()
    {
        Mock<IAnalogAlarm> alarm1 = CreateInstance(1);
        (Mock<IVSEDevice> device, List<Mock<IMonitoringChannel<IIODataSample>>> channels, _) = CreateDevice(alarm1.Object);
        AnalogAlarms sut = new(device.Object, logger: null);

        sut.Dispose();

        channels.Single().Verify(c => c.Stop(), Times.Once);
        channels.Single().Verify(c => c.Dispose(), Times.Once);
    }
}
