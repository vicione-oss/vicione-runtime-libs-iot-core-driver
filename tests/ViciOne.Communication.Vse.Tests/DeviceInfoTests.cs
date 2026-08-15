using IFM.SDK.VSE;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class DeviceInfoTests
{
    private static Mock<IVSEDevice> CreateDevice(DeviceType deviceType = DeviceType.VSE150, string serialNumber = "12345")
    {
        Mock<IHardwareVersion> hardwareVersion = new();
        hardwareVersion.Setup(h => h.BoardVersion).Returns("A1");
        hardwareVersion.Setup(h => h.ProductionCode).Returns("PC01");

        Mock<IDeviceInfo> deviceInfo = new();
        deviceInfo.Setup(d => d.DeviceType).Returns(deviceType);
        deviceInfo.Setup(d => d.SerialNumber).Returns(serialNumber);
        deviceInfo.Setup(d => d.DeviceName).Returns("MyVSE");
        deviceInfo.Setup(d => d.MacAddress).Returns("00:11:22:33:44:55");
        deviceInfo.Setup(d => d.FirmwareVersion).Returns(new IFM.SDK.VSE.Version(1, 2, 3));
        deviceInfo.Setup(d => d.HardwareVersion).Returns(hardwareVersion.Object);

        Mock<ICommands> commands = new();
        commands.Setup(c => c.GetDateTime()).Returns(new DateTime(2026, 7, 30, 12, 0, 0));

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.DeviceInfo).Returns(deviceInfo.Object);
        device.Setup(d => d.Commands).Returns(commands.Object);

        return device;
    }

    [Fact]
    public void Constructor_ExposesUnderlyingDeviceInfoProperties()
    {
        DeviceInfo sut = new(CreateDevice(deviceType: DeviceType.VSE150, serialNumber: "12345").Object);

        Assert.Equal("VSE150", sut.Type);
        Assert.Equal("VSE150-12345", sut.Name);
        Assert.Equal("MyVSE", sut.DeviceName);
        Assert.Equal("12345", sut.SerialNumber);
        Assert.Equal("00:11:22:33:44:55", sut.MacAddress);
        Assert.Equal("A1", sut.BoardVersion);
        Assert.Equal("PC01", sut.ProductionCode);
        Assert.Equal(1, sut.FirmwareVersion.Major);
        Assert.Equal(2, sut.FirmwareVersion.Minor);
        Assert.Equal(3, sut.FirmwareVersion.Patch);
    }

    [Fact]
    public void GetDeviceDateTime_ReadsThroughToCommands()
    {
        DeviceInfo sut = new(CreateDevice().Object);

        Assert.Equal(new DateTime(2026, 7, 30, 12, 0, 0), sut.GetDeviceDateTime());
    }
}
