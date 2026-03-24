using System;
using ifmIoTCore;
using ifmIoTCore.Common.Variant;
using ifmIoTCore.Elements;
using ifmIoTCore.Elements.ServiceData.Requests;

namespace ViciOne.Driver.IoTCore.Extensions;

//TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/53378 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "IDE0051:Remove unused private member", Justification = "False positive")]
internal static class IoTCoreExtensions
{
    extension(IIoTCore core)
    {
        internal IServiceElement? GetServiceElement(string serviceAddress)
        {
            IBaseElement node = core.GetElementByAddress(serviceAddress);
            if (node is null || node.Address == "/" || node is not IServiceElement element)
                return null;
            return element;
        }

        private VariantObject? GetData(string elementAddress)
        {
            string address = $"{elementAddress}/{Identifiers.GetData}";
            return GetServiceElement(core, address)?.Invoke() as VariantObject;
        }

        public VariantValue? GetDataValueAsValue(string elementAddress)
            => ExtractValue<VariantValue>(core.GetData(elementAddress));

        public VariantObject? GetDataValueAsObject(string elementAddress)
            => ExtractValue<VariantObject>(core.GetData(elementAddress));

        public byte[]? ReadAcyclic(string deviceAddress, int index, int? subIndex = null)
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

    private static T? ExtractValue<T>(VariantObject? variantObject) where T : Variant
    {
        if (variantObject?.TryGetValue(Identifiers.Value, out Variant variant) ?? false)
            return variant as T;
        return null;
    }
}
