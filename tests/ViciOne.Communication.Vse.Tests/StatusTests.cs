using IFM.SDK.VSE;
using Moq;
using ViciOne.Communication.Vse.Data;
using ViciOne.Communication.Vse.EventManager;

namespace ViciOne.Communication.Vse.Tests;

public class StatusTests
{
    private static (Mock<IVSEDevice> Device, Mock<ICommands> Commands, DeviceEventManager EventManager) CreateDevice(ConnectionStatus connectionStatus = ConnectionStatus.Connected, SystemMode systemMode = SystemMode.Supervise)
    {
        Mock<ICommands> commands = new();
        Mock<IVSEDevice> device = new();
        device.Setup(d => d.ConnectionStatus).Returns(connectionStatus);
        device.Setup(d => d.SystemMode).Returns(systemMode);
        device.Setup(d => d.Commands).Returns(commands.Object);

        DeviceEventManager eventManager = new(device.Object, loggerFactory: null);

        return (device, commands, eventManager);
    }

    [Fact]
    public void Constructor_ExposesConnectionStatusAndSystemMode()
    {
        (Mock<IVSEDevice> device, _, DeviceEventManager eventManager) = CreateDevice(ConnectionStatus.Connected, SystemMode.Measure);
        using Status sut = new(device.Object, eventManager);

        Assert.Equal(ConnectionStatus.Connected, sut.ConnectionStatus);
        Assert.Equal(SystemMode.Measure, sut.SystemMode);
    }

    [Fact]
    public void SystemMode_SetWhileDisconnected_Throws()
    {
        (Mock<IVSEDevice> device, _, DeviceEventManager eventManager) = CreateDevice(ConnectionStatus.Disconnected);
        using Status sut = new(device.Object, eventManager);

        Assert.Throws<InvalidOperationException>(() => sut.SystemMode = SystemMode.Measure);
    }

    [Fact]
    public void SystemMode_SetWhileConnected_CallsSetSystemMode()
    {
        (Mock<IVSEDevice> device, Mock<ICommands> commands, DeviceEventManager eventManager) = CreateDevice(ConnectionStatus.Connected);
        using Status sut = new(device.Object, eventManager);

        sut.SystemMode = SystemMode.Measure;

        commands.Verify(c => c.SetSystemMode(SystemMode.Measure), Times.Once);
    }

    [Fact]
    public void ConnectionStatusChanged_RaisesChanged()
    {
        (Mock<IVSEDevice> device, _, DeviceEventManager eventManager) = CreateDevice();
        using Status sut = new(device.Object, eventManager);
        int raised = 0;
        sut.Changed += () => raised++;

        device.Raise(d => d.ConnectionStatusChanged += null, device.Object, ConnectionStatus.Disconnected);

        WaitUntil(() => raised > 0);
        Assert.True(raised > 0);
    }

    [Fact]
    public void SystemModeChanged_RaisesChanged()
    {
        (Mock<IVSEDevice> device, _, DeviceEventManager eventManager) = CreateDevice();
        using Status sut = new(device.Object, eventManager);
        int raised = 0;
        sut.Changed += () => raised++;

        device.Raise(d => d.SystemModeChanged += null, device.Object, SystemMode.Measure, Origin.Internal);

        WaitUntil(() => raised > 0);
        Assert.True(raised > 0);
    }

    private static void WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.Now < deadline)
            Thread.Sleep(10);
    }
}
