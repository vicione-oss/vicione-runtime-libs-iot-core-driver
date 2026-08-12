using IFM.SDK.VSE;
using IFM.SDK.VSE.DataChannels;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class InputsTests
{
    private static (Mock<IVSEDevice> Device, Mock<IMonitoringChannel<IIODataSample>> Channel, Action<IIODataSample> Receive) CreateDevice(params IInput[] inputs)
    {
        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Inputs).Returns(inputs);

        Mock<IIODataSample> initialSample = new();
        initialSample.Setup(s => s.GetValue()).Returns(0f);
        Mock<IReadDataChannel<IIODataSample>> readChannel = new();
        readChannel.Setup(c => c.Read()).Returns(initialSample.Object);

        Mock<IMonitoringChannel<IIODataSample>> channel = new();
        Action<IIODataSample>? captured = null;
        channel.Setup(c => c.Start(It.IsAny<Action<IIODataSample>>()))
            .Callback<Action<IIODataSample>>(callback => captured = callback);

        Mock<IDataLink> dataLink = new();
        dataLink.Setup(d => d.GetReadDataChannel(It.IsAny<IInput>())).Returns(readChannel.Object);
        dataLink.Setup(d => d.CreateMonitoringChannel<IIODataSample>(It.IsAny<DataChannelInfo>()))
            .Returns(channel.Object);

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);
        device.Setup(d => d.DataLink).Returns(dataLink.Object);

        return (device, channel, sample => captured!(sample));
    }

    private static Mock<IInput> CreateInstance(int id, InputType type)
    {
        Mock<IInput> instance = new();
        instance.Setup(i => i.Id).Returns(id);
        instance.Setup(i => i.Name).Returns($"Input{id:D2}");
        instance.Setup(i => i.Type).Returns(type);
        return instance;
    }

    [Fact]
    public void Constructor_CreatesOneInputPerDeviceInput()
    {
        Mock<IInput> external = CreateInstance(1, InputType.External);
        Mock<IInput> dynamic = CreateInstance(2, InputType.Dynamic);
        (Mock<IVSEDevice> device, _, _) = CreateDevice(external.Object, dynamic.Object);

        using Inputs sut = new(device.Object, logger: null);

        Assert.Equal(2, sut.All.Count);
    }

    [Fact]
    public void Constructor_OnlyExternalInputs_StartsTheMonitoringChannel()
    {
        Mock<IInput> external = CreateInstance(1, InputType.External);
        (Mock<IVSEDevice> device, Mock<IMonitoringChannel<IIODataSample>> channel, _) = CreateDevice(external.Object);

        using Inputs sut = new(device.Object, logger: null);

        channel.Verify(c => c.Start(It.IsAny<Action<IIODataSample>>()), Times.Once);
    }

    [Fact]
    public void Constructor_NoExternalInputs_DoesNotCreateAChannel()
    {
        Mock<IInput> dynamic = CreateInstance(1, InputType.Dynamic);
        (Mock<IVSEDevice> device, _, _) = CreateDevice(dynamic.Object);
        var dataLink = Mock.Get(device.Object.DataLink);

        using Inputs sut = new(device.Object, logger: null);

        dataLink.Verify(d => d.CreateMonitoringChannel<IIODataSample>(It.IsAny<DataChannelInfo>()), Times.Never);
    }

    [Fact]
    public void OnReceived_DispatchesTheSampleToTheMatchingExternalInputById()
    {
        Mock<IInput> external1 = CreateInstance(1, InputType.External);
        Mock<IInput> external2 = CreateInstance(2, InputType.External);
        (Mock<IVSEDevice> device, _, Action<IIODataSample> receive) = CreateDevice(external1.Object, external2.Object);
        using Inputs sut = new(device.Object, logger: null);
        Input input2 = sut.All.Single(i => i.Id == 2);

        Mock<IIODataSample> sample = new();
        sample.Setup(s => s.Id).Returns(2);
        sample.Setup(s => s.GetValue()).Returns(55f);
        receive(sample.Object);

        Assert.Equal(55f, input2.Value);
    }

    [Fact]
    public void Dispose_StopsAndDisposesTheChannel()
    {
        Mock<IInput> external = CreateInstance(1, InputType.External);
        (Mock<IVSEDevice> device, Mock<IMonitoringChannel<IIODataSample>> channel, _) = CreateDevice(external.Object);
        Inputs sut = new(device.Object, logger: null);

        sut.Dispose();

        channel.Verify(c => c.Stop(), Times.Once);
        channel.Verify(c => c.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_NoExternalInputs_DoesNotThrow()
    {
        Mock<IInput> dynamic = CreateInstance(1, InputType.Dynamic);
        (Mock<IVSEDevice> device, _, _) = CreateDevice(dynamic.Object);
        Inputs sut = new(device.Object, logger: null);

        sut.Dispose();
    }
}
