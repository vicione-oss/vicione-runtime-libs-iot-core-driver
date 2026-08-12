using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.Vse;
using ViciOne.Communication.Vse.Data;
using ViciOne.DeviceTree.Contracts;
using InputType = IFM.SDK.VSE.InputType;

namespace ViciOne.Driver.Vse;

/// <summary>
/// The result of parsing a <see cref="VseConnection"/>'s live data into the display-facing device tree, paired
/// with a lookup from each node's <c>Path</c> (the address a FunctionBlock's NodePath setting is actually
/// configured with, e.g. <c>/Device/Objects/Object01</c>) back to the
/// <see cref="Communication.Vse"/> domain object it represents - <see cref="VseDevice"/> uses that lookup
/// to dispatch pushes/writes without needing to know anything about the tree shape itself.
/// </summary>
internal readonly record struct VseParseResult(DeviceTreeVseDevice Tree, IReadOnlyDictionary<string, object> ItemsById);

/// <summary>
/// Builds the device tree for one <see cref="VseDevice"/> from its underlying connection's live data categories
/// (alarms, counters, inputs, objects, variants).
/// </summary>
public static class VseParser
{
    public const string IdentifierActiveVariant = "ActiveVariant";
    public const string IdentifierAverage = "Average";
    public const string IdentifierAveragingPeriod = "AveragingPeriod";
    public const string IdentifierBaseLine = "BaseLine";
    public const string IdentifierDamage = "Damage";
    public const string IdentifierError = "Error";
    public const string IdentifierLimit = "Limit";
    public const string IdentifierMaximum = "Maximum";
    public const string IdentifierMinimum = "Minimum";
    public const string IdentifierName = "Name";
    public const string IdentifierRefValue = "RefValue";
    public const string IdentifierRotSpeed = "RotSpeed";
    public const string IdentifierState = "State";
    public const string IdentifierValue = "Value";
    public const string IdentifierWarning = "Warning";

    public const string AddressAlarms = "/Device/Alarms";
    public const string AddressCounters = "/Device/Counters";
    public const string AddressExternalInputs = "/Device/Inputs/External";
    public const string AddressObjects = "/Device/Objects";
    public const string AddressVariants = "/Device/Variants";

    public const string NodeNameAlarms = "Alarms";
    public const string NodeNameCounters = "Counters";
    public const string NodeNameExternal = "External";
    public const string NodeNameInputs = "Inputs";
    public const string NodeNameObjects = "Objects";
    public const string NodeNameRawData = "RawData";
    public const string NodeNameVariants = "Variants";

    // TODO: the separator is stored as a constant in the DeviceManagement module. This reference direction should
    // maybe be changed at some point, but only if the DeviceTree remains the transport model.
    public const string IdSeparatorNameAlias = "__!__";

    // Single source of truth for each category's identifier format - shared between the full tree builders
    // below and BuildItemsById, so the two can never drift apart on what a given entity's address looks like.
    private static string AlarmIdentifier(int alarmId) => $"Alarm{alarmId + 1:D2}";
    private static string CounterIdentifier(int counterId) => $"Counter{counterId + 1:D2}";
    private static string ExternalInputIdentifier(int inputId) => $"Input{inputId + 1:D2}";
    private static string ObjectIdentifier(int objectId) => $"Object{objectId + 1:D2}";

    private static void AddActiveVariant(DeviceTreeVseDevice vse, Variants variants)
    {
        if (variants.All.Count == 0)
            return;

        string variantsId = $"{vse.Id}/{NodeNameVariants}";

        vse.Children.Add(new DeviceTreeVseVariants
        {
            Name = NodeNameVariants,
            Id = variantsId,
            Path = AddressVariants,
            Children =
            [
                new DeviceTreeProcessData()
                {
                    Name = IdentifierActiveVariant,
                    DataType = DataType.Whole,
                    Id = $"{variantsId}/{IdentifierActiveVariant}",
                    IsWriteable = true,
                },
            ]
        });
    }

