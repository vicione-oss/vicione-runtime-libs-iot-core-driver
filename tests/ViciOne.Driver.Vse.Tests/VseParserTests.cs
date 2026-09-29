using IFM.SDK.VSE;
using IFM.SDK.VSE.DataChannels;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;
using Moq;
using ViciOne.Communication.Vse.Data;
using ViciOne.Communication.Vse.EventManager;
using ViciOne.DeviceTree.Contracts;

namespace ViciOne.Driver.Vse.Tests;

public class VseParserTests
{
    private static DeviceTreeVseDevice CreateTree() => new()
    {
        Id = "test",
        MacAddress = "00:00:00:00:00:00",
        Name = "Test",
        Url = new Uri("http://127.0.0.1"),
    };

    private static IDeviceTreeBase RequireChild(IDeviceTreeBase parent, string name)
        => parent.Children.Single(c => c.Name == name);

    private static DeviceInfo CreateDeviceInfo(DeviceType type = DeviceType.VSE150, string serial = "12345", string mac = "aa:bb:cc:dd:ee:ff")
    {
        Mock<IHardwareVersion> hardwareVersion = new();
        hardwareVersion.Setup(h => h.BoardVersion).Returns("A1");

        Mock<IDeviceInfo> deviceInfo = new();
        deviceInfo.Setup(d => d.DeviceType).Returns(type);
        deviceInfo.Setup(d => d.SerialNumber).Returns(serial);
        deviceInfo.Setup(d => d.MacAddress).Returns(mac);
        deviceInfo.Setup(d => d.FirmwareVersion).Returns(new IFM.SDK.VSE.Version(1, 2, 3));
        deviceInfo.Setup(d => d.HardwareVersion).Returns(hardwareVersion.Object);

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.DeviceInfo).Returns(deviceInfo.Object);

