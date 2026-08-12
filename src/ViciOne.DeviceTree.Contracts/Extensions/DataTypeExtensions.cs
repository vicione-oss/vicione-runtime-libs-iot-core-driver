namespace ViciOne.DeviceTree.Contracts.Extensions;

/// <summary>
/// Extension members for <see cref="DataType"/>.
/// </summary>
// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class DataTypeExtensions
{
    extension(DataType dataType)
    {
        /// <summary>
        /// Whether this data type's values are suitable for compression (see <see cref="IDeviceTreeCompressableDataNode"/>).
        /// </summary>
        public bool SupportsCompression => dataType switch
        {
            DataType.Flag => true,
            DataType.Whole => true,
            DataType.UnsignedWhole => true,
            DataType.Real => true,
            _ => false,
        };

        /// <summary>
        /// Whether this data type's values are suitable for the live view (see <see cref="IDeviceTreeLiveDataNode"/>).
        /// </summary>
        public bool SupportsLiveView => dataType switch
        {
            DataType.Flag => true,
            DataType.Whole => true,
            DataType.UnsignedWhole => true,
            DataType.Real => true,
            DataType.Text => true,
            _ => false,
        };

        /// <summary>
        /// Whether this data type's values are suitable for logging.
        /// </summary>
        public bool SupportsLogging => dataType switch
        {
            DataType.Flag => true,
            DataType.Whole => true,
            DataType.UnsignedWhole => true,
            DataType.Real => true,
            DataType.Blob => true,
            _ => false,
        };

        /// <summary>
        /// The kind of subscriber FunctionBlock this data type's values should be wired into, or
        /// <see langword="null"/> if no subscriber FunctionBlock supports this data type as a process value.
        /// </summary>
        public SubscriberValueKind? SubscriberKind => dataType switch
        {
            DataType.Flag => SubscriberValueKind.Flag,
            DataType.Whole or DataType.UnsignedWhole or DataType.Real => SubscriberValueKind.Number,
            DataType.Text => SubscriberValueKind.Text,
            _ => null,
        };
    }
}
