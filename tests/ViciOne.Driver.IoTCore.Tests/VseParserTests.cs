using ifmIoTCore;
using ifmIoTCore.Common.Variant;
using ifmIoTCore.Elements;
using Microsoft.Extensions.Logging;
using Moq;
using ViciOne.Driver.IoTCore.Contracts.Constants;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace ViciOne.Driver.IoTCore.Tests;

public class VseParserTests
{
    private delegate IBaseElement CallbackDelegate(string address);

    public class AddActiveVariant
    {
        [Fact]
        public void SetsCorrectDeviceTreeVseVariants()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantObject()
                        {
                            ["Name"] = new VariantValue("ActiveVariant"),
                        },
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Variants")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Variants/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddActiveVariant(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.Equal("Variants", deviceTree.Children.First().Name);
            Assert.Equal("test/Variants", deviceTree.Children.First().Id);
            Assert.Single(deviceTree.Children.First().Children);
            Assert.Equal("ActiveVariant", deviceTree.Children.First().Children.First().Name);
            Assert.Equal(DataType.IntegerT, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First()).DataType);
            Assert.Equal("test/Variants/ActiveVariant", deviceTree.Children.First().Children.First().Id);
            Assert.True(((DeviceTreeProcessData)deviceTree.Children.First().Children.First()).IsWriteable);
        }
    }

    public class AddAlarms
    {
        [Fact]
        public void SetsCorrectAlarms()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantObject()
                        {
                            ["Name"] = new VariantValue("TestAlarm"),
                            ["Id"] = new VariantValue(1),
                            ["Type"] = new VariantValue("HourlyAlarm"),
                        },
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddAlarms(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Alarms", deviceTree.Children.First().Name);
            Assert.Equal("test/Alarms", deviceTree.Children.First().Id);
            Assert.Single(deviceTree.Children.First().Children);
            Assert.IsType<DeviceTreeVseAlarm>(deviceTree.Children.First().Children.First());
            Assert.Equal("TestAlarm", ((DeviceTreeVseAlarm)deviceTree.Children.First().Children.First()).Alias);
            Assert.Equal("dataElement", deviceTree.Children.First().Children.First().Name);
            Assert.Equal($"test/Alarms/dataElement{VseParser.IdSeparatorNameAlias}TestAlarm", deviceTree.Children.First().Children.First().Id);
            Assert.Equal("localhost", ((DeviceTreeVseAlarm)deviceTree.Children.First().Children.First()).Path);
            Assert.Equal("HourlyAlarm", ((DeviceTreeVseAlarm)deviceTree.Children.First().Children.First()).Type);
            Assert.Equal(2, deviceTree.Children.First().Children.First().Children.Count);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children.First());
            Assert.Equal("Value", deviceTree.Children.First().Children.First().Children.First().Name);
            Assert.Equal(DataType.BooleanT, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children.First()).DataType);
            Assert.Equal($"test/Alarms/dataElement{VseParser.IdSeparatorNameAlias}TestAlarm/Value", deviceTree.Children.First().Children.First().Children.First().Id);
            Assert.IsType<DeviceTreeConstantData>(deviceTree.Children.First().Children.First().Children[1]);
            Assert.Equal("Name", deviceTree.Children.First().Children.First().Children[1].Name);
            Assert.Equal(DataType.StringT, ((DeviceTreeConstantData)deviceTree.Children.First().Children.First().Children[1]).DataType);
            Assert.Equal($"test/Alarms/dataElement{VseParser.IdSeparatorNameAlias}TestAlarm/Name", deviceTree.Children.First().Children.First().Children[1].Id);
            Assert.Equal("TestAlarm", ((DeviceTreeConstantData)deviceTree.Children.First().Children.First().Children[1]).Value);
            Assert.False(deviceTree.Children.First().Children.First().Children[1].Visible);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataIsTypeOfVariantValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantValue(1),
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms")))
               .Returns(new CallbackDelegate(a =>
               {
                   IDataElement dataElement = Mock.Of<IDataElement>();
                   Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                   dataElement.Address = "localhost";
                   var baseElement = new Mock<IBaseElement>();
                   baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                   return baseElement.Object;
               }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddAlarms(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Alarms", deviceTree.Children.First().Name);
            Assert.Equal("test/Alarms", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenVariantIsTypeOfVariantValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantValue(1));

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms")))
               .Returns(new CallbackDelegate(a =>
               {
                   IDataElement dataElement = Mock.Of<IDataElement>();
                   Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                   dataElement.Address = "localhost";
                   var baseElement = new Mock<IBaseElement>();
                   baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                   return baseElement.Object;
               }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddAlarms(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Alarms", deviceTree.Children.First().Name);
            Assert.Equal("test/Alarms", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenVariantHasNoValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                Name = "Test",
                MacAddress = "00:00:00:00:00:00",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value1"] = new VariantObject(),
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms")))
               .Returns(new CallbackDelegate(a =>
               {
                   IDataElement dataElement = Mock.Of<IDataElement>();
                   Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                   dataElement.Address = "localhost";
                   var baseElement = new Mock<IBaseElement>();
                   baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                   return baseElement.Object;
               }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddAlarms(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Alarms", deviceTree.Children.First().Name);
            Assert.Equal("test/Alarms", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataHasNoValues()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject(),
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms")))
               .Returns(new CallbackDelegate(a =>
               {
                   IDataElement dataElement = Mock.Of<IDataElement>();
                   Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                   dataElement.Address = "localhost";
                   var baseElement = new Mock<IBaseElement>();
                   baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                   return baseElement.Object;
               }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Alarms/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddAlarms(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Alarms", deviceTree.Children.First().Name);
            Assert.Equal("test/Alarms", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }
    }

    public class AddExternalInputs
    {
        [Fact]
        public void SetsCorrectExternalInputs()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantObject()
                        {
                            ["Name"] = new VariantValue("TestInput"),
                            ["Id"] = new VariantValue(1),
                            ["Unit"] = new VariantValue(15),
                        },
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddExternalInputs(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Inputs", deviceTree.Children.First().Name);
            Assert.Equal("test/Inputs", deviceTree.Children.First().Id);

            Assert.Single(deviceTree.Children.First().Children);
            IDeviceTreeBase secondNode = deviceTree.Children.First().Children.First();
            Assert.IsType<DeviceTreeStructureNode>(secondNode);
            Assert.Equal("External", secondNode.Name);
            Assert.Equal("test/Inputs/External", secondNode.Id);

            IDeviceTreeBase deviceTreeVseInputNode = secondNode.Children.First();
            Assert.Equal("TestInput", ((DeviceTreeVseInput)deviceTreeVseInputNode).Alias);
            Assert.Equal("dataElement", deviceTreeVseInputNode.Name);
            Assert.Equal($"test/Inputs/External/dataElement{VseParser.IdSeparatorNameAlias}TestInput", deviceTreeVseInputNode.Id);
            Assert.Equal("localhost", ((DeviceTreeVseInput)deviceTreeVseInputNode).Path);
            Assert.Equal("15", ((DeviceTreeVseInput)deviceTreeVseInputNode).Unit);
            Assert.Equal(1, ((DeviceTreeVseInput)deviceTreeVseInputNode).InputId);
            Assert.Equal(3, deviceTreeVseInputNode.Children.Count);
            Assert.IsType<DeviceTreeProcessData>(deviceTreeVseInputNode.Children.First());
            Assert.Equal("Value", deviceTreeVseInputNode.Children.First().Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTreeVseInputNode.Children.First()).DataType);
            Assert.Equal($"test/Inputs/External/dataElement{VseParser.IdSeparatorNameAlias}TestInput/Value", deviceTreeVseInputNode.Children.First().Id);
            Assert.True(((DeviceTreeProcessData)deviceTreeVseInputNode.Children.First()).HasUnit);
            Assert.True(((DeviceTreeProcessData)deviceTreeVseInputNode.Children.First()).IsWriteable);
            Assert.IsType<DeviceTreeProcessData>(deviceTreeVseInputNode.Children[1]);
            Assert.Equal("Unit", deviceTreeVseInputNode.Children[1].Name);
            Assert.Equal(DataType.StringT, ((DeviceTreeProcessData)deviceTreeVseInputNode.Children[1]).DataType);
            Assert.Equal($"test/Inputs/External/dataElement{VseParser.IdSeparatorNameAlias}TestInput/Unit", deviceTreeVseInputNode.Children[1].Id);
            Assert.False(((DeviceTreeProcessData)deviceTreeVseInputNode.Children[1]).Visible);
            Assert.IsType<DeviceTreeConstantData>(deviceTreeVseInputNode.Children[2]);
            Assert.Equal("Name", deviceTreeVseInputNode.Children[2].Name);
            Assert.Equal(DataType.StringT, ((DeviceTreeConstantData)deviceTreeVseInputNode.Children[2]).DataType);
            Assert.Equal($"test/Inputs/External/dataElement{VseParser.IdSeparatorNameAlias}TestInput/Name", deviceTreeVseInputNode.Children[2].Id);
            Assert.Equal("TestInput", ((DeviceTreeConstantData)deviceTreeVseInputNode.Children[2]).Value);
            Assert.False(deviceTreeVseInputNode.Children[1].Visible);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataIsTypeOfVariantValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantValue(1),
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddExternalInputs(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            IDeviceTreeBase firstNode = deviceTree.Children.First();
            Assert.IsType<DeviceTreeStructureNode>(firstNode);
            Assert.Equal("Inputs", firstNode.Name);
            Assert.Equal("test/Inputs", firstNode.Id);
            IDeviceTreeBase secondNode = firstNode.Children.First();
            Assert.IsType<DeviceTreeStructureNode>(secondNode);
            Assert.Equal("External", secondNode.Name);
            Assert.Equal("test/Inputs/External", secondNode.Id);
            Assert.Empty(secondNode.Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenVariantIsTypeOfVariantValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();
            var variantObject = new VariantObject
            {
                ["value"] = new VariantObject()
            };

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(variantObject);

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddExternalInputs(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            IDeviceTreeBase firstNode = deviceTree.Children.First();
            Assert.IsType<DeviceTreeStructureNode>(firstNode);
            Assert.Equal("Inputs", firstNode.Name);
            Assert.Equal("test/Inputs", firstNode.Id);
            IDeviceTreeBase secondNode = firstNode.Children.First();
            Assert.IsType<DeviceTreeStructureNode>(secondNode);
            Assert.Equal("External", secondNode.Name);
            Assert.Equal("test/Inputs/External", secondNode.Id);
            Assert.Empty(secondNode.Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenVariantHasNoValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value1"] = new VariantObject(),
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddExternalInputs(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            IDeviceTreeBase firstNode = deviceTree.Children.First();
            Assert.IsType<DeviceTreeStructureNode>(firstNode);
            Assert.Equal("Inputs", firstNode.Name);
            Assert.Equal("test/Inputs", firstNode.Id);
            Assert.Empty(firstNode.Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataHasNoValues()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject(),
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddExternalInputs(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            IDeviceTreeBase firstNode = deviceTree.Children.First();
            Assert.IsType<DeviceTreeStructureNode>(firstNode);
            Assert.Equal("Inputs", firstNode.Name);
            Assert.Equal("test/Inputs", firstNode.Id);
            IDeviceTreeBase secondNode = firstNode.Children.First();
            Assert.IsType<DeviceTreeStructureNode>(secondNode);
            Assert.Equal("External", secondNode.Name);
            Assert.Equal("test/Inputs/External", secondNode.Id);
            Assert.Empty(secondNode.Children);
        }

        //baseElement.Setup(b => b.Subs).Returns(Enumerable.Empty<IBaseElement>()); wurde wegen nullability auf Enumerable.Empty geändert,
        //prüfen ob der Fall existieren kann und was dann passiert
        //    [Fact]
        //    public void SetsCorrectDeviceTreeStructureNodeWhenElementHasNoSubs()
        //    {
        //        var deviceTree = new DeviceTreeVseDevice()
        //        {
        //            Id = "test",
        //            Name = "Test",
        //            VseAddress = "127.0.0.1",
        //        };
        //        var deviceNode = new Mock<IIoTCore>();
        //        var serviceElement = new Mock<IServiceElement>();

        //        serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
        //            .Returns(new VariantObject
        //            {
        //                ["value"] = new VariantObject(),
        //            });

        //        deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External")))
        //            .Returns(new CallbackDelegate((string a) =>
        //            {
        //                IDataElement dataElement = Mock.Of<IDataElement>();
        //                Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
        //                dataElement.Address = "localhost";
        //                var baseElement = new Mock<IBaseElement>();
        //                baseElement.Setup(b => b.Subs).Returns(Enumerable.Empty<IBaseElement>());

        //                return baseElement.Object;
        //            }));
        //        deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/External/getdata")))
        //            .Returns(serviceElement.Object);

        //        VseParser.AddExternalInputs(deviceTree, deviceNode.Object);

        //        Assert.Single(deviceTree.Children);
        //        IDeviceTreeBase firstNode = deviceTree.Children.First();
        //        Assert.IsType<DeviceTreeStructureNode>(firstNode);
        //        Assert.Equal("Inputs", firstNode.Name);
        //        Assert.Equal("test/Inputs", firstNode.Id);
        //        Assert.Empty(firstNode.Children);
        //    }
    }

    public class AddCounters
    {
        [Fact]
        public void SetsCorrectCounters()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantObject()
                        {
                            ["Name"] = new VariantValue("TestCounter"),
                            ["Type"] = new VariantValue("DefaultCounter"),
                            ["Unit"] = new VariantValue(15),
                        },
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddCounters(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Counters", deviceTree.Children.First().Name);
            Assert.Equal("test/Counters", deviceTree.Children.First().Id);
            Assert.Single(deviceTree.Children.First().Children);
            Assert.IsType<DeviceTreeVseCounter>(deviceTree.Children.First().Children.First());
            Assert.Equal("TestCounter", ((DeviceTreeVseCounter)deviceTree.Children.First().Children.First()).Alias);
            Assert.Equal("dataElement", deviceTree.Children.First().Children.First().Name);
            Assert.Equal($"test/Counters/dataElement{VseParser.IdSeparatorNameAlias}TestCounter", deviceTree.Children.First().Children.First().Id);
            Assert.Equal("localhost", ((DeviceTreeVseCounter)deviceTree.Children.First().Children.First()).Path);
            Assert.Equal("DefaultCounter", ((DeviceTreeVseCounter)deviceTree.Children.First().Children.First()).Type);
            Assert.Equal("15", ((DeviceTreeVseCounter)deviceTree.Children.First().Children.First()).Unit);
            Assert.Equal(5, deviceTree.Children.First().Children.First().Children.Count);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children.First());
            Assert.Equal("Value", deviceTree.Children.First().Children.First().Children.First().Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children.First()).DataType);
            Assert.True(((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children.First()).HasUnit);
            Assert.Equal($"test/Counters/dataElement{VseParser.IdSeparatorNameAlias}TestCounter/Value", deviceTree.Children.First().Children.First().Children.First().Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[1]);
            Assert.Equal("Unit", deviceTree.Children.First().Children.First().Children[1].Name);
            Assert.Equal(DataType.StringT, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[1]).DataType);
            Assert.False(deviceTree.Children.First().Children.First().Children[1].Visible);
            Assert.Equal($"test/Counters/dataElement{VseParser.IdSeparatorNameAlias}TestCounter/Unit", deviceTree.Children.First().Children.First().Children[1].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[2]);
            Assert.Equal("Limit", deviceTree.Children.First().Children.First().Children[2].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[2]).DataType);
            Assert.Equal($"test/Counters/dataElement{VseParser.IdSeparatorNameAlias}TestCounter/Limit", deviceTree.Children.First().Children.First().Children[2].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[3]);
            Assert.Equal("State", deviceTree.Children.First().Children.First().Children[3].Name);
            Assert.Equal(DataType.StringT, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[3]).DataType);
            Assert.Equal($"test/Counters/dataElement{VseParser.IdSeparatorNameAlias}TestCounter/State", deviceTree.Children.First().Children.First().Children[3].Id);
            Assert.IsType<DeviceTreeConstantData>(deviceTree.Children.First().Children.First().Children[4]);
            Assert.Equal("Name", deviceTree.Children.First().Children.First().Children[4].Name);
            Assert.Equal(DataType.StringT, ((DeviceTreeConstantData)deviceTree.Children.First().Children.First().Children[4]).DataType);
            Assert.Equal($"test/Counters/dataElement{VseParser.IdSeparatorNameAlias}TestCounter/Name", deviceTree.Children.First().Children.First().Children[4].Id);
            Assert.Equal("TestCounter", ((DeviceTreeConstantData)deviceTree.Children.First().Children.First().Children[4]).Value);
            Assert.False(deviceTree.Children.First().Children.First().Children[4].Visible);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataIsTypeOfVariantValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantValue(1),
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddCounters(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Counters", deviceTree.Children.First().Name);
            Assert.Equal("test/Counters", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenVariantIsTypeOfVariantValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantValue(1));

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddCounters(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Counters", deviceTree.Children.First().Name);
            Assert.Equal("test/Counters", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenVariantHasNoValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                    .Returns(new VariantObject
                    {
                        ["value1"] = new VariantObject(),
                    });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddCounters(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Counters", deviceTree.Children.First().Name);
            Assert.Equal("test/Counters", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataHasNoValues()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                    .Returns(new VariantObject
                    {
                        ["value"] = new VariantObject(),
                    });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Counters/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddCounters(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Counters", deviceTree.Children.First().Name);
            Assert.Equal("test/Counters", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }
    }

    public class AddInfos
    {
        [Fact]
        public void SetsCorrectInfos()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var informationServiceElement = new Mock<IServiceElement>();
            var ipServiceElement = new Mock<IServiceElement>();

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string t) => t == "/Device/Information/Device/getdata")))
                .Returns(new CallbackDelegate(a => informationServiceElement.Object));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string t) => t == "/Device/Information/IP/MACAddress/getdata")))
                .Returns(new CallbackDelegate(a => ipServiceElement.Object));

            informationServiceElement.Setup(i => i.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject
                    {
                        ["Type"] = new VariantValue("InfoType"),
                        ["Name"] = new VariantValue("InfoName"),
                        ["Firmware"] = new VariantValue("vVersion"),
                        ["Revision"] = new VariantValue("hwRevision"),
                        ["Serial"] = new VariantValue("12345"),
                    },
                });

            ipServiceElement.Setup(i => i.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantValue("bd:5e:cb:e7:cd:3a"),
                });

            VseParser.AddInfos(deviceTree, deviceNode.Object);

            Assert.Equal("InfoType - InfoName", deviceTree.Name);
            Assert.Equal("bd:5e:cb:e7:cd:3a", deviceTree.MacAddress);
        }

        [Fact]
        public void ReturnsWhenServiceVariantHasNoValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = string.Empty,
                Name = "Test",
                Description = new()
                {
                    Text = string.Empty,
                },
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var informationServiceElement = new Mock<IServiceElement>();

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string t) => t == "/Device/Information/Device/getdata")))
                .Returns(new CallbackDelegate(a => informationServiceElement.Object));

            informationServiceElement.Setup(i => i.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value1"] = new VariantObject(),
                });

            VseParser.AddInfos(deviceTree, deviceNode.Object);

            Assert.Equal("Test", deviceTree.Name);
            Assert.Equal(string.Empty, deviceTree.Description.Text);
            Assert.Equal(string.Empty, deviceTree.MacAddress);
        }

        [Fact]
        public void ReturnsWhenMacVariantIsNotVariantValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = string.Empty,
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var informationServiceElement = new Mock<IServiceElement>();
            var ipServiceElement = new Mock<IServiceElement>();

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string t) => t == "/Device/Information/Device/getdata")))
                .Returns(new CallbackDelegate(a => informationServiceElement.Object));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string t) => t == "/Device/Information/IP/MACAddress/getdata")))
                .Returns(new CallbackDelegate(a => ipServiceElement.Object));

            informationServiceElement.Setup(i => i.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject
                    {
                        ["Type"] = new VariantValue("InfoType"),
                        ["Name"] = new VariantValue("InfoName"),
                        ["Firmware"] = new VariantValue("vVersion"),
                        ["Revision"] = new VariantValue("hwRevision"),
                        ["Serial"] = new VariantValue("12345"),
                    },
                });

            ipServiceElement.Setup(i => i.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject());

            VseParser.AddInfos(deviceTree, deviceNode.Object);

            Assert.Equal("InfoType - InfoName", deviceTree.Name);
            Assert.Equal(string.Empty, deviceTree.MacAddress);
        }
    }

    public class AddObjects
    {
        [Fact]
        public void SetsCorrectObjects()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantObject()
                        {
                            ["Name"] = new VariantValue("TestObject"),
                            ["InputType"] = new VariantValue("InputType"),
                            ["InputID"] = new VariantValue(1),
                            ["ID"] = new VariantValue(1),
                            ["Type"] = new VariantValue("TestObject1"),
                            ["Unit"] = new VariantValue(123),
                        },
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddObjects(deviceTree, deviceNode.Object, new Mock<ILogger>().Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Objects", deviceTree.Children.First().Name);
            Assert.Equal("test/Objects", deviceTree.Children.First().Id);
            Assert.Single(deviceTree.Children.First().Children);
            Assert.IsType<DeviceTreeVseObject>(deviceTree.Children.First().Children.First());
            Assert.Equal("TestObject", ((DeviceTreeVseObject)deviceTree.Children.First().Children.First()).Alias);
            Assert.Equal("dataElement", deviceTree.Children.First().Children.First().Name);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject", deviceTree.Children.First().Children.First().Id);
            Assert.Equal("InputType", ((DeviceTreeVseObject)deviceTree.Children.First().Children.First()).InputType);
            Assert.Equal("1", ((DeviceTreeVseObject)deviceTree.Children.First().Children.First()).InputId);
            Assert.Equal("1", ((DeviceTreeVseObject)deviceTree.Children.First().Children.First()).ObjectId);
            Assert.Equal("localhost", ((DeviceTreeVseObject)deviceTree.Children.First().Children.First()).Path);
            Assert.Equal("TestObject1", ((DeviceTreeVseObject)deviceTree.Children.First().Children.First()).Type);
            Assert.Equal("123", ((DeviceTreeVseObject)deviceTree.Children.First().Children.First()).Unit);
            Assert.Equal(12, deviceTree.Children.First().Children.First().Children.Count);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children.First());
            Assert.Equal("Average", deviceTree.Children.First().Children.First().Children.First().Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children.First()).DataType);
            Assert.True(((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children.First()).HasUnit);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/Average", deviceTree.Children.First().Children.First().Children.First().Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[1]);
            Assert.Equal("Maximum", deviceTree.Children.First().Children.First().Children[1].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[1]).DataType);
            Assert.True(((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[1]).HasUnit);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/Maximum", deviceTree.Children.First().Children.First().Children[1].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[2]);
            Assert.Equal("Minimum", deviceTree.Children.First().Children.First().Children[2].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[2]).DataType);
            Assert.True(((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[2]).HasUnit);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/Minimum", deviceTree.Children.First().Children.First().Children[2].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[3]);
            Assert.Equal("Unit", deviceTree.Children.First().Children.First().Children[3].Name);
            Assert.Equal(DataType.StringT, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[3]).DataType);
            Assert.False(deviceTree.Children.First().Children.First().Children[3].Visible);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/Unit", deviceTree.Children.First().Children.First().Children[3].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[4]);
            Assert.Equal("RotSpeed", deviceTree.Children.First().Children.First().Children[4].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[4]).DataType);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/RotSpeed", deviceTree.Children.First().Children.First().Children[4].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[4]);
            Assert.Equal("RotSpeed", deviceTree.Children.First().Children.First().Children[4].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[4]).DataType);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/RotSpeed", deviceTree.Children.First().Children.First().Children[4].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[5]);
            Assert.Equal("RefValue", deviceTree.Children.First().Children.First().Children[5].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[5]).DataType);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/RefValue", deviceTree.Children.First().Children.First().Children[5].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[6]);
            Assert.Equal("Warning", deviceTree.Children.First().Children.First().Children[6].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[6]).DataType);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/Warning", deviceTree.Children.First().Children.First().Children[6].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[7]);
            Assert.Equal("BaseLine", deviceTree.Children.First().Children.First().Children[7].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[7]).DataType);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/BaseLine", deviceTree.Children.First().Children.First().Children[7].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[8]);
            Assert.Equal("Damage", deviceTree.Children.First().Children.First().Children[8].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[8]).DataType);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/Damage", deviceTree.Children.First().Children.First().Children[8].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[9]);
            Assert.Equal("AveragingPeriod", deviceTree.Children.First().Children.First().Children[9].Name);
            Assert.Equal(DataType.Float32T, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[9]).DataType);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/AveragingPeriod", deviceTree.Children.First().Children.First().Children[9].Id);
            Assert.IsType<DeviceTreeProcessData>(deviceTree.Children.First().Children.First().Children[10]);
            Assert.Equal("Error", deviceTree.Children.First().Children.First().Children[10].Name);
            Assert.Equal(DataType.StringT, ((DeviceTreeProcessData)deviceTree.Children.First().Children.First().Children[10]).DataType);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/Error", deviceTree.Children.First().Children.First().Children[10].Id);
            Assert.IsType<DeviceTreeConstantData>(deviceTree.Children.First().Children.First().Children[11]);
            Assert.Equal("Name", deviceTree.Children.First().Children.First().Children[11].Name);
            Assert.Equal(DataType.StringT, ((DeviceTreeConstantData)deviceTree.Children.First().Children.First().Children[11]).DataType);
            Assert.Equal($"test/Objects/dataElement{VseParser.IdSeparatorNameAlias}TestObject/Name", deviceTree.Children.First().Children.First().Children[11].Id);
            Assert.Equal("TestObject", ((DeviceTreeConstantData)deviceTree.Children.First().Children.First().Children[11]).Value);
            Assert.False(deviceTree.Children.First().Children.First().Children[11].Visible);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataIsTypeOfVariantValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantValue(1),
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddObjects(deviceTree, deviceNode.Object, new Mock<ILogger>().Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Objects", deviceTree.Children.First().Name);
            Assert.Equal("test/Objects", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenVariantIsTypeOfVariantValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantValue(1));

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddObjects(deviceTree, deviceNode.Object, new Mock<ILogger>().Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Objects", deviceTree.Children.First().Name);
            Assert.Equal("test/Objects", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenVariantHasNoValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value1"] = new VariantObject(),
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddObjects(deviceTree, deviceNode.Object, new Mock<ILogger>().Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Objects", deviceTree.Children.First().Name);
            Assert.Equal("test/Objects", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataHasNoValues()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject(),
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Objects/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddObjects(deviceTree, deviceNode.Object, new Mock<ILogger>().Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("Objects", deviceTree.Children.First().Name);
            Assert.Equal("test/Objects", deviceTree.Children.First().Id);
            Assert.Empty(deviceTree.Children.First().Children);
        }
    }

    public class GetVsePortFromUri
    {
        [Theory]
        [InlineData(1234, 1234)]
        [InlineData(null, 3321)]
        public void ReturnsCorrectPort(int? port, int expected)
        {
            Uri uri = port.HasValue ? new UriBuilder("http", "test.com", port.Value).Uri : new Uri("http://www.test.com/");

            Assert.Equal(expected, VseAddresses.GetVsePortFromUri(uri));
        }
    }

    public class AddRawData
    {
        [Fact]
        public void SetsCorrectRawData()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["Data"] = new VariantObject()
                        {
                            ["Name"] = new VariantValue("name"),
                            ["Id"] = new VariantValue(1),
                        },
                    },
                });
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string s) => s == "/Device/Inputs/Dynamic/getdata")))
                .Returns(serviceElement.Object);

            VseParser.AddRawData(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("test/RawData", deviceTree.Children.First().Id);
            Assert.Equal("RawData", deviceTree.Children.First().Name);
            Assert.Single(deviceTree.Children.First().Children);
            Assert.IsType<DeviceTreeVseRawData>(deviceTree.Children.First().Children.First());
            Assert.Equal("test/RawData/name", deviceTree.Children.First().Children.First().Id);
            Assert.Equal("name", deviceTree.Children.First().Children.First().Name);
            Assert.Equal(1, ((DeviceTreeVseRawData)deviceTree.Children.First().Children.First()).Index);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenVariantIsNotVariantObject()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantValue(1));

            deviceNode.Setup(d => d.GetElementByAddress(It.IsAny<string>()))
                .Returns(new CallbackDelegate(a => serviceElement.Object));

            VseParser.AddRawData(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("test/RawData", deviceTree.Children.First().Id);
            Assert.Equal("RawData", deviceTree.Children.First().Name);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataIsEmpty()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject { ["value"] = null, });

            deviceNode.Setup(d => d.GetElementByAddress(It.IsAny<string>()))
                .Returns(new CallbackDelegate(a => serviceElement.Object));

            VseParser.AddRawData(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("test/RawData", deviceTree.Children.First().Id);
            Assert.Equal("RawData", deviceTree.Children.First().Name);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataHasNoValue()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject { ["value1"] = null, });

            deviceNode.Setup(d => d.GetElementByAddress(It.IsAny<string>()))
                .Returns(new CallbackDelegate(a => serviceElement.Object));

            VseParser.AddRawData(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("test/RawData", deviceTree.Children.First().Id);
            Assert.Equal("RawData", deviceTree.Children.First().Name);
        }

        [Fact]
        public void SetsCorrectDeviceTreeStructureNodeWhenDataIsNotVariantObject()
        {
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["Data"] = new VariantValue(1),
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.IsAny<string>()))
                .Returns(new CallbackDelegate(a => serviceElement.Object));

            VseParser.AddRawData(deviceTree, deviceNode.Object);

            Assert.Single(deviceTree.Children);
            Assert.IsType<DeviceTreeStructureNode>(deviceTree.Children.First());
            Assert.Equal("test/RawData", deviceTree.Children.First().Id);
            Assert.Equal("RawData", deviceTree.Children.First().Name);
        }
    }

    public class ParseVse
    {
        [Fact]
        public void ParsesCorrectly()
        {
            var deviceNode = new Mock<IIoTCore>();
            var serviceElement = new Mock<IServiceElement>();
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://127.0.0.1"),
            };
            var informationServiceElement = new Mock<IServiceElement>();
            var ipServiceElement = new Mock<IServiceElement>();

            serviceElement.Setup(s => s.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject()
                    {
                        ["dataElement"] = new VariantObject()
                        {
                            ["Name"] = new VariantValue("name"),
                            ["Id"] = new VariantValue(1),
                            ["InputType"] = new VariantValue("InputType"),
                            ["InputID"] = new VariantValue(1),
                            ["ID"] = new VariantValue(1),
                            ["Type"] = new VariantValue("Test1"),
                            ["Unit"] = new VariantValue(123),
                        },
                    },
                });

            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string a) => a == "/Device/Inputs/Dynamic/getdata")))
                .Returns(new CallbackDelegate(a => serviceElement.Object));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string a) => a == "/Device/Information/Device/getdata")))
                .Returns(new CallbackDelegate(a => informationServiceElement.Object));
            deviceNode.Setup(d => d.GetElementByAddress(It.Is((string a) => a == "/Device/Information/IP/MACAddress/getdata")))
                .Returns(new CallbackDelegate(a => ipServiceElement.Object));
            deviceNode.Setup(
                d => d.GetElementByAddress(
                    It.Is((string a) =>
                    a == "/Device/Variants" ||
                    a == "/Device/Objects" ||
                    a == "/Device/Counters" ||
                    a == "/Device/Inputs/External" ||
                    a == "/Device/Alarms")))
                .Returns(new CallbackDelegate(a =>
                {
                    IDataElement dataElement = Mock.Of<IDataElement>();
                    Mock.Get(dataElement).Setup(d => d.Identifier).Returns("dataElement");
                    dataElement.Address = "localhost";
                    var baseElement = new Mock<IBaseElement>();
                    baseElement.Setup(b => b.Subs).Returns([dataElement,]);

                    return baseElement.Object;
                }));
            deviceNode.Setup(
                d => d.GetElementByAddress(
                    It.Is((string a) =>
                    a == "/Device/Variants/getdata" ||
                    a == "/Device/Objects/getdata" ||
                    a == "/Device/Counters/getdata" ||
                    a == "/Device/Inputs/External/getdata" ||
                    a == "/Device/Alarms/getdata")))
                .Returns(serviceElement.Object);

            informationServiceElement.Setup(i => i.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantObject
                    {
                        ["Type"] = new VariantValue("InfoType"),
                        ["Name"] = new VariantValue("InfoName"),
                        ["Firmware"] = new VariantValue("vVersion"),
                        ["Revision"] = new VariantValue("hwRevision"),
                        ["Serial"] = new VariantValue("12345"),
                    },
                });

            ipServiceElement.Setup(i => i.Invoke(It.IsAny<Variant>(), It.IsAny<int?>()))
                .Returns(new VariantObject
                {
                    ["value"] = new VariantValue("bd:5e:cb:e7:cd:3a"),
                });

            VseParser.ParseVse(deviceTree, deviceNode.Object, new Mock<ILogger>().Object);

            Assert.Equal(6, deviceTree.Children.Count);
            Assert.Equal("InfoType - InfoName", deviceTree.Name);
            Assert.Equal("bd:5e:cb:e7:cd:3a", deviceTree.MacAddress);
        }

        [Fact]
        public void LogsErrorWhenExceptionWasThrown()
        {
            var deviceNode = new Mock<IIoTCore>();
            var logger = new Mock<ILogger>();
            var deviceTree = new DeviceTreeVseDevice()
            {
                Id = "test",
                MacAddress = "00:00:00:00:00:00",
                Name = "Test",
                Url = new("http://localhost:1234"),
            };

            logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>()))
                .Returns(true);

            deviceNode.Setup(d => d.GetElementByAddress(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Test Exception"));

            VseParser.ParseVse(deviceTree, deviceNode.Object, logger.Object);

            logger.Verify(x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Parsing VSE device at '{deviceTree.Url}' failed: Test Exception,", StringComparison.Ordinal)),
                It.IsAny<InvalidOperationException>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)), Times.Once);
        }
    }
}
