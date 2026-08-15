using System.Globalization;
using ifm.Common.Variant;
using ifm.IoTCore.Common;
using ifm.IoTCore.Contracts;
using ifm.IoTCore.ElementManager.Contracts.Elements;

namespace ViciOne.Communication.IoTCore.Extensions;

/// <summary>
/// Helpers for reading process/service data from an <see cref="IIoTCore"/> device tree.
/// </summary>
//TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/53378 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "IDE0051:Remove unused private member", Justification = "False positive")]
// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class IoTCoreExtensions
{
    // Not exposed by the new SDK's own Identifiers class (which only covers generic IoTCore service names) -
    // verified live against a real IO-Link master that these literal wire identifiers are unchanged.
    private const string IolReadAcyclicIdentifier = "iolreadacyclic";
    private const string ValueKey = "value";
    // The "getdatamulti" request lists the addresses to read under "datatosend"; each answered address maps to
    // an object carrying the read value under "data" - verified live against a real IO-Link master.
    private const string GetDataMultiIdentifier = "getdatamulti";
    private const string DataToSendKey = "datatosend";
    private const string DataKey = "data";

    extension(IIoTCore core)
    {
        /// <summary>
        /// Resolves <paramref name="serviceAddress"/> to a service element, if it exists and is actually
        /// invocable (not the tree root).
        /// </summary>
        /// <param name="serviceAddress">The element address to resolve.</param>
        /// <returns>The resolved service element, or <see langword="null"/> if it doesn't exist or isn't a service.</returns>
        internal IServiceElement? GetServiceElement(string serviceAddress)
        {
            IBaseElement node = core.ElementManager.GetElementByAddress(serviceAddress, acquireLock: true);
            if (node is null || node.Address == "/" || node is not IServiceElement element)
                return null;
            return element;
        }

        /// <summary>
        /// Invokes the "getdata" service under <paramref name="elementAddress"/> and returns its raw result.
        /// </summary>
        /// <param name="elementAddress">The element address whose data to read.</param>
        /// <returns>The service's raw result, or <see langword="null"/> if the service doesn't exist.</returns>
        private VariantObject? GetData(string elementAddress)
        {
            string address = $"{elementAddress}/{Identifiers.GetData}";
            return GetServiceElement(core, address)?.Invoke(null!, new ServiceInfo()).AsVariantObject();
        }

        /// <summary>
        /// Reads the current value at <paramref name="elementAddress"/> as a scalar <see cref="VariantValue"/>.
        /// </summary>
        /// <param name="elementAddress">The element address whose data to read.</param>
        /// <returns>The current value, or <see langword="null"/> if it couldn't be read.</returns>
        public VariantValue? GetDataValueAsValue(string elementAddress)
            => ExtractValue<VariantValue>(core.GetData(elementAddress));

        /// <summary>
        /// Reads the current value at <paramref name="elementAddress"/> as a structured <see cref="VariantObject"/>.
        /// </summary>
        /// <param name="elementAddress">The element address whose data to read.</param>
        /// <returns>The current value, or <see langword="null"/> if it couldn't be read.</returns>
        public VariantObject? GetDataValueAsObject(string elementAddress)
            => ExtractValue<VariantObject>(core.GetData(elementAddress));

        /// <summary>
        /// Reads several element values in one "getdatamulti" request instead of one request per element.
        /// </summary>
        /// <param name="deviceAddress">The address of the device whose "getdatamulti" service to invoke.</param>
        /// <param name="elementAddresses">The element addresses to read, in the space the invoked service expects.</param>
        /// <returns>The read values keyed by their element address; an address the service did not answer with a value is absent.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="deviceAddress"/> exposes no "getdatamulti" service.</exception>
        public Dictionary<string, VariantValue> GetDataMulti(string deviceAddress, IReadOnlyCollection<string> elementAddresses)
        {
            string serviceAddress = $"{deviceAddress}/{GetDataMultiIdentifier}";
            IServiceElement service = GetServiceElement(core, serviceAddress)
                ?? throw new InvalidOperationException($"No '{GetDataMultiIdentifier}' service exists at '{serviceAddress}'.");

            VariantArray addressesToSend = [];
            foreach (string elementAddress in elementAddresses)
                addressesToSend.Add(new VariantValue(elementAddress));
            VariantObject request = new() { { DataToSendKey, addressesToSend } };

            VariantObject response = service.Invoke(request, new ServiceInfo()).AsVariantObject();
            Dictionary<string, VariantValue> values = new(StringComparer.Ordinal);
            foreach (string elementAddress in elementAddresses)
            {
                if (response.TryGetValue(elementAddress, out Variant? entry) &&
                    entry.AsVariantObject().TryGetValue(DataKey, out Variant? data) &&
                    data is VariantValue value)
                    values[elementAddress] = value;
            }
            return values;
        }

        /// <summary>
        /// Reads an ISDU value from <paramref name="deviceAddress"/> via its "iolreadacyclic" service.
        /// </summary>
        /// <param name="deviceAddress">The IO-Link device element address to read from.</param>
        /// <param name="index">The ISDU index to read.</param>
        /// <param name="subIndex">The ISDU sub-index to read, or <see langword="null"/> for index 0.</param>
        /// <returns>The decoded (hex-encoded) response bytes, or <see langword="null"/> if the read failed.</returns>
        public byte[]? ReadAcyclic(string deviceAddress, int index, int? subIndex = null)
        {
            string address = $"{deviceAddress}/{IolReadAcyclicIdentifier}";
            Variant arg = Variant.FromObject(new ReadAcyclicRequest(index, subIndex ?? 0));
            VariantObject? variantObject = GetServiceElement(core, address)?.Invoke(arg, new ServiceInfo()).AsVariantObject();
            if (variantObject is null)
                return null;

            VariantValue? extractedValue = ExtractValue<VariantValue>(variantObject);
            if (extractedValue is null)
                return null;

            return Convert.FromHexString(extractedValue.ToString());
        }
    }

    extension(VariantValue value)
    {
        /// <summary>
        /// Reads <paramref name="value"/> as a string.
        /// </summary>
        /// <returns>The value's string representation.</returns>
        public string GetString()
            => value.ToString();

        /// <summary>
        /// Reads <paramref name="value"/> as an 8-bit unsigned integer.
        /// </summary>
        /// <returns>The value converted to <see cref="byte"/>.</returns>
        public byte GetUInt8()
            => Convert.ToByte(value.Value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads <paramref name="value"/> as a 16-bit unsigned integer.
        /// </summary>
        /// <returns>The value converted to <see cref="ushort"/>.</returns>
        public ushort GetUInt16()
            => Convert.ToUInt16(value.Value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads <paramref name="value"/> as a 32-bit unsigned integer.
        /// </summary>
        /// <returns>The value converted to <see cref="uint"/>.</returns>
        public uint GetUInt32()
            => Convert.ToUInt32(value.Value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Reads <paramref name="value"/> as a 32-bit signed integer.
        /// </summary>
        /// <returns>The value converted to <see cref="int"/>.</returns>
        public int GetInt32()
            => Convert.ToInt32(value.Value, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The payload shape the "iolreadacyclic" service expects - verified live against a real IO-Link master.
    /// </summary>
    private sealed record ReadAcyclicRequest(int Index, int SubIndex);

    /// <summary>
    /// Reads the "value" entry out of a service result and casts it to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="variantObject">The service result to read from.</param>
    /// <returns>The value entry, or <see langword="null"/> if it's missing or not a <typeparamref name="T"/>.</returns>
    private static T? ExtractValue<T>(VariantObject? variantObject) where T : Variant
    {
        if (variantObject?.TryGetValue(ValueKey, out Variant variant) ?? false)
            return variant as T;
        return null;
    }
}
