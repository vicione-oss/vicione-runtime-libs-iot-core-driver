using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class DeviceObjectTests
{
    private static Mock<IObject> CreateInstance(int id = 1, string name = "Object01", bool isFastObject = false, int? refTriggerId = null)
    {
        Mock<IObject> instance = new();
        instance.Setup(o => o.Id).Returns(id);
        instance.Setup(o => o.Name).Returns(name);
        instance.Setup(o => o.Unit).Returns("m/s^2");
        instance.Setup(o => o.IsFastObject).Returns(isFastObject);
        instance.Setup(o => o.RefTriggerId).Returns(refTriggerId);
        return instance;
    }

    private static Mock<IObjectDataSample> CreateSample(float value = 1f, float rotSpeed = 0f, float refValue = 0f, ObjectFlags flags = ObjectFlags.NoFlag)
    {
        Mock<IObjectDataSample> sample = new();
        sample.Setup(s => s.GetValues()).Returns([value]);
        sample.Setup(s => s.GetSpeedValues()).Returns([rotSpeed]);
        sample.Setup(s => s.GetReferenceValues()).Returns([refValue]);
        sample.Setup(s => s.GetTimestamps()).Returns([DateTime.Now.Ticks]);
        sample.Setup(s => s.Flags).Returns(flags);
        return sample;
    }

    [Fact]
    public void Constructor_NonFastObject_StartsWithDynamicValuesMonitoringEnabled()
    {
        using DeviceObject sut = new(CreateInstance(isFastObject: false).Object, averagingPeriod: 1000);

        Assert.True(sut.DynamicValuesMonitoring);
    }

    [Fact]
    public void Constructor_FastObject_StartsWithDynamicValuesMonitoringDisabled()
    {
        using DeviceObject sut = new(CreateInstance(isFastObject: true).Object, averagingPeriod: 1000);

        Assert.False(sut.DynamicValuesMonitoring);
    }

    [Fact]
    public void Constructor_NoRefTrigger_RefValueIdIsNull()
    {
        using DeviceObject sut = new(CreateInstance(refTriggerId: null).Object, averagingPeriod: 1000);

        Assert.Null(sut.RefValueId);
    }

    [Fact]
    public void Average_DynamicValuesMonitoringDisabled_IsNaN()
    {
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000)
        {
            DynamicValuesMonitoring = false,
        };

        Assert.True(float.IsNaN(sut.Average));
        Assert.True(float.IsNaN(sut.Maximum));
        Assert.True(float.IsNaN(sut.Minimum));
    }

    [Fact]
    public void Received_DynamicValuesMonitoringEnabled_UpdatesAverageMaximumMinimum()
    {
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);

        sut.Received(CreateSample(value: 2f).Object);
        sut.Received(CreateSample(value: 4f).Object);

        Assert.Equal(3f, sut.Average);
        Assert.Equal(4f, sut.Maximum);
        Assert.Equal(2f, sut.Minimum);
    }

    [Fact]
    public void Received_ChangesTheAverage_RaisesChanged()
    {
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);
        sut.Received(CreateSample(value: 1f).Object);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(CreateSample(value: 5f).Object);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Received_SameValueAgain_DoesNotRaiseChanged()
    {
        // Average/Maximum/Minimum/RefValue/RotSpeed are all unchanged by an identical repeated sample within
        // the same window, so Changed should not fire a second time.
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);
        sut.Received(CreateSample(value: 3f).Object);
        int raised = 0;
        sut.Changed += () => raised++;

        sut.Received(CreateSample(value: 3f).Object);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Changed_ObservedFromWithinTheHandler_AlreadyReflectsTheJustReceivedSample()
    {
        // Average/Maximum/Minimum are computed on read (via SlidingWindowDataProcessor.Calculate), not cached
        // at Received()-time - this locks in that AddSamples() runs before Changed fires, so a subscriber
        // reading Average synchronously inside its handler sees the up-to-date value, not a stale one.
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);
        float observedAverage = float.NaN;
        sut.Changed += () => observedAverage = sut.Average;

        sut.Received(CreateSample(value: 5f).Object);

        Assert.Equal(5f, observedAverage);
    }

    [Fact]
    public void Received_FlagsUnchanged_DoesNotRaiseFlagsChanged()
    {
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);
        sut.Received(CreateSample(flags: ObjectFlags.NoFlag).Object);
        int raised = 0;
        sut.FlagsChanged += () => raised++;

        sut.Received(CreateSample(flags: ObjectFlags.NoFlag).Object);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Received_FirstSampleHasNoFlag_StillRaisesFlagsChanged()
    {
        // _flags defaults to NoFlag (0) before any sample arrives - if the device's very first reported sample
        // is NoFlag too (the common "object starts out healthy" case), that must still count as a real change.
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);
        int raised = 0;
        sut.FlagsChanged += () => raised++;

        sut.Received(CreateSample(flags: ObjectFlags.NoFlag).Object);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void Received_FlagsChanged_RaisesFlagsChangedAndUpdatesState()
    {
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);
        int raised = 0;
        sut.FlagsChanged += () => raised++;

        sut.Received(CreateSample(flags: ObjectFlags.InternalError).Object);

        Assert.Equal(1, raised);
        Assert.Equal("error", sut.State);
    }

    [Theory]
    [InlineData(ObjectFlags.IsWarning)]
    [InlineData(ObjectFlags.IsDamage)]
    public void State_WarningOrDamageFlags_AreNotReportedAsError(ObjectFlags flags)
    {
        // ObjectFlagsExtensions.IsError() explicitly excludes IsWarning/IsDamage - they are their own
        // reportable conditions (see Flags), not an "error" state.
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);

        sut.Received(CreateSample(flags: flags).Object);

        Assert.Equal("ok", sut.State);
    }

    [Fact]
    public void State_DisabledByVariant_TakesPrecedenceOverErrorFlags()
    {
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000)
        {
            DisabledByVariant = true,
        };
        sut.Received(CreateSample(flags: ObjectFlags.InternalError).Object);

        Assert.Equal("inactive-by-variant", sut.State);
    }

    [Fact]
    public void DynamicValuesMonitoring_SetToSameValue_DoesNotRaiseMonitoringStateChanged()
    {
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);
        int raised = 0;
        sut.MonitoringStateChanged += _ => raised++;

        sut.DynamicValuesMonitoring = true;

        Assert.Equal(0, raised);
    }

    [Fact]
    public void DynamicValuesMonitoring_Toggled_RaisesMonitoringStateChanged()
    {
        using DeviceObject sut = new(CreateInstance().Object, averagingPeriod: 1000);
        bool? raisedWith = null;
        sut.MonitoringStateChanged += enabled => raisedWith = enabled;

        sut.DynamicValuesMonitoring = false;

        Assert.False(raisedWith);
    }
}
