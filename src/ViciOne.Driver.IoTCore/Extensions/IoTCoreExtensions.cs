using System;
using ifmIoTCore;
using ifmIoTCore.Common.Variant;
using ifmIoTCore.Elements;
using ifmIoTCore.Elements.ServiceData.Requests;

namespace ViciOne.Driver.IoTCore.Extensions;

internal static class IoTCoreExtensions
{
    private static T? ExtractValue<T>(VariantObject? variantObject) where T : Variant
    {
        if (variantObject?.TryGetValue(Identifiers.Value, out Variant variant) ?? false)
            return variant as T;
        return null;
    }

    internal static IServiceElement? GetServiceElement(this IIoTCore core, string serviceAddress)
    {
        IBaseElement node = core.GetElementByAddress(serviceAddress);
        if (node is null || node.Address == "/" || node is not IServiceElement element)
            return null;
        return element;
    }

    private static VariantObject? GetData(IIoTCore core, string elementAddress)
    {
        string address = $"{elementAddress}/{Identifiers.GetData}";
        return GetServiceElement(core, address)?.Invoke() as VariantObject;
    }

    public static VariantValue? GetDataValueAsValue(this IIoTCore core, string elementAddress)
        => ExtractValue<VariantValue>(GetData(core, elementAddress));

    public static VariantObject? GetDataValueAsObject(this IIoTCore core, string elementAddress)
        => ExtractValue<VariantObject>(GetData(core, elementAddress));

    public static byte[]? ReadAcyclic(this IIoTCore core, string deviceAddress, int index, int? subIndex = null)
    {
        string address = $"{deviceAddress}/{Identifiers.IolReadAcyclic}";
        var arg = Variant.FromObject(new ReadAcyclicRequestServiceData(index, subIndex ?? 0));
        if (GetServiceElement(core, address)?.Invoke(arg) is not VariantObject variantObject)
            return null;

        VariantValue? extractedValue = ExtractValue<VariantValue>(variantObject);
        if (extractedValue is null)
            return null;

        return Convert.FromHexString(extractedValue.GetString());
    }
}
