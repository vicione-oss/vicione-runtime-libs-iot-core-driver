namespace ViciOne.Driver.IoTCore.Contracts.Dcp;

public sealed class DcpScanningResult
{
    public List<DcpDevice> Devices { get; set; } = [];
    public List<string> Messages { get; set; } = [];
}
