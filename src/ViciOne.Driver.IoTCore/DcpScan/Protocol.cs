using System.Net.NetworkInformation;

namespace ViciOne.Driver.IoTCore.DcpScan;

/// <summary>
/// Wire helpers for PN-DCP Identify.
/// </summary>
internal static class Protocol
{
    public const ushort EtherTypeProfinet = 0x8892;
    public const ushort TpidVlan = 0x8100;
    public const ushort TpidQinQ = 0x88A8;
    public const ushort TpidProvider = 0x9100;

    public static readonly PhysicalAddress PnDcpMulticast = PhysicalAddress.Parse("01-0E-CF-00-00-00");

    public const ushort FrameIdIdentifyReq = 0xFEFE;
    public const ushort FrameIdIdentifyRes = 0xFEFF;

    public const byte ServiceIdIdentify = 0x05;
    public const byte ServiceTypeRequest = 0x00;
}
