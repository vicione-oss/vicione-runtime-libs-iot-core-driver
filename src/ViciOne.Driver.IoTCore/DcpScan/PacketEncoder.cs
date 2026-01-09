using System.IO;
using System.Net.NetworkInformation;

namespace ViciOne.Driver.IoTCore.DcpScan;

internal static class PacketEncoder
{
    public static void Ethernet(Stream buffer, PhysicalAddress dst, PhysicalAddress src, ushort type)
    {
        WriteBytes(buffer, dst.GetAddressBytes());
        WriteBytes(buffer, src.GetAddressBytes());
        WriteU16(buffer, type);
    }

    public static void Vlan(Stream buffer, ushort priority, ushort innerEtherType)
    {
        ushort tci = (ushort)((priority & 7) << 13); // DEI=0, VID=0
        WriteU16(buffer, tci);
        WriteU16(buffer, innerEtherType);
    }

    public static void RtFrameId(Stream buffer, ushort value) => WriteU16(buffer, value);

    public static void DcpIdentifyRequest(Stream buffer, uint xidBe)
    {
        // ServiceId/Type 0x05/0x00 encoded as u16 0x0500
        WriteU16(buffer, 0x0500);
        WriteU32(buffer, xidBe);
        WriteU16(buffer, 0x0001); // ResponseDelayFactor
        WriteU16(buffer, 0x0004); // DCPDataLength (AllSelector block follows)
        // AllSelector block: option 0xFFFF, length 0
        WriteU16(buffer, 0xFFFF);
        WriteU16(buffer, 0x0000);
    }

    private static void WriteBytes(Stream s, byte[] data) => s.Write(data, 0, data.Length);
    private static void WriteU16(Stream s, ushort v) { s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }
    private static void WriteU32(Stream s, uint v)
    {
        s.WriteByte((byte)(v >> 24));
        s.WriteByte((byte)(v >> 16));
        s.WriteByte((byte)(v >> 8));
        s.WriteByte((byte)v);
    }
}