    private static void AddAlarms(DeviceTreeVseDevice vse, DigitalAlarms alarms)
    {
        DeviceTreeStructureNode deviceTreeNode = new()
        {
            Name = NodeNameAlarms,
            Id = $"{vse.Id}/{NodeNameAlarms}",
        };
        vse.Children.Add(deviceTreeNode);

        foreach (DigitalAlarm alarm in alarms.All)
        {
            string identifier = AlarmIdentifier(alarm.Id);
            string id = $"{deviceTreeNode.Id}/{identifier}{IdSeparatorNameAlias}{alarm.Name}";
            string path = $"{AddressAlarms}/{identifier}";

            deviceTreeNode.Children.Add(new DeviceTreeVseAlarm()
            {
                Alias = alarm.Name,
                Name = identifier,
                Id = id,
                Path = path,
                Type = alarm.Type,
                Children =
                [
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierValue,
                        DataType = DataType.Flag,
                        Id = $"{id}/{IdentifierValue}",
                    },
                    new DeviceTreeAssignedName()
                    {
                        Name = IdentifierName,
                        Id = $"{id}/{IdentifierName}",
                        Value = alarm.Name,
                    }
                ]
            });
        }
    }

    private static void AddCounters(DeviceTreeVseDevice vse, Counters counters)
    {
        DeviceTreeStructureNode deviceTreeNode = new()
        {
            Name = NodeNameCounters,
            Id = $"{vse.Id}/{NodeNameCounters}",
        };
        vse.Children.Add(deviceTreeNode);

        foreach (Counter counter in counters.All)
        {
            string identifier = CounterIdentifier(counter.Id);
            string id = $"{deviceTreeNode.Id}/{identifier}{IdSeparatorNameAlias}{counter.Name}";
            string path = $"{AddressCounters}/{identifier}";

            deviceTreeNode.Children.Add(new DeviceTreeVseCounter()
            {
                Alias = counter.Name,
                Name = identifier,
                Id = id,
                Path = path,
                Type = counter.Type,
                Unit = counter.Unit,
                Children =
                [
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierValue,
                        DataType = DataType.Real,
                        HasUnit = true,
                        Id = $"{id}/{IdentifierValue}",
                        Unit = counter.Unit,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierLimit,
                        DataType = DataType.Real,
                        Id = $"{id}/{IdentifierLimit}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierState,
                        DataType = DataType.Text,
                        Id = $"{id}/{IdentifierState}",
                    },
                    new DeviceTreeAssignedName()
                    {
                        Name = IdentifierName,
                        Id = $"{id}/{IdentifierName}",
                        Value = counter.Name,
                    }
                ]
            });
        }
    }

    private static void AddExternalInputs(DeviceTreeVseDevice vse, Inputs inputs)
    {
        DeviceTreeStructureNode deviceTreeInputs = new()
        {
            Name = NodeNameInputs,
            Id = $"{vse.Id}/{NodeNameInputs}",
        };
        vse.Children.Add(deviceTreeInputs);

        DeviceTreeStructureNode deviceTreeExternal = new()
        {
            Name = NodeNameExternal,
            Id = $"{deviceTreeInputs.Id}/{NodeNameExternal}",
        };
        deviceTreeInputs.Children.Add(deviceTreeExternal);

        foreach (Input input in inputs.All)
        {
            if (input.RawType != InputType.External)
                continue;

            string identifier = ExternalInputIdentifier(input.Id);
            string id = $"{deviceTreeExternal.Id}/{identifier}{IdSeparatorNameAlias}{input.Name}";
            string path = $"{AddressExternalInputs}/{identifier}";

            deviceTreeExternal.Children.Add(new DeviceTreeVseInput()
            {
                Alias = input.Name,
                Name = identifier,
                Id = id,
                InputId = input.Id + 1,
                Path = path,
                Unit = input.Unit,
                Children =
                [
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierValue,
                        DataType = DataType.Real,
                        Id = $"{id}/{IdentifierValue}",
                        HasUnit = true,
                        IsWriteable = true,
                        Unit = input.Unit,
                    },
                    new DeviceTreeAssignedName()
                    {
                        Name = IdentifierName,
                        Id = $"{id}/{IdentifierName}",
                        Value = input.Name,
                    }
                ]
            });
        }
    }

    private static void AddInfos(DeviceTreeVseDevice vse, DeviceInfo deviceInfo)
    {
        vse.SoftwareRevision = deviceInfo.FirmwareVersion.ToString();
        vse.HardwareRevision = deviceInfo.BoardVersion;
        vse.SerialNumber = deviceInfo.SerialNumber;
        vse.Manufacturer = "ifm electronic GmbH";
        vse.ProductName = deviceInfo.Type;
        vse.DeviceFamily = "VSE";
        vse.Name = $"{deviceInfo.Type} - {deviceInfo.Name}";
        vse.Description = null;
        vse.MacAddress = deviceInfo.MacAddress;
    }

    private static void AddObjects(DeviceTreeVseDevice vse, DeviceObjects objects, ILogger logger)
    {
        DeviceTreeStructureNode deviceTreeNode = new()
        {
            Name = NodeNameObjects,
            Id = $"{vse.Id}/{NodeNameObjects}",
        };
        vse.Children.Add(deviceTreeNode);

        foreach (DeviceObject deviceObject in objects.All)
        {
            string identifier = ObjectIdentifier(deviceObject.Id);
            string id = $"{deviceTreeNode.Id}/{identifier}{IdSeparatorNameAlias}{deviceObject.Name}";
            string path = $"{AddressObjects}/{identifier}";

            if (string.IsNullOrEmpty(deviceObject.Unit))
                logger.NoUnitSet(deviceObject.Name, vse.Url);

            deviceTreeNode.Children.Add(new DeviceTreeVseObject()
            {
                Alias = deviceObject.Name,
                Name = identifier,
                Id = id,
                InputType = deviceObject.InputType,
                InputId = (deviceObject.InputId + 1).ToString(CultureInfo.InvariantCulture),
                ObjectId = (deviceObject.Id + 1).ToString(CultureInfo.InvariantCulture),
                Path = path,
                Type = deviceObject.Type,
                Unit = deviceObject.Unit,
                Children =
                [
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierAverage,
                        DataType = DataType.Real,
                        HasUnit = true,
                        Id = $"{id}/{IdentifierAverage}",
                        Unit = deviceObject.Unit,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierMaximum,
                        DataType = DataType.Real,
                        HasUnit = true,
                        Id = $"{id}/{IdentifierMaximum}",
                        Unit = deviceObject.Unit,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierMinimum,
                        DataType = DataType.Real,
                        HasUnit = true,
                        Id = $"{id}/{IdentifierMinimum}",
                        Unit = deviceObject.Unit,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierRotSpeed,
                        DataType = DataType.Real,
                        Id = $"{id}/{IdentifierRotSpeed}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierRefValue,
                        DataType = DataType.Real,
                        Id = $"{id}/{IdentifierRefValue}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierWarning,
                        DataType = DataType.Real,
                        Id = $"{id}/{IdentifierWarning}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierBaseLine,
                        DataType = DataType.Real,
                        Id = $"{id}/{IdentifierBaseLine}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierDamage,
                        DataType = DataType.Real,
                        Id = $"{id}/{IdentifierDamage}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierAveragingPeriod,
                        DataType = DataType.Real,
                        Id = $"{id}/{IdentifierAveragingPeriod}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierError,
                        DataType = DataType.Text,
                        Id = $"{id}/{IdentifierError}",
                    },
                    new DeviceTreeAssignedName()
                    {
                        Name = IdentifierName,
                        Id = $"{id}/{IdentifierName}",
                        Value = deviceObject.Name,
                    },
                ]
            });
        }
    }

    private static void AddRawData(DeviceTreeVseDevice vse, Inputs inputs)
    {
        DeviceTreeStructureNode rawDataDeviceNode = new()
        {
            Name = NodeNameRawData,
            Id = $"{vse.Id}/{NodeNameRawData}",
        };
        vse.Children.Add(rawDataDeviceNode);

        // The raw-data-recordable "sensors" are exactly the device's Dynamic inputs - RawDataRecorder's 1-based
        // sensor index is that same input's own (1-based) id.
        foreach (Input input in inputs.All)
        {
            if (input.RawType != InputType.Dynamic)
                continue;

            rawDataDeviceNode.Children.Add(new DeviceTreeVseRawData()
            {
                Name = input.Name,
                Id = $"{rawDataDeviceNode.Id}/{input.Name}",
                Index = input.Id + 1,
            });
        }
    }

    private static void AddRawDataTriggerEventRelations(DeviceTreeVseDevice vse)
    {
        DeviceTreeVseObject[]? vseObjects = vse.Children.FirstOrDefault(c => c.Name == NodeNameObjects)?.Children.Cast<DeviceTreeVseObject>().ToArray();
        if (vseObjects is null || vseObjects.Length == 0)
            return;

        DeviceTreeVseRawData[]? vseRawDatas = vse.Children.FirstOrDefault(c => c.Name == NodeNameRawData)?.Children.Cast<DeviceTreeVseRawData>().ToArray();
        if (vseRawDatas is null || vseRawDatas.Length == 0)
            return;

        foreach (DeviceTreeVseRawData vseRawData in vseRawDatas)
        {
            foreach (DeviceTreeVseObject vseObject in vseObjects)
            {
                vseRawData.EventTriggerConfigurations.Add(new()
                {
                    Name = vseObject.Alias ?? vseObject.Name,
                    IsSensorConfigured = GetIsSensorConfigured(vseObject, vseRawData),
                    ReferenceNodeId = vseObject.Id,
                });
            }
        }

        static bool GetIsSensorConfigured(DeviceTreeVseObject vseObject, DeviceTreeVseRawData vseRawData)
            => string.Equals(vseObject.InputId, $"{vseRawData.Index}", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(vseObject.InputType, "dynamic", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses <paramref name="vseDevice"/>'s current data into a device tree, waiting for the connection to
    /// finish its first rebuild first if it hasn't already.
    /// </summary>
    public static async Task<DeviceTreeVseDevice> ParseDeviceAsync(VseDevice vseDevice, ILogger logger)
    {
        if (vseDevice.Initialized == false || vseDevice.Connected == false)
        {
            const int MaxWaitTime = 30_000;
            const int RetryInterval = 100;
            const int MaxRetries = MaxWaitTime / RetryInterval;
            int retries = 0;
            using PeriodicTimer timer = new(TimeSpan.FromMilliseconds(RetryInterval));
            do
            {
                await timer.WaitForNextTickAsync();
                retries++;
            } while ((vseDevice.Initialized == false || vseDevice.Connected == false) && retries < MaxRetries);
        }

        DeviceTreeVseDevice vse = new()
        {
            Id = $"vse@{vseDevice.Uri.DnsSafeHost}:{vseDevice.Uri.Port}",
            Description = new DeviceTreeNodeDescription
            {
                Text = "an ifm Vse device",
            },
            MacAddress = "00:00:00:00:00:00",
            Name = "Vse Device",
            Url = vseDevice.Uri,
        };

        if (vseDevice.Initialized && vseDevice.Connected)
        {
            // VseConnection's categories are only null before its very first Rebuild() - Initialized being true
            // here means that has already happened, so these are guaranteed non-null.
            VseConnection connection = vseDevice.Connection;
            return ParseVse(vse, connection.DeviceInfo, connection.DigitalAlarms!, connection.Counters!, connection.Inputs!,
                connection.DeviceObjects!, connection.Variants!, logger).Tree;
        }

        logger.FailedToConnectToVse(vseDevice.Uri);
        vse.Status = ConnectionStatus.Offline;
        return vse;
    }

    /// <summary>
    /// Builds the device tree from a Vse connection's live data categories. Takes the categories directly
    /// (rather than a <see cref="VseDevice"/>/<see cref="VseConnection"/>) so it can be exercised with mocked
    /// categories in isolation, the same way every category in <c>ViciOne.Communication.Vse.Tests</c> already is.
    /// </summary>
    internal static VseParseResult ParseVse(DeviceTreeVseDevice vse, DeviceInfo deviceInfo, DigitalAlarms digitalAlarms,
        Counters counters, Inputs inputs, DeviceObjects deviceObjects, Variants variants, ILogger logger)
    {
        IReadOnlyDictionary<string, object> itemsById = new Dictionary<string, object>();

        // Each category is isolated in its own try/catch - one category throwing (e.g. AddObjects) shouldn't
        // blank out the ones after it (AddRawData, AddActiveVariant, ...) for this rebuild cycle.
        TryAddCategory(() => AddInfos(vse, deviceInfo), nameof(AddInfos), vse, logger);
        TryAddCategory(() => AddAlarms(vse, digitalAlarms), nameof(AddAlarms), vse, logger);
        TryAddCategory(() => AddCounters(vse, counters), nameof(AddCounters), vse, logger);
        TryAddCategory(() => AddExternalInputs(vse, inputs), nameof(AddExternalInputs), vse, logger);
        TryAddCategory(() => AddObjects(vse, deviceObjects, logger), nameof(AddObjects), vse, logger);
        TryAddCategory(() => AddRawData(vse, inputs), nameof(AddRawData), vse, logger);
        TryAddCategory(() => AddActiveVariant(vse, variants), nameof(AddActiveVariant), vse, logger);
        TryAddCategory(() => AddRawDataTriggerEventRelations(vse), nameof(AddRawDataTriggerEventRelations), vse, logger);
        TryAddCategory(() => itemsById = BuildItemsById(digitalAlarms, counters, inputs, deviceObjects, variants), nameof(BuildItemsById), vse, logger);

        return new VseParseResult(vse, itemsById);
    }

    /// <summary>
    /// Builds the address-to-domain-object lookup <see cref="VseParseResult.ItemsById"/> also carries, without
    /// materializing any <c>DeviceTreeVse*</c>/<see cref="DeviceTreeProcessData"/> node objects - what
    /// <see cref="VseDevice"/> actually needs after every rebuild to repoint its live dispatch table at the
    /// freshly (re)created domain collections. Only <see cref="DeviceTreeSubscriber"/> (via
    /// <see cref="ParseDeviceAsync"/>) needs the full tree, so <see cref="VseDevice"/> calls this directly
    /// instead of going through <see cref="ParseVse"/>, skipping that otherwise-unused allocation on every
    /// rebuild.
    /// </summary>
    internal static IReadOnlyDictionary<string, object> BuildItemsById(DigitalAlarms digitalAlarms, Counters counters,
        Inputs inputs, DeviceObjects deviceObjects, Variants variants)
    {
        Dictionary<string, object> itemsById = [];

        // DigitalAlarmSubscriber registers/reads under its own NodePath (the node's own Path) - it receives the
        // whole DigitalAlarm and reads Value/Name off it, there's no separate "/Value" address.
        foreach (DigitalAlarm alarm in digitalAlarms.All)
            itemsById[$"{AddressAlarms}/{AlarmIdentifier(alarm.Id)}"] = alarm;

        // CounterSubscriber registers/reads under its own NodePath (the node's own Path) - it receives the whole
        // Counter and reads Value/Unit/Limit/State/Name off it.
        foreach (Counter counter in counters.All)
            itemsById[$"{AddressCounters}/{CounterIdentifier(counter.Id)}"] = counter;

        // InputSubscriber registers/reads under its own NodePath (the node's own Path) and also writes there
        // directly (InputSubscriber.cs appends "/Value" itself before calling Write) - it receives the whole
        // Input and reads/sets Value on it.
        foreach (Input input in inputs.All)
        {
            if (input.RawType != InputType.External)
                continue;

            string path = $"{AddressExternalInputs}/{ExternalInputIdentifier(input.Id)}";
            itemsById[path] = input;
            itemsById[$"{path}/{IdentifierValue}"] = input;
        }

        // ObjectSubscriber registers/reads under its own NodePath (the node's own Path) - it receives the whole
        // DeviceObject and reads Average/Maximum/.../Name off it; objects are not writable.
        foreach (DeviceObject deviceObject in deviceObjects.All)
            itemsById[$"{AddressObjects}/{ObjectIdentifier(deviceObject.Id)}"] = deviceObject;

        // Two addresses resolve to the same Variants object: VariantsSubscriber registers/reads availability
        // under its own NodePath (the node's own Path), but writes through "<NodePath>/ActiveVariant" - see
        // VariantsSubscriber.cs.
        if (variants.All.Count > 0)
        {
            itemsById[AddressVariants] = variants;
            itemsById[$"{AddressVariants}/{IdentifierActiveVariant}"] = variants;
        }

        return itemsById;
    }

    private static void TryAddCategory(Action addCategory, string categoryName, DeviceTreeVseDevice vse, ILogger logger)
    {
        try
        {
            addCategory();
        }
        catch (Exception ex)
        {
            logger.ParsingVseCategoryFailed(categoryName, vse.Url, ex.Message, ex.StackTrace!);
        }
    }
}
