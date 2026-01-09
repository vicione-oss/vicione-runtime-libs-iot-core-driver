namespace ViciOne.Driver.IoTCore.Contracts.DeviceTree;

public sealed class DeviceTreeBlobData : IDeviceTreeBase, IDeviceTreeSchedulableDataNode
{
    public const int ReadBlobDuration = 4_000; //it's always 4 seconds
    public const int ReadBlobSampleRate = 25_000; //hardcoded for VVB

    //Ursprünglich aus ACX.ViciOne.IoTCoreManager.Constants.Identifiers, die ist aber im PoC nur als Datei eingebunden
    //sollten wir möglichst nachziehen wenn wir die Treiber einbinden
    public const string DefaultProcessName = "Recording";

    public List<IDeviceTreeBase> Children { get; set; } = [];
    public DataType DataType { get; } = DataType.BlobT;
    public DeviceTreeNodeDescription? Description { get; set; }
    public bool HasUnit => false;
    public required string Id { get; set; }
    public bool IsNew { get; set; }
    public bool IsOffline { get; set; }
    public bool IsWriteable => false;
    public required string Name { get; set; }
    public List<SchedulerConfiguration> SchedulerConfigurations { get; set; } = [];
    public string SensorType { get; } = "VVB";
    public bool ShowDetailButton { get; set; }
    public bool Visible => true;
}
