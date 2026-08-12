using System.Net.NetworkInformation;

namespace ViciOne.Communication.IoTCore.DcpScan;

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
            DcpPacketEncoder.Ethernet(ms, DcpProtocol.PnDcpMulticast, srcMac, DcpProtocol.TpidVlan);
            DcpPacketEncoder.Vlan(ms, priority: 0, innerEtherType: DcpProtocol.EtherTypeProfinet);
        }
        else
        {
            DcpPacketEncoder.Ethernet(ms, DcpProtocol.PnDcpMulticast, srcMac, DcpProtocol.EtherTypeProfinet);
        }
        DcpPacketEncoder.RtFrameId(ms, DcpProtocol.FrameIdIdentifyReq);
        DcpPacketEncoder.DcpIdentifyRequest(ms, xidBe);
        return ms.ToArray();
    }
}
