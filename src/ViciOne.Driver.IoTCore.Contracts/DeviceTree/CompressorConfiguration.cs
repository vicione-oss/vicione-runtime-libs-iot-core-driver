namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public sealed class CompressorConfiguration
{
    public int CompressionTime { get; set; }
    public required Guid DataGroupIdentifier { get; set; }
    public bool Enabled { get; set; }
    public PoolingMode PoolingMode { get; set; }
}
