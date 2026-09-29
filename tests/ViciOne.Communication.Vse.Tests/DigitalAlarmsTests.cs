using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

// Only the empty-list path is covered here: DigitalAlarms.ctor calls IDigitalAlarm.AsChannelInfo() per alarm, and
// that extension method downcasts internally to the SDK's own concrete alarm type - it throws InvalidCastException
// for anything else implementing IDigitalAlarm, including Moq's proxies. Confirmed against real hardware that a
// hand-built OutputChannelInfo cannot stand in for it (see the comment in DigitalAlarms.cs), so the per-alarm
// channel wiring here is only verified against real hardware, not unit tests. DigitalAlarmTests.cs still covers
// the DigitalAlarm domain object (Value computation, dedup) in full, since that never touches AsChannelInfo().
public class DigitalAlarmsTests
{
    private static Mock<IVSEDevice> CreateDevice()
    {
        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.DigitalAlarms).Returns([]);

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);

        return device;
    }

    [Fact]
    public void Constructor_NoAlarms_DoesNotCreateAnyChannels()
    {
        Mock<IVSEDevice> device = CreateDevice();

        using DigitalAlarms sut = new(device.Object, logger: null);

        Assert.Empty(sut.All);
        device.Verify(d => d.DataLink, Times.Never);
    }

    [Fact]
    public void Dispose_NoAlarms_DoesNotThrow()
    {
        Mock<IVSEDevice> device = CreateDevice();
        DigitalAlarms sut = new(device.Object, logger: null);

        sut.Dispose();
    }
}
