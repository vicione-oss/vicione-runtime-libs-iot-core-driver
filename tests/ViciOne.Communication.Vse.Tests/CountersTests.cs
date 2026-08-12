using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class CountersTests
{
    private static (Mock<IVSEDevice> Device, Mock<IMonitoringChannel<ICounterDataSample>> Channel, Action<ICounterDataSample> Receive) CreateDevice(params ICounter[] counters)
    {
        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Counters).Returns(counters);

        Mock<IMonitoringChannel<ICounterDataSample>> channel = new();
        Action<ICounterDataSample>? captured = null;
        channel.Setup(c => c.Start(It.IsAny<Action<ICounterDataSample>>()))
            .Callback<Action<ICounterDataSample>>(callback => captured = callback);

        Mock<IDataLink> dataLink = new();
        dataLink.Setup(d => d.CreateMonitoringChannel<ICounterDataSample>(It.IsAny<DataChannelInfo>()))
            .Returns(channel.Object);

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);
        device.Setup(d => d.DataLink).Returns(dataLink.Object);

        return (device, channel, sample => captured!(sample));
    }

    private static Mock<ICounter> CreateCounter(int id)
    {
        Mock<ICounter> counter = new();
        counter.Setup(c => c.Id).Returns(id);
        counter.Setup(c => c.Name).Returns($"Counter{id:D2}");
        return counter;
    }

    [Fact]
    public void Constructor_CreatesOneCounterPerDeviceCounter()
    {
        Mock<ICounter> counter1 = CreateCounter(1);
        Mock<ICounter> counter2 = CreateCounter(2);
        (Mock<IVSEDevice> device, _, _) = CreateDevice(counter1.Object, counter2.Object);

        using Counters sut = new(device.Object, logger: null);

        Assert.Equal(2, sut.All.Count);
        Assert.Contains(sut.All, c => c.Id == 1 && c.Name == "Counter01");
        Assert.Contains(sut.All, c => c.Id == 2 && c.Name == "Counter02");
    }

    [Fact]
    public void Constructor_StartsTheMonitoringChannel()
    {
        (Mock<IVSEDevice> device, Mock<IMonitoringChannel<ICounterDataSample>> channel, _) = CreateDevice(CreateCounter(1).Object);

        using Counters sut = new(device.Object, logger: null);

        channel.Verify(c => c.Start(It.IsAny<Action<ICounterDataSample>>()), Times.Once);
    }

    [Fact]
    public void Constructor_NoCounters_DoesNotCreateAChannel()
    {
        // DataChannelInfoHelpers.AsChannelInfo() throws on an empty collection, so Counters must
        // skip channel creation entirely rather than pass one through - and there's nothing to
        // monitor anyway.
        (Mock<IVSEDevice> device, _, _) = CreateDevice();

        using Counters sut = new(device.Object, logger: null);

        Assert.Empty(sut.All);
        device.Verify(d => d.DataLink, Times.Never);
    }

    [Fact]
    public void OnReceived_DispatchesTheSampleToTheMatchingCounterById()
    {
        Mock<ICounter> counter1 = CreateCounter(1);
        Mock<ICounter> counter2 = CreateCounter(2);
        (Mock<IVSEDevice> device, _, Action<ICounterDataSample> receive) = CreateDevice(counter1.Object, counter2.Object);
        using Counters sut = new(device.Object, logger: null);
        Counter counter2Data = sut.All.Single(c => c.Id == 2);
        int changed = 0;
        counter2Data.Changed += () => changed++;

        Mock<ICounterDataSample> sample = new();
        sample.Setup(s => s.Id).Returns(2);
        sample.Setup(s => s.GetValue()).Returns(99f);
        receive(sample.Object);

        Assert.Equal(1, changed);
        Assert.Equal(99f, counter2Data.Value);
    }

    [Fact]
    public void Dispose_StopsAndDisposesTheChannel()
    {
        (Mock<IVSEDevice> device, Mock<IMonitoringChannel<ICounterDataSample>> channel, _) = CreateDevice(CreateCounter(1).Object);
        Counters sut = new(device.Object, logger: null);

        sut.Dispose();

        channel.Verify(c => c.Stop(), Times.Once);
        channel.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_NoCounters_DoesNotThrow()
    {
        (Mock<IVSEDevice> device, _, _) = CreateDevice();
        Counters sut = new(device.Object, logger: null);

        sut.Dispose();
    }
}
