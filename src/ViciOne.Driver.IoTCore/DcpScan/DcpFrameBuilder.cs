using System.IO;
using System.Net.NetworkInformation;

namespace ViciOne.Driver.IoTCore.DcpScan;

/// <summary>
/// Builds DCP related raw Ethernet frames.
/// </summary>
internal static class DcpFrameBuilder
{
    public static byte[] BuildIdentifyFrame(PhysicalAddress srcMac, uint xidBe, bool vlanFrame)
    {
        using var ms = new MemoryStream();
        if (vlanFrame)
        {
            PacketEncoder.Ethernet(ms, Protocol.PnDcpMulticast, srcMac, Protocol.TpidVlan);
            PacketEncoder.Vlan(ms, priority: 0, innerEtherType: Protocol.EtherTypeProfinet);
        }
        else
        {
            PacketEncoder.Ethernet(ms, Protocol.PnDcpMulticast, srcMac, Protocol.EtherTypeProfinet);
        }
        PacketEncoder.RtFrameId(ms, Protocol.FrameIdIdentifyReq);
        PacketEncoder.DcpIdentifyRequest(ms, xidBe);
        return ms.ToArray();
    }
}
