using IFM.SDK.VSE;
using IFM.SDK.VSE.DataChannels;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class InputTests
{
    private static Mock<IInput> CreateInstance(int id = 1, string name = "Input01", InputType type = InputType.External)
    {
        Mock<IInput> instance = new();
        instance.Setup(i => i.Id).Returns(id);
        instance.Setup(i => i.Name).Returns(name);
        instance.Setup(i => i.Unit).Returns("rpm");
        instance.Setup(i => i.Type).Returns(type);
        return instance;
    }

    private static Mock<IDataLink> CreateDataLink(float initialValue = 0f)
    {
        Mock<IIODataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(initialValue);

        Mock<IReadDataChannel<IIODataSample>> readChannel = new();
        readChannel.Setup(c => c.Read()).Returns(sample.Object);

        Mock<IDataLink> dataLink = new();
        dataLink.Setup(d => d.GetReadDataChannel(It.IsAny<IInput>())).Returns(readChannel.Object);

        return dataLink;
    }

    [Fact]
    public void Constructor_ExternalInput_ReadsTheInitialValue()
    {
        Mock<IDataLink> dataLink = CreateDataLink(initialValue: 42f);

        Input sut = new(CreateInstance(type: InputType.External).Object, dataLink.Object);

        Assert.True(sut.HasValue);
        Assert.True(sut.DynamicValueMonitoring);
        Assert.Equal(42f, sut.Value);
    }

    [Theory]
    [InlineData(InputType.Dynamic)]
    [InlineData(InputType.Analog)]
    [InlineData(InputType.Digital)]
    public void Constructor_NonExternalInput_HasNoValue(InputType type)
    {
        Mock<IDataLink> dataLink = CreateDataLink();

        Input sut = new(CreateInstance(type: type).Object, dataLink.Object);

        Assert.False(sut.HasValue);
        Assert.False(sut.DynamicValueMonitoring);
        Assert.True(float.IsNaN(sut.Value));
        dataLink.Verify(d => d.GetReadDataChannel(It.IsAny<IInput>()), Times.Never);
    }

    [Fact]
    public void Value_SetOnExternalInput_WritesThroughTheDataLink()
    {
        Mock<IDataLink> dataLink = CreateDataLink();
        Mock<IWriteDataChannel> writeChannel = new();
        dataLink.Setup(d => d.GetWriteDataChannel(WriteDataType.Rpm, 1)).Returns(writeChannel.Object);
        Input sut = new(CreateInstance(id: 1, type: InputType.External).Object, dataLink.Object)
        {
            Value = 7f
        };

        writeChannel.Verify(c => c.Write(7f), Times.Once);
    }

    [Fact]
    public void Value_SetOnNonExternalInput_Throws()
    {
        Input sut = new(CreateInstance(type: InputType.Dynamic).Object, CreateDataLink().Object);

        Assert.Throws<InvalidOperationException>(() => sut.Value = 1f);
    }

    [Fact]
    public void Received_UpdatesValueAndRaisesChanged()
    {
        Input sut = new(CreateInstance(type: InputType.External).Object, CreateDataLink().Object);
        Mock<IIODataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(99f);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(sample.Object);

        Assert.Equal(1, raised);
        Assert.Equal(99f, sut.Value);
    }

    [Fact]
    public void Received_SameValueAgain_DoesNotRaiseChanged()
    {
        Input sut = new(CreateInstance(type: InputType.External).Object, CreateDataLink(initialValue: 5f).Object);
        Mock<IIODataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(5f);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(sample.Object);

        Assert.Equal(0, raised);
    }
}