        return new DeviceInfo(device.Object);
    }

    private static Counters CreateCounters(params (int Id, string Name, CounterType Type, string Unit)[] items)
    {
        List<ICounter> counters = [];
        foreach ((int id, string name, CounterType type, string unit) in items)
        {
            Mock<ICounter> counter = new();
            counter.Setup(c => c.Id).Returns(id);
            counter.Setup(c => c.Name).Returns(name);
            counter.Setup(c => c.Type).Returns(type);
            counter.Setup(c => c.Unit).Returns(unit);
            counters.Add(counter.Object);
        }

        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Counters).Returns(counters);

        Mock<IDataLink> dataLink = new();
        if (counters.Count > 0)
        {
            Mock<IMonitoringChannel<ICounterDataSample>> channel = new();
            dataLink.Setup(d => d.CreateMonitoringChannel<ICounterDataSample>(It.IsAny<DataChannelInfo>())).Returns(channel.Object);
        }

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);
        device.Setup(d => d.DataLink).Returns(dataLink.Object);

        return new Counters(device.Object, logger: null);
    }

    private static DigitalAlarms CreateDigitalAlarms(params (int Id, string Name, AlarmType Type)[] items)
    {
        List<IDigitalAlarm> alarms = [];
        foreach ((int id, string name, AlarmType type) in items)
        {
            Mock<IDigitalAlarm> alarm = new();
            alarm.Setup(a => a.Id).Returns(id);
            alarm.Setup(a => a.Name).Returns(name);
            alarm.Setup(a => a.Type).Returns(type);
            alarms.Add(alarm.Object);
        }

        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.DigitalAlarms).Returns(alarms);

        Mock<IDataLink> dataLink = new();
        dataLink.Setup(d => d.CreateMonitoringChannel<IIODataSample>(It.IsAny<DataChannelInfo>()))
            .Returns(() => new Mock<IMonitoringChannel<IIODataSample>>().Object);

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);
        device.Setup(d => d.DataLink).Returns(dataLink.Object);

        return new DigitalAlarms(device.Object, logger: null);
    }

    private static Inputs CreateInputs(params (int Id, string Name, InputType Type, string Unit)[] items)
    {
        List<IInput> inputs = [];
        foreach ((int id, string name, InputType type, string unit) in items)
        {
            Mock<IInput> input = new();
            input.Setup(i => i.Id).Returns(id);
            input.Setup(i => i.Name).Returns(name);
            input.Setup(i => i.Type).Returns(type);
            input.Setup(i => i.Unit).Returns(unit);
            inputs.Add(input.Object);
        }

        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Inputs).Returns(inputs);

        Mock<IDataLink> dataLink = new();
        if (items.Any(i => i.Type == InputType.External))
        {
            Mock<IIODataSample> initialSample = new();
            initialSample.Setup(s => s.GetValue()).Returns(0f);
            Mock<IReadDataChannel<IIODataSample>> readChannel = new();
            readChannel.Setup(c => c.Read()).Returns(initialSample.Object);
            dataLink.Setup(d => d.GetReadDataChannel(It.IsAny<IInput>())).Returns(readChannel.Object);

            Mock<IMonitoringChannel<IIODataSample>> channel = new();
            dataLink.Setup(d => d.CreateMonitoringChannel<IIODataSample>(It.IsAny<DataChannelInfo>())).Returns(channel.Object);
        }

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);
        device.Setup(d => d.DataLink).Returns(dataLink.Object);

        return new Inputs(device.Object, logger: null);
    }

    private static DeviceObjects CreateDeviceObjects(params (int Id, string Name, ObjectType Type, string Unit, int InputId, InputType InputType)[] items)
    {
        List<IObject> objects = [];
        foreach ((int id, string name, ObjectType type, string unit, int inputId, InputType inputType) in items)
        {
            Mock<IObject> obj = new();
            obj.Setup(o => o.Id).Returns(id);
            obj.Setup(o => o.Name).Returns(name);
            obj.Setup(o => o.Type).Returns(type);
            obj.Setup(o => o.Unit).Returns(unit);
            obj.Setup(o => o.InputId).Returns(inputId);
            obj.Setup(o => o.InputType).Returns(inputType);
            objects.Add(obj.Object);
        }

        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Objects).Returns(objects);

        Mock<IMonitoringChannel<IObjectDataSample>> channel = new();
        channel.Setup(c => c.ChannelInfo).Returns([new ObjectChannelInfo([])]);

        Mock<IDataLink> dataLink = new();
        dataLink.Setup(d => d.CreateMonitoringChannel<IObjectDataSample>(It.IsAny<DataChannelInfo>())).Returns(channel.Object);

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);
        device.Setup(d => d.DataLink).Returns(dataLink.Object);

        using DeviceEventManager eventManager = new(device.Object, loggerFactory: null);
        return new DeviceObjects(device.Object, eventManager, logger: null);
    }

    private static Variants CreateVariants(params int[] ids)
    {
        List<IVariant> variants = [];
        foreach (int id in ids)
        {
            Mock<IVariant> variant = new();
            variant.Setup(v => v.Id).Returns(id);
            variant.Setup(v => v.ActivatedObjects).Returns([]);
            variants.Add(variant.Object);
        }

        Mock<IParameters> parameters = new();
        parameters.Setup(p => p.Variants).Returns(variants);

        Mock<IVSEDevice> device = new();
        device.Setup(d => d.Parameters).Returns(parameters.Object);

        using DeviceEventManager eventManager = new(device.Object, loggerFactory: null);
        return new Variants(device.Object, eventManager);
    }

    public class Infos
    {
        [Fact]
        public void MapsDeviceInfoOntoTheTree()
        {
            DeviceTreeVseDevice tree = CreateTree();
            DeviceInfo deviceInfo = CreateDeviceInfo(DeviceType.VSE150, "12345", "aa:bb:cc:dd:ee:ff");
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs();
            using DeviceObjects objects = CreateDeviceObjects();
            using Variants variants = CreateVariants();

            VseParser.ParseVse(tree, deviceInfo, alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            Assert.Equal("VSE150 - VSE150-12345", tree.Name);
            Assert.Equal("aa:bb:cc:dd:ee:ff", tree.MacAddress);
            Assert.Equal(deviceInfo.FirmwareVersion.ToString(), tree.SoftwareRevision);
            Assert.Equal("A1", tree.HardwareRevision);
            Assert.Equal("12345", tree.SerialNumber);
            Assert.Equal("ifm electronic GmbH", tree.Manufacturer);
            Assert.Equal("VSE150", tree.ProductName);
            Assert.Equal("VSE", tree.DeviceFamily);
        }
    }

    // Only the empty-list path is covered here: CreateDigitalAlarms() can't construct a DigitalAlarms with actual
    // alarms via a mocked IDigitalAlarm - AsChannelInfo() downcasts internally to the SDK's own concrete alarm
    // type and throws InvalidCastException for Moq's proxies (see the comment on DigitalAlarmsTests.cs). The
    // AddAlarms tree-shape logic for a populated alarm was verified against real hardware instead.
    public class Alarms
    {
        [Fact]
        public void NoAlarms_StillAddsTheEmptyAlarmsStructureNode()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs();
            using DeviceObjects objects = CreateDeviceObjects();
            using Variants variants = CreateVariants();

            VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            Assert.Empty(RequireChild(tree, VseParser.NodeNameAlarms).Children);
        }
    }

    public class CounterNodes
    {
        [Fact]
        public void AddsOneVseCounterPerCounterAndMapsItsAddress()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters((0, "Uptime", CounterType.VSEUptime, "h"));
            using Inputs inputs = CreateInputs();
            using DeviceObjects objects = CreateDeviceObjects();
            using Variants variants = CreateVariants();

            VseParseResult result = VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            DeviceTreeVseCounter counterNode = Assert.IsType<DeviceTreeVseCounter>(Assert.Single(RequireChild(tree, VseParser.NodeNameCounters).Children));
            Assert.Equal("Uptime", counterNode.Alias);
            Assert.Equal("Counter01", counterNode.Name);
            Assert.Equal($"{VseParser.AddressCounters}/Counter01", counterNode.Path);
            Assert.Equal("VSEUptime", counterNode.Type);
            Assert.Equal("h", counterNode.Unit);
            Assert.Equal(["Value", "Limit", "State", "Name"], counterNode.Children.Select(c => c.Name));

            Assert.Same(counters.All.Single(), result.ItemsById[counterNode.Path]);
        }
    }

    public class ExternalInputs
    {
        [Fact]
        public void OnlyIncludesExternalTypeInputs()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs(
                (0, "Speed", InputType.External, "rpm"),
                (1, "Vibration", InputType.Dynamic, string.Empty));
            using DeviceObjects objects = CreateDeviceObjects();
            using Variants variants = CreateVariants();

            VseParseResult result = VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            IDeviceTreeBase externalNode = RequireChild(RequireChild(tree, VseParser.NodeNameInputs), VseParser.NodeNameExternal);
            DeviceTreeVseInput inputNode = Assert.IsType<DeviceTreeVseInput>(Assert.Single(externalNode.Children));
            Assert.Equal("Speed", inputNode.Alias);
            Assert.Equal("Input01", inputNode.Name);
            Assert.Equal(1, inputNode.InputId);
            Assert.Equal("rpm", inputNode.Unit);
            Assert.Equal($"{VseParser.AddressExternalInputs}/Input01", inputNode.Path);

            Input speedInput = inputs.All.Single(i => i.Name == "Speed");
            Assert.Same(speedInput, result.ItemsById[inputNode.Path]);
            Assert.Same(speedInput, result.ItemsById[$"{inputNode.Path}/Value"]);
        }
    }

    public class Objects
    {
        [Fact]
        public void AddsOneVseObjectWithAllProcessDataChildren()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs();
            using DeviceObjects objects = CreateDeviceObjects((0, "Bearing", ObjectType.Bearing, "g", 2, InputType.Dynamic));
            using Variants variants = CreateVariants();

            VseParseResult result = VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            DeviceTreeVseObject objectNode = Assert.IsType<DeviceTreeVseObject>(Assert.Single(RequireChild(tree, VseParser.NodeNameObjects).Children));
            Assert.Equal("Bearing", objectNode.Alias);
            Assert.Equal("Object01", objectNode.Name);
            Assert.Equal("dynamic", objectNode.InputType);
            Assert.Equal("3", objectNode.InputId);
            Assert.Equal("1", objectNode.ObjectId);
            Assert.Equal("bearing", objectNode.Type);
            Assert.Equal("g", objectNode.Unit);
            Assert.Equal(
                ["Average", "Maximum", "Minimum", "RotSpeed", "RefValue", "Warning", "BaseLine", "Damage", "AveragingPeriod", "Error", "Name"],
                objectNode.Children.Select(c => c.Name));

            Assert.Same(objects.All.Single(), result.ItemsById[objectNode.Path]);
        }

        [Fact]
        public void EmptyUnit_LogsAWarning()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs();
            using DeviceObjects objects = CreateDeviceObjects((0, "Bearing", ObjectType.Bearing, string.Empty, 0, InputType.Dynamic));
            using Variants variants = CreateVariants();
            Mock<ILogger> logger = new();
            logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, logger.Object);

            logger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("Bearing", StringComparison.Ordinal)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }

    public class RawData
    {
        [Fact]
        public void OnlyIncludesDynamicTypeInputsAndUsesTheirOneBasedIdAsIndex()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs(
                (0, "Vibration", InputType.Dynamic, string.Empty),
                (1, "Speed", InputType.External, "rpm"));
            using DeviceObjects objects = CreateDeviceObjects();
            using Variants variants = CreateVariants();

            VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            DeviceTreeVseRawData rawDataNode = Assert.IsType<DeviceTreeVseRawData>(Assert.Single(RequireChild(tree, VseParser.NodeNameRawData).Children));
            Assert.Equal("Vibration", rawDataNode.Name);
            Assert.Equal(1, rawDataNode.Index);
        }
    }

    public class ActiveVariant
    {
        [Fact]
        public void VariantsExist_AddsTheVariantsNodeAndMapsItsAddress()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs();
            using DeviceObjects objects = CreateDeviceObjects();
            using Variants variants = CreateVariants(0, 1);

            VseParseResult result = VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            DeviceTreeVseVariants variantsNode = Assert.IsType<DeviceTreeVseVariants>(RequireChild(tree, VseParser.NodeNameVariants));
            Assert.Equal(VseParser.AddressVariants, variantsNode.Path);
            IDeviceTreeBase activeVariant = Assert.Single(variantsNode.Children);
            Assert.Equal("ActiveVariant", activeVariant.Name);
            Assert.True(((DeviceTreeProcessData)activeVariant).IsWriteable);

            // Two addresses resolve to the same Variants object: the node's own Path (register/read target) and
            // "<Path>/ActiveVariant" (write target) - see VariantsSubscriber.cs.
            Assert.Same(variants, result.ItemsById[variantsNode.Path]);
            Assert.Same(variants, result.ItemsById[$"{variantsNode.Path}/ActiveVariant"]);
        }

        [Fact]
        public void NoVariants_OmitsTheVariantsNodeEntirely()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs();
            using DeviceObjects objects = CreateDeviceObjects();
            using Variants variants = CreateVariants();

            VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            Assert.DoesNotContain(tree.Children, c => c.Name == VseParser.NodeNameVariants);
        }
    }

    public class RawDataTriggerEventRelations
    {
        [Fact]
        public void MarksOnlyTheMatchingDynamicObjectAsConfiguredForItsSensor()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs((0, "Vibration", InputType.Dynamic, string.Empty));
            using DeviceObjects objects = CreateDeviceObjects(
                (0, "Matching", ObjectType.Bearing, "g", 0, InputType.Dynamic),
                (1, "Other", ObjectType.Bearing, "g", 5, InputType.Analog));
            using Variants variants = CreateVariants();

            VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            DeviceTreeVseRawData rawDataNode = Assert.IsType<DeviceTreeVseRawData>(Assert.Single(RequireChild(tree, VseParser.NodeNameRawData).Children));
            Assert.True(rawDataNode.EventTriggerConfigurations.Single(e => e.Name == "Matching").IsSensorConfigured);
            Assert.False(rawDataNode.EventTriggerConfigurations.Single(e => e.Name == "Other").IsSensorConfigured);
        }
    }

    public class ParseVse
    {
        [Fact]
        public void AllCategoriesPopulated_ProducesOneTopLevelNodePerCategory()
        {
            DeviceTreeVseDevice tree = CreateTree();
            // Alarms left empty: CreateDigitalAlarms() can't construct a populated DigitalAlarms via a mocked
            // IDigitalAlarm (see the comment on the Alarms test class above) - the Alarms structure node is
            // still added regardless of whether it has children, so the top-level node count is unaffected.
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters((0, "Counter", CounterType.Totalisator, "x"));
            using Inputs inputs = CreateInputs((0, "Speed", InputType.External, "rpm"));
            using DeviceObjects objects = CreateDeviceObjects((0, "Object", ObjectType.Bearing, "g", 0, InputType.None));
            using Variants variants = CreateVariants(0);

            VseParser.ParseVse(tree, CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());

            Assert.Equal(
                [VseParser.NodeNameAlarms, VseParser.NodeNameCounters, VseParser.NodeNameInputs, VseParser.NodeNameObjects, VseParser.NodeNameRawData, VseParser.NodeNameVariants],
                tree.Children.Select(c => c.Name));
        }

        [Fact]
        public void ExceptionInOneCategory_LogsAWarningAndStillBuildsTheOtherCategories()
        {
            DeviceTreeVseDevice tree = CreateTree();
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs();
            using DeviceObjects objects = CreateDeviceObjects();
            using Variants variants = CreateVariants();
            Mock<ILogger> logger = new();
            logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            Mock<IDeviceInfo> deviceInfo = new();
            deviceInfo.Setup(d => d.FirmwareVersion).Throws(new InvalidOperationException("boom"));
            Mock<IVSEDevice> device = new();
            device.Setup(d => d.DeviceInfo).Returns(deviceInfo.Object);

            VseParser.ParseVse(tree, new DeviceInfo(device.Object), alarms, counters, inputs, objects, variants, logger.Object);

            // AddInfos (the only category that touches DeviceInfo) failed, but that shouldn't prevent the
            // unrelated categories (Alarms/Counters/Inputs/Objects/RawData) from still populating the tree.
            Assert.Null(tree.SoftwareRevision);
            Assert.NotEmpty(tree.Children);
            logger.Verify(l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v!.ToString()!.Contains("AddInfos", StringComparison.Ordinal)),
                It.Is<InvalidOperationException>(e => e.Message.Contains("boom", StringComparison.Ordinal)),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }
    }

    public class BuildItemsById
    {
        // BuildItemsById is a leaner alternative to ParseVse (VseDevice uses it to avoid materializing a whole
        // DeviceTree just to get the address lookup) - this pins it to produce exactly the same ItemsById as
        // ParseVse for the same inputs, so the two can never silently drift apart on address format.
        [Fact]
        public void MatchesParseVsesItemsById_ForAllCategoriesPopulated()
        {
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters((0, "Counter", CounterType.Totalisator, "x"));
            using Inputs inputs = CreateInputs((0, "Speed", InputType.External, "rpm"));
            using DeviceObjects objects = CreateDeviceObjects((0, "Object", ObjectType.Bearing, "g", 0, InputType.None));
            using Variants variants = CreateVariants(0);

            VseParseResult expected = VseParser.ParseVse(CreateTree(), CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());
            IReadOnlyDictionary<string, object> actual = VseParser.BuildItemsById(alarms, counters, inputs, objects, variants);

            Assert.Equal(expected.ItemsById.Keys.OrderBy(k => k, StringComparer.Ordinal), actual.Keys.OrderBy(k => k, StringComparer.Ordinal));
            foreach (string key in expected.ItemsById.Keys)
                Assert.Same(expected.ItemsById[key], actual[key]);
        }

        [Fact]
        public void MatchesParseVsesItemsById_ForAllCategoriesEmpty()
        {
            using DigitalAlarms alarms = CreateDigitalAlarms();
            using Counters counters = CreateCounters();
            using Inputs inputs = CreateInputs();
            using DeviceObjects objects = CreateDeviceObjects();
            using Variants variants = CreateVariants();

            VseParseResult expected = VseParser.ParseVse(CreateTree(), CreateDeviceInfo(), alarms, counters, inputs, objects, variants, Mock.Of<ILogger>());
            IReadOnlyDictionary<string, object> actual = VseParser.BuildItemsById(alarms, counters, inputs, objects, variants);

            Assert.Empty(actual);
            Assert.Equal(expected.ItemsById.Keys, actual.Keys);
        }
    }
}
