using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class AnalogAlarmTests
{
    private static Mock<IAnalogAlarm> CreateInstance(int id = 1)
    {
        Mock<IAnalogAlarm> instance = new();
        instance.Setup(a => a.Id).Returns(id);
        instance.Setup(a => a.Name).Returns($"Alarm{id:D2}");
        instance.Setup(a => a.Unit).Returns("V");
        return instance;
    }

    [Fact]
    public void Received_UpdatesValueAndRaisesChanged()
    {
        AnalogAlarm sut = new(CreateInstance().Object);
        Mock<IIODataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(3.5f);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(sample.Object);

        Assert.Equal(1, raised);
        Assert.Equal(3.5f, sut.Value);
    }

    [Fact]
    public void Received_SameValueAgain_DoesNotRaiseChanged()
    {
        AnalogAlarm sut = new(CreateInstance().Object);
        Mock<IIODataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(1f);
        sut.Received(sample.Object);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(sample.Object);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Received_FirstSampleMatchesDotNetDefault_StillRaisesChanged()
    {
        // Value defaults to 0f before any sample arrives - if the device's very first reported sample happens
        // to be exactly 0f too, that must still count as a real change.
        AnalogAlarm sut = new(CreateInstance().Object);
        Mock<IIODataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(0f);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(sample.Object);

        Assert.Equal(1, raised);
    }
}
