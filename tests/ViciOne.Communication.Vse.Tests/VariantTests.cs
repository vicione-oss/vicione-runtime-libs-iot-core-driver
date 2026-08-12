using IFM.SDK.VSE.Parameters;
using Moq;
using ViciOne.Communication.Vse.Data;

namespace ViciOne.Communication.Vse.Tests;

public class VariantTests
{
    [Fact]
    public void Constructor_ExposesUnderlyingVariantProperties()
    {
        Mock<IVariant> instance = new();
        instance.Setup(v => v.Id).Returns(2);
        instance.Setup(v => v.ActivatedObjects).Returns([1, 3, 5]);

        Variant sut = new(instance.Object);

        Assert.Equal(2, sut.Id);
        Assert.Equal([1, 3, 5], sut.ActivatedObjects);
    }
}
