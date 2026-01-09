using ifmIoTCore;
using ifmIoTCore.Common.Variant;
using ifmIoTCore.Elements;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.Driver.IoTCore.Contracts.Constants;
using ViciOne.Driver.IoTCore.Contracts.DeviceTree;

namespace ViciOne.Driver.IoTCore;

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
    public const string IdentifierUnit = "Unit";
    public const string IdentifierValue = "Value";
    public const string IdentifierWarning = "Warning";

    // TODO: Separator ist im DeviceManagement-Modul als Konstante hinterlegt. Eventuell sollte diese Referenz-Richtung geändert werden, aber nur wenn der DeviceTree das Übertragungsmodell bleibt
    public const string IdSeparatorNameAlias = "__!__";

    internal static void AddActiveVariant(DeviceTreeVseDevice vse, IIoTCore core)
    {
        string variantsId = $"{vse.Id}/{NodeNames.Variants}";

        IBaseElement node = core.GetElementByAddress(VseAddresses.Variants);
        if ((core.GetElementByAddress($"{VseAddresses.Variants}/{VseAddresses.GetData}") as IServiceElement)!.Invoke(null, null) is not VariantObject)
            return;

        var variantsDeviceTreeNode = new DeviceTreeVseVariants()
        {
            Name = NodeNames.Variants,
            Id = variantsId,
            Path = node.Address,
            Children =
            [
                new DeviceTreeProcessData()
                {
                    Name = IdentifierActiveVariant,
                    DataType = DataType.IntegerT,
                    Id = $"{variantsId}/{IdentifierActiveVariant}",
                    IsWriteable = true,
                    StructureUnit = string.Empty,
                },
            ]
        };

        vse.Children.Add(variantsDeviceTreeNode);
    }

    internal static void AddAlarms(DeviceTreeVseDevice vse, IIoTCore core)
    {
        var deviceTreeNode = new DeviceTreeStructureNode()
        {
            Name = NodeNames.Alarms,
            Id = $"{vse.Id}/{NodeNames.Alarms}",
        };

        vse.Children.Add(deviceTreeNode);

        IBaseElement node = core.GetElementByAddress(VseAddresses.Alarms);
        if ((core.GetElementByAddress($"{VseAddresses.Alarms}/{VseAddresses.GetData}") as IServiceElement)!.Invoke(null, null) is not VariantObject variant)
            return;

        if (variant.TryGetValue("value", out Variant value) == false ||
            value is not VariantObject values)
            return;

        foreach (IDataElement child in node.Subs.OfType<IDataElement>())
        {
            if (values.TryGetValue(child.Identifier, out Variant element) == false ||
                element is not VariantObject elementObject)
                continue;

            string name = elementObject[IoTCoreProperties.Name].ToObject<string>();
            string id = $"{deviceTreeNode.Id}/{child.Identifier}{IdSeparatorNameAlias}{name}";

            deviceTreeNode.Children.Add(new DeviceTreeVseAlarm()
            {
                Alias = name,
                Name = child.Identifier,
                Id = id,
                Path = child.Address,
                Type = elementObject[IoTCoreProperties.Type].ToObject<string>(),
                Children =
                [
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierValue,
                        DataType = DataType.BooleanT,
                        Id = $"{id}/{IdentifierValue}",
                    },
                    new DeviceTreeConstantData()
                    {
                        Name = IdentifierName,
                        DataType = DataType.StringT,
                        Id = $"{id}/{IdentifierName}",
                        Value = name,
                        Visible = false,
                    }
                ]
            });
        }
    }

    internal static void AddCounters(DeviceTreeVseDevice vse, IIoTCore core)
    {
        var deviceTreeNode = new DeviceTreeStructureNode()
        {
            Name = NodeNames.Counters,
            Id = $"{vse.Id}/{NodeNames.Counters}",
        };

        vse.Children.Add(deviceTreeNode);

        IBaseElement node = core.GetElementByAddress(VseAddresses.Counters);
        if ((core.GetElementByAddress($"{VseAddresses.Counters}/{VseAddresses.GetData}") as IServiceElement)!.Invoke(null, null) is not VariantObject variant)
            return;

        if (variant.TryGetValue("value", out Variant value) == false ||
            value is not VariantObject values)
            return;

        foreach (IDataElement child in node.Subs.OfType<IDataElement>())
        {
            if (values.TryGetValue(child.Identifier, out Variant element) == false ||
                element is not VariantObject elementObject)
                continue;

            string name = elementObject[IoTCoreProperties.Name].ToObject<string>();
            string id = $"{deviceTreeNode.Id}/{child.Identifier}{IdSeparatorNameAlias}{name}";
            string unit = elementObject[IoTCoreProperties.Unit].ToObject<string>();

            deviceTreeNode.Children.Add(new DeviceTreeVseCounter()
            {
                Alias = name,
                Name = child.Identifier,
                Id = id,
                Path = child.Address,
                Type = elementObject[IoTCoreProperties.Type].ToObject<string>(),
                Unit = unit,
                Children =
                [
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierValue,
                        DataType = DataType.Float32T,
                        HasUnit = true,
                        Id = $"{id}/{IdentifierValue}",
                        StructureUnit = unit,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierUnit,
                        DataType = DataType.StringT,
                        Id = $"{id}/{IdentifierUnit}",
                        Visible = false,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierLimit,
                        DataType = DataType.Float32T,
                        Id = $"{id}/{IdentifierLimit}",
                        StructureUnit = string.Empty, //Todo: check? is the limit not of the same unit as the value
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierState,
                        DataType = DataType.StringT,
                        Id = $"{id}/{IdentifierState}",
                    },
                    new DeviceTreeConstantData()
                    {
                        Name = IdentifierName,
                        DataType = DataType.StringT,
                        Id = $"{id}/{IdentifierName}",
                        Value = name,
                        Visible = false,
                    }
                ]
            });
        }
    }

    internal static void AddExternalInputs(DeviceTreeVseDevice vse, IIoTCore core)
    {
        var deviceTreeInputs = new DeviceTreeStructureNode()
        {
            Name = NodeNames.Inputs,
            Id = $"{vse.Id}/{NodeNames.Inputs}",
        };

        vse.Children.Add(deviceTreeInputs);

        IBaseElement node = core.GetElementByAddress(VseAddresses.ExternalInputs);
        if (node?.Subs is null)
            return;

        if ((core.GetElementByAddress($"{VseAddresses.ExternalInputs}/{VseAddresses.GetData}") as IServiceElement)!.Invoke(null, null) is not VariantObject variant)
            return;

        if (variant.TryGetValue("value", out Variant value) == false ||
            value is not VariantObject values)
            return;

        var deviceTreeExternal = new DeviceTreeStructureNode()
        {
            Name = NodeNames.External,
            Id = $"{deviceTreeInputs.Id}/{NodeNames.External}",
        };

        deviceTreeInputs.Children.Add(deviceTreeExternal);

        foreach (IDataElement inputNode in node.Subs.OfType<IDataElement>())
        {
            if (values.TryGetValue(inputNode.Identifier, out Variant element) == false ||
                element is not VariantObject elementObject)
                continue;

            string name = elementObject[IoTCoreProperties.Name].ToObject<string>();
            string id = $"{deviceTreeExternal.Id}/{inputNode.Identifier}{IdSeparatorNameAlias}{name}";
            string unit = elementObject[IoTCoreProperties.Unit].ToObject<string>();

            deviceTreeExternal.Children.Add(new DeviceTreeVseInput()
            {
                Alias = name,
                Name = inputNode.Identifier,
                Id = id,
                InputId = elementObject[IoTCoreProperties.Id].ToObject<int>(),
                Unit = unit,
                Path = inputNode.Address,
                Children =
                [
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierValue,
                        DataType = DataType.Float32T,
                        Id = $"{id}/{IdentifierValue}",
                        HasUnit = true,
                        IsWriteable = true,
                        StructureUnit = unit,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierUnit,
                        DataType = DataType.StringT,
                        Id = $"{id}/{IdentifierUnit}",
                        Visible = false,
                    },
                    new DeviceTreeConstantData()
                    {
                        Name = IdentifierName,
                        DataType = DataType.StringT,
                        Id = $"{id}/{IdentifierName}",
                        Value = name,
                        Visible = false,
                    }
                ]
            });
        }
    }

    internal static void AddInfos(DeviceTreeVseDevice vse, IIoTCore core)
    {
        // IoTCore-10.45.24.101:3321/Device/Documentation/getdata

        if (core.GetElementByAddress(VseAddresses.VseInformationGetData) is not IServiceElement serviceElement ||
            serviceElement.Invoke(null, null) is not VariantObject variant)
            return;

        if (variant.TryGetValue("value", out Variant value) == false ||
            value is not VariantObject values)
            return;

        vse.SoftwareRevision = values[IoTCoreProperties.Firmware].ToString();
        vse.HardwareRevision = values[IoTCoreProperties.Revision].ToString();
        vse.SerialNumber = values[IoTCoreProperties.Serial].ToString();
        vse.Manufacturer = "ifm electronic GmbH";
        vse.ProductName = values[IoTCoreProperties.Type].ToString();
        vse.DeviceFamily = "VSE";
        vse.Name = $"{values[IoTCoreProperties.Type]} - {values[IoTCoreProperties.Name]}";
        vse.Description = null;

        variant = ((core.GetElementByAddress(VseAddresses.VseMacAddressGetData) as IServiceElement)!.Invoke(null, null) as VariantObject)!;
        if (variant is null)
            return;

        if (variant.TryGetValue("value", out value) == false ||
            value is not VariantValue macAddress)
            return;

        vse.MacAddress = macAddress.ToObject<string>();
    }

    internal static void AddObjects(DeviceTreeVseDevice vse, IIoTCore core, ILogger logger)
    {
        var deviceTreeNode = new DeviceTreeStructureNode()
        {
            Name = NodeNames.Objects,
            Id = $"{vse.Id}/{NodeNames.Objects}",
        };

        vse.Children.Add(deviceTreeNode);

        IBaseElement node = core.GetElementByAddress(VseAddresses.Objects);
        if ((core.GetElementByAddress($"{VseAddresses.Objects}/{VseAddresses.GetData}") as IServiceElement)!.Invoke(null, null) is not VariantObject variant)
            return;

        if (variant.TryGetValue("value", out Variant value) == false ||
            value is not VariantObject values)
            return;

        foreach (IDataElement objectNode in node.Subs.OfType<IDataElement>())
        {
            if (values.TryGetValue(objectNode.Identifier, out Variant element) == false ||
                element is not VariantObject elementObject)
                continue;

            string name = elementObject[IoTCoreProperties.Name].ToObject<string>();
            string id = $"{deviceTreeNode.Id}/{objectNode.Identifier}{IdSeparatorNameAlias}{name}";
            string unit = elementObject[IoTCoreProperties.Unit].ToObject<string>();

            if (string.IsNullOrEmpty(unit))
                logger.NoUnitSet(name, vse.Url);

            deviceTreeNode.Children.Add(new DeviceTreeVseObject()
            {
                Alias = name,
                Name = objectNode.Identifier,
                Id = id,
                InputType = elementObject[IoTCoreProperties.InputType].ToObject<string>(),
                InputId = elementObject[IoTCoreProperties.InputId].ToObject<string>(),
                ObjectId = elementObject[IoTCoreProperties.ObjectId].ToObject<string>(),
                Path = objectNode.Address,
                Type = elementObject[IoTCoreProperties.Type].ToObject<string>(),
                Unit = unit,
                Children =
                [
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierAverage,
                        DataType = DataType.Float32T,
                        HasUnit = true,
                        Id = $"{id}/{IdentifierAverage}",
                        StructureUnit = unit,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierMaximum,
                        DataType = DataType.Float32T,
                        HasUnit = true,
                        Id = $"{id}/{IdentifierMaximum}",
                        StructureUnit = unit,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierMinimum,
                        DataType = DataType.Float32T,
                        HasUnit = true,
                        Id = $"{id}/{IdentifierMinimum}",
                        StructureUnit = unit,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierUnit,
                        DataType = DataType.StringT,
                        Id = $"{id}/{IdentifierUnit}",
                        Visible = false,
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierRotSpeed,
                        DataType = DataType.Float32T,
                        Id = $"{id}/{IdentifierRotSpeed}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierRefValue,
                        DataType = DataType.Float32T,
                        Id = $"{id}/{IdentifierRefValue}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierWarning,
                        DataType = DataType.Float32T,
                        Id = $"{id}/{IdentifierWarning}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierBaseLine,
                        DataType = DataType.Float32T,
                        Id = $"{id}/{IdentifierBaseLine}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierDamage,
                        DataType = DataType.Float32T,
                        Id = $"{id}/{IdentifierDamage}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierAveragingPeriod,
                        DataType = DataType.Float32T,
                        Id = $"{id}/{IdentifierAveragingPeriod}",
                    },
                    new DeviceTreeProcessData()
                    {
                        Name = IdentifierError,
                        DataType = DataType.StringT,
                        Id = $"{id}/{IdentifierError}",
                    },
                    new DeviceTreeConstantData()
                    {
                        Name = IdentifierName,
                        DataType = DataType.StringT,
                        Id = $"{id}/{IdentifierName}",
                        Value = name,
                        Visible = false,
                    },
                ]
            });
        }
    }

    internal static void AddRawData(DeviceTreeVseDevice vse, IIoTCore core)
    {
        var rawDataDeviceNode = new DeviceTreeStructureNode()
        {
            Name = NodeNames.RawData,
            Id = $"{vse.Id}/{NodeNames.RawData}",
        };

        vse.Children.Add(rawDataDeviceNode);

        if ((core.GetElementByAddress(VseAddresses.DynamicInputsGetData) as IServiceElement)!.Invoke(null, null) is not VariantObject variant)
            return;

        if (variant.TryGetValue("value", out Variant value) == false ||
            value is not VariantObject values)
            return;

        foreach (KeyValuePair<Variant, Variant> rawDataValuePair in values)
        {
            if (rawDataValuePair.Value is not VariantObject elementObject)
                continue;

            string name = elementObject[IoTCoreProperties.Name].ToObject<string>();

            rawDataDeviceNode.Children.Add(new DeviceTreeVseRawData()
            {
                Name = name,
                Id = $"{rawDataDeviceNode.Id}/{name}",
                Index = elementObject[IoTCoreProperties.Id].ToObject<int>(),
                Unit = string.Empty,
            });
        }
    }

    private static void AddRawDataTriggerEventRelations(DeviceTreeVseDevice vse)
    {
        DeviceTreeVseObject[]? vseObjects = vse.Children.FirstOrDefault(c => c.Name == NodeNames.Objects)?.Children.Cast<DeviceTreeVseObject>().ToArray();
        if (vseObjects is null ||
            vseObjects.Length == 0)
            return;

        DeviceTreeVseRawData[]? vseRawDatas = vse.Children.FirstOrDefault(c => c.Name == NodeNames.RawData)?.Children.Cast<DeviceTreeVseRawData>().ToArray();
        if (vseRawDatas is null ||
            vseRawDatas.Length == 0)
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1508:Toten bedingten Code vermeiden", Justification = "Das warten auf die Initialisierung muss nur gestartet werden, wenn das Gerät nicht bereits initialisiert ist.")]
    public static async Task<DeviceTreeVseDevice> ParseDeviceAsync(VseDevice vseDevice, ILogger logger)
    {
        // TODO: Warten auf die Initialisierung in Erweiterungsmethode auslagern
        if (vseDevice.Initialized == false || vseDevice.Connected == false)
        {
            const int MaxWaitTime = 30_000;
            const int RetryInterval = 100;
            const int MaxRetries = MaxWaitTime / RetryInterval;
            int retries = 0;
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(RetryInterval));
            do
            {
                await timer.WaitForNextTickAsync();
                retries++;
            } while ((vseDevice.Initialized == false || vseDevice.Connected == false) && retries < MaxRetries);
        }

        var vse = new DeviceTreeVseDevice
        {
            Id = $"vse@{vseDevice.Uri.DnsSafeHost}:{vseDevice.Uri.Port}",
            Description = new DeviceTreeNodeDescription
            {
                Text = "an ifm VSE device",
            },
            MacAddress = "00:00:00:00:00:00",
            Name = "VSE Device",
            Url = vseDevice.Uri,
        };

        if (vseDevice.Initialized && vseDevice.Connected)
        {
            return ParseVse(vse, vseDevice.Core, logger);
        }
        else
        {
            logger.FailedToConnectToVse(vseDevice.Uri);

            vse.IsOffline = true;
            return vse;
        }
    }

    internal static DeviceTreeVseDevice ParseVse(DeviceTreeVseDevice vse, IIoTCore core, ILogger logger)
    {
        try
        {
            AddInfos(vse, core);
            AddAlarms(vse, core);
            AddCounters(vse, core);
            AddExternalInputs(vse, core);
            AddObjects(vse, core, logger);
            AddRawData(vse, core);
            AddActiveVariant(vse, core);
            AddRawDataTriggerEventRelations(vse);
        }
        catch (Exception ex)
        {
            logger.ParsingVseFailed(vse.Url, ex.Message, ex.StackTrace!);
        }

        return vse;
    }

    internal static string[] GetRelevantEntries(IIoTCore core)
    {
        return
            [
                .. core.GetElementByAddress(VseAddresses.Alarms)?.Subs.OfType<IDataElement>().Select(x => $"{x.Address}{IdSeparatorNameAlias}{GetElementName(x)}") ?? [],
                .. core.GetElementByAddress(VseAddresses.Counters)?.Subs.OfType<IDataElement>().Select(x => $"{x.Address}{IdSeparatorNameAlias}{GetElementName(x)}") ?? [],
                .. core.GetElementByAddress(VseAddresses.ExternalInputs)?.Subs.OfType<IDataElement>().Select(x => $"{x.Address}{IdSeparatorNameAlias}{GetElementName(x)}") ?? [],
                .. core.GetElementByAddress(VseAddresses.Objects)?.Subs.OfType<IDataElement>().Select(x => $"{x.Address}{IdSeparatorNameAlias}{GetElementName(x)}{IdSeparatorNameAlias}{GetInputId(x)}") ?? [],
                .. (core.GetElementByAddress(VseAddresses.DynamicInputs) as IDataElement)?.Value?.AsVariantObject()?.Select(x => $"{VseAddresses.DynamicInputs}{IdSeparatorNameAlias}{GetRawDataElementName(x.Value)}") ?? [],
            ];

        static string GetElementName(IDataElement element)
            => (element.GetElementByIdentifier(IoTCoreProperties.Name) as IDataElement)?.Value?.ToObject<string>() ?? string.Empty;

        static string GetInputId(IDataElement element)
            => (element.GetElementByIdentifier(IoTCoreProperties.InputId) as IDataElement)?.Value?.ToObject<string>() ?? string.Empty;

        static string GetRawDataElementName(Variant variant)
            => variant.AsVariantObject()?[IoTCoreProperties.Name].ToObject<string>() ?? string.Empty;
    }
}
