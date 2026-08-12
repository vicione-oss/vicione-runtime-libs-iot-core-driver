using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class CounterTests
{
    private static Mock<ICounter> CreateCounter(int id = 1, string name = "Counter01", string unit = "h", int limit = 100, int? inputId = null, CounterType type = CounterType.VSEUptime)
    {
        Mock<ICounter> counter = new();
        counter.Setup(c => c.Id).Returns(id);
        counter.Setup(c => c.Name).Returns(name);
        counter.Setup(c => c.Unit).Returns(unit);
        counter.Setup(c => c.Limit).Returns(limit);
        counter.Setup(c => c.InputId).Returns(inputId);
        counter.Setup(c => c.Type).Returns(type);
        return counter;
    }

    [Fact]
    public void Constructor_ExposesUnderlyingCounterProperties()
    {
        Mock<ICounter> counter = CreateCounter(id: 3, name: "Totalisator01", unit: "kg", limit: 500, inputId: 7, type: CounterType.Totalisator);

        Counter sut = new(counter.Object);

        Assert.Equal(3, sut.Id);
        Assert.Equal("Totalisator01", sut.Name);
        Assert.Equal("kg", sut.Unit);
        Assert.Equal(500, sut.Limit);
        Assert.Equal(7, sut.InputId);
        Assert.Equal("Totalisator", sut.Type);
        Assert.Equal(0, sut.Value);
        Assert.Equal(string.Empty, sut.State);
        Assert.Null(sut.Timestamp);
        Assert.False(sut.IsValid);
    }

    [Fact]
    public void Received_UpdatesValueStateTimestampAndValidity()
    {
        Counter sut = new(CreateCounter().Object);
        Mock<ICounterDataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(42f);
        sample.Setup(s => s.Elapsed).Returns(true);
        sample.Setup(s => s.GetTimestamp()).Returns(637_000_000_000L);
        sample.Setup(s => s.IsValid()).Returns(true);

        sut.Received(sample.Object);

        Assert.Equal(42f, sut.Value);
        Assert.Equal("Elapsed", sut.State);
        Assert.Equal(new DateTime(637_000_000_000L), sut.Timestamp);
        Assert.True(sut.IsValid);
    }

    [Fact]
    public void Received_NotElapsed_ClearsState()
    {
        Counter sut = new(CreateCounter().Object);
        Mock<ICounterDataSample> sample = new();
        sample.Setup(s => s.Elapsed).Returns(false);

        sut.Received(sample.Object);

        Assert.Equal(string.Empty, sut.State);
    }

    [Fact]
    public void Received_ValueChanged_RaisesChanged()
    {
        Counter sut = new(CreateCounter().Object);
        Mock<ICounterDataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(1f);

        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(sample.Object);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Received_ValueAndStateUnchanged_DoesNotRaiseChanged()
    {
        Counter sut = new(CreateCounter().Object);
        Mock<ICounterDataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(1f);
        sample.Setup(s => s.Elapsed).Returns(false);
        sut.Received(sample.Object);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(sample.Object);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Received_FirstSampleMatchesDotNetDefaults_StillRaisesChanged()
    {
        // Value defaults to 0f and State to string.Empty before any sample arrives - if the device's very first
        // reported sample happens to be exactly 0f/not-elapsed too, that must still count as a real change,
        // since there was no prior actual reading to compare against.
        Counter sut = new(CreateCounter().Object);
        Mock<ICounterDataSample> sample = new();
        sample.Setup(s => s.GetValue()).Returns(0f);
        sample.Setup(s => s.Elapsed).Returns(false);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(sample.Object);

        Assert.Equal(1, raised);
    }
}
