using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;
using ViciOne.Communication.Vse.EventManager;

namespace ViciOne.Communication.Vse.Tests;

public class DeviceObjectsTests
{
    private static (Mock<IVSEDevice> Device, Mock<IMonitoringChannel<IObjectDataSample>> Channel, Action<IObjectDataSample> Receive, DeviceEventManager EventManager) CreateDevice(params IObject[] objects)
    {
        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Objects).Returns(objects);

        Mock<IMonitoringChannel<IObjectDataSample>> channel = new();
        Action<IObjectDataSample>? captured = null;
        channel.Setup(c => c.Start(It.IsAny<Action<IObjectDataSample>>()))
            .Callback<Action<IObjectDataSample>>(callback => captured = callback);
        channel.Setup(c => c.ChannelInfo).Returns([new ObjectChannelInfo([])]);

        Mock<IDataLink> dataLink = new();
        dataLink.Setup(d => d.CreateMonitoringChannel<IObjectDataSample>(It.IsAny<DataChannelInfo>()))
            .Returns(channel.Object);

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);
        device.Setup(d => d.DataLink).Returns(dataLink.Object);
        device.Setup(d => d.ConnectionStatus).Returns(ConnectionStatus.Connected);

        DeviceEventManager eventManager = new(device.Object, loggerFactory: null);

        return (device, channel, sample => captured!(sample), eventManager);
    }

    private static Mock<IObject> CreateInstance(int id)
    {
        Mock<IObject> instance = new();
        instance.Setup(o => o.Id).Returns(id);
        instance.Setup(o => o.Name).Returns($"Object{id:D2}");
        return instance;
    }

    [Fact]
    public void Constructor_CreatesOneDeviceObjectPerDeviceObject()
    {
        Mock<IObject> object1 = CreateInstance(1);
        Mock<IObject> object2 = CreateInstance(2);
        (Mock<IVSEDevice> device, _, _, DeviceEventManager eventManager) = CreateDevice(object1.Object, object2.Object);
        using (eventManager)
        {
            using DeviceObjects sut = new(device.Object, eventManager, logger: null);

            Assert.Equal(2, sut.All.Count);
        }
    }

    [Fact]
    public void Constructor_DefaultsAveragingPeriodToTheDocumentedMinimum()
    {
        (Mock<IVSEDevice> device, _, _, DeviceEventManager eventManager) = CreateDevice();
        using (eventManager)
        {
            using DeviceObjects sut = new(device.Object, eventManager, logger: null);

            Assert.Equal(DeviceObjects.AveragingPeriodMin, sut.AveragingPeriod);
        }
    }

    [Theory]
    [InlineData(DeviceObjects.AveragingPeriodMin - 1u)]
    [InlineData(DeviceObjects.AveragingPeriodMax + 1u)]
    public void AveragingPeriod_OutOfRange_Throws(uint value)
    {
        (Mock<IVSEDevice> device, _, _, DeviceEventManager eventManager) = CreateDevice();
        using (eventManager)
        {
            using DeviceObjects sut = new(device.Object, eventManager, logger: null);

            Assert.Throws<ArgumentOutOfRangeException>(() => sut.AveragingPeriod = value);
        }
    }

    [Fact]
    public void AveragingPeriod_Set_PropagatesToAllDeviceObjects()
    {
        Mock<IObject> object1 = CreateInstance(1);
        (Mock<IVSEDevice> device, _, _, DeviceEventManager eventManager) = CreateDevice(object1.Object);
        using (eventManager)
        {
            using DeviceObjects sut = new(device.Object, eventManager, logger: null);

            sut.AveragingPeriod = 2000;

            Assert.Equal(2000u, sut.All.Single().AveragingPeriod);
        }
    }

    [Fact]
    public void OnReceived_DispatchesTheSampleToTheMatchingDynamicallyMonitoredObjectById()
    {
        Mock<IObject> object1 = CreateInstance(1);
        Mock<IObject> object2 = CreateInstance(2);
        (Mock<IVSEDevice> device, _, Action<IObjectDataSample> receive, DeviceEventManager eventManager) = CreateDevice(object1.Object, object2.Object);
        using (eventManager)
        {
            using DeviceObjects sut = new(device.Object, eventManager, logger: null);
            DeviceObject object2Data = sut.All.Single(o => o.Id == 2);

            Mock<IObjectDataSample> sample = new();
            sample.Setup(s => s.Id).Returns(2);
            sample.Setup(s => s.GetValues()).Returns([5f]);
            sample.Setup(s => s.GetSpeedValues()).Returns([0f]);
            sample.Setup(s => s.GetReferenceValues()).Returns([0f]);
            sample.Setup(s => s.GetTimestamps()).Returns([DateTime.Now.Ticks]);
            receive(sample.Object);

            Assert.Equal(5f, object2Data.Average);
        }
    }

    [Fact]
    public void VariantChanged_DisablesObjectsNotInTheActiveVariant()
    {
        Mock<IObject> object1 = CreateInstance(1);
        Mock<IObject> object2 = CreateInstance(2);
        (Mock<IVSEDevice> device, _, _, DeviceEventManager eventManager) = CreateDevice(object1.Object, object2.Object);
        using (eventManager)
        {
            using DeviceObjects sut = new(device.Object, eventManager, logger: null);
            DeviceObject object1Data = sut.All.Single(o => o.Id == 1);
            DeviceObject object2Data = sut.All.Single(o => o.Id == 2);

            Mock<IVariant> variant = new();
            variant.Setup(v => v.ActivatedObjects).Returns([1]);
            device.Raise(d => d.VariantChanged += null, device.Object, variant.Object);

            bool observed = SpinWait.SpinUntil(() => object2Data.DisabledByVariant, TimeSpan.FromSeconds(2));

            Assert.True(observed);
            Assert.False(object1Data.DisabledByVariant);
            Assert.True(object2Data.DisabledByVariant);
        }
    }
}
