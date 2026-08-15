using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class DigitalAlarmTests
{
    private static Mock<IDigitalAlarm> CreateInstance(int id = 1, bool normallyClosed = false, AlarmType type = AlarmType.Custom)
    {
        Mock<IDigitalAlarm> instance = new();
        instance.Setup(a => a.Id).Returns(id);
        instance.Setup(a => a.Name).Returns($"Alarm{id:D2}");
        instance.Setup(a => a.NormallyClosed).Returns(normallyClosed);
        instance.Setup(a => a.Type).Returns(type);
        return instance;
    }

    private static Mock<IIODataSample> CreateSample(float value)
    {
        Mock<IIODataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(value);
        return sample;
    }

    [Theory]
    [InlineData(false, 1.0f, true)]
    [InlineData(false, 0.0f, false)]
    [InlineData(true, 1.0f, false)]
    [InlineData(true, 0.0f, true)]
    public void Received_ComputesValueFromNormallyClosedAndRawSample(bool normallyClosed, float rawValue, bool expected)
    {
        DigitalAlarm sut = new(CreateInstance(normallyClosed: normallyClosed).Object);

        sut.Received(CreateSample(rawValue).Object);

        Assert.Equal(expected, sut.Value);
    }

    [Fact]
    public void Received_ValueUnchanged_DoesNotRaiseChanged()
    {
        DigitalAlarm sut = new(CreateInstance(normallyClosed: false).Object);
        sut.Received(CreateSample(0.0f).Object);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(CreateSample(0.0f).Object);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Received_ValueChanged_RaisesChanged()
    {
        DigitalAlarm sut = new(CreateInstance(normallyClosed: false).Object);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(CreateSample(1.0f).Object);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Received_FirstSampleMatchesDotNetDefault_StillRaisesChanged()
    {
        // Value defaults to false before any sample arrives - if the device's very first reported sample
        // computes to "inactive" too, that must still count as a real change.
        DigitalAlarm sut = new(CreateInstance(normallyClosed: false).Object);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(CreateSample(0.0f).Object);

        Assert.Equal(1, raised);
    }
}
