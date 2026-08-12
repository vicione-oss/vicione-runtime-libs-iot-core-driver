using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;
using ViciOne.Communication.Vse.EventManager;

namespace ViciOne.Communication.Vse.Tests;

public class VariantsTests
{
    private static Mock<IVariant> CreateInstance(int id)
    {
        Mock<IVariant> instance = new();
        instance.Setup(v => v.Id).Returns(id);
        instance.Setup(v => v.ActivatedObjects).Returns([]);
        return instance;
    }

    private static (Mock<IVSEDevice> Device, Mock<ICommands> Commands, DeviceEventManager EventManager) CreateDevice(params IVariant[] variants)
    {
        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Variants).Returns(variants);

        Mock<ICommands> commands = new();
        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);
        device.Setup(d => d.Commands).Returns(commands.Object);

        DeviceEventManager eventManager = new(device.Object, loggerFactory: null);

        return (device, commands, eventManager);
    }

    [Fact]
    public void Constructor_ExposesAllVariants()
    {
        Mock<IVariant> variant1 = CreateInstance(1);
        Mock<IVariant> variant2 = CreateInstance(2);
        (Mock<IVSEDevice> device, _, DeviceEventManager eventManager) = CreateDevice(variant1.Object, variant2.Object);

        using Variants sut = new(device.Object, eventManager);

        Assert.Equal(2, sut.All.Count);
        Assert.Contains(sut.All, v => v.Id == 1);
        Assert.Contains(sut.All, v => v.Id == 2);
    }

    [Fact]
    public void ActiveVariantId_Get_ReadsThroughToDevice()
    {
        Mock<IVariant> activeVariant = CreateInstance(3);
        (Mock<IVSEDevice> device, _, DeviceEventManager eventManager) = CreateDevice();
        device.Setup(d => d.Variant).Returns(activeVariant.Object);
        using Variants sut = new(device.Object, eventManager);

        Assert.Equal(3, sut.ActiveVariantId);
    }

    [Fact]
    public void ActiveVariantId_Set_CallsSetVariant()
    {
        (Mock<IVSEDevice> device, Mock<ICommands> commands, DeviceEventManager eventManager) = CreateDevice();
        using Variants sut = new(device.Object, eventManager);

        sut.ActiveVariantId = 4;

        commands.Verify(c => c.SetVariant(4), Times.Once);
    }

    [Fact]
    public void VariantChanged_RaisesActiveVariantChangedWithMatchingInstanceFromAll()
    {
        Mock<IVariant> variant1 = CreateInstance(1);
        Mock<IVariant> variant2 = CreateInstance(2);
        (Mock<IVSEDevice> device, _, DeviceEventManager eventManager) = CreateDevice(variant1.Object, variant2.Object);
        using Variants sut = new(device.Object, eventManager);
        Variant? raisedVariant = null;
        sut.ActiveVariantChanged += v => raisedVariant = v;

        device.Raise(d => d.VariantChanged += null, device.Object, variant2.Object);

        WaitUntil(() => raisedVariant is not null);
        Assert.NotNull(raisedVariant);
        Assert.Equal(2, raisedVariant.Id);
        Assert.Same(sut.All.Single(v => v.Id == 2), raisedVariant);
    }

    private static void WaitUntil(Func<bool> condition, int timeoutMs = 2000)
    {
        DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
        while (!condition() && DateTime.Now < deadline)
            Thread.Sleep(10);
    }
}
