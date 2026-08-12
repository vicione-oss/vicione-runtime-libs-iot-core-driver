using System.Net.NetworkInformation;

namespace ViciOne.Communication.IoTCore.DcpScan;

/// <summary>
/// Writes the raw big-endian binary fields that make up a PN-DCP Identify request frame.
/// </summary>
internal static class DcpPacketEncoder
{
    /// <summary>
    /// Writes an Ethernet header (destination/source MAC addresses and EtherType).
    /// </summary>
    /// <param name="buffer">The stream to append the header to.</param>
    /// <param name="dst">The destination MAC address.</param>
    /// <param name="src">The source MAC address.</param>
    /// <param name="type">The EtherType (or, for a VLAN frame, the VLAN TPID).</param>
    public static void Ethernet(Stream buffer, PhysicalAddress dst, PhysicalAddress src, ushort type)
    {
        WriteBytes(buffer, dst.GetAddressBytes());
        WriteBytes(buffer, src.GetAddressBytes());
        WriteU16(buffer, type);
    }

    /// <summary>
    /// Writes an 802.1Q VLAN tag (priority + inner EtherType; DEI and VID are always 0).
    /// </summary>
    /// <param name="buffer">The stream to append the tag to.</param>
    /// <param name="priority">The VLAN priority (only the lowest 3 bits are used).</param>
    /// <param name="innerEtherType">The EtherType of the frame carried inside this VLAN tag.</param>
    public static void Vlan(Stream buffer, ushort priority, ushort innerEtherType)
    {
        ushort tci = (ushort)((priority & 7) << 13); // DEI=0, VID=0
        WriteU16(buffer, tci);
        WriteU16(buffer, innerEtherType);
    }

    /// <summary>
    /// Writes the PROFINET RT FrameID field (e.g. <see cref="DcpProtocol.FrameIdIdentifyReq"/>).
    /// </summary>
    /// <param name="buffer">The stream to append the field to.</param>
    /// <param name="value">The FrameID value to write.</param>
    public static void RtFrameId(Stream buffer, ushort value) => WriteU16(buffer, value);

    /// <summary>
    /// Writes a PN-DCP Identify request body (service ID/type, transaction ID, response delay, and the
    /// AllSelector block that asks the device to report all of its DCP blocks).
    /// </summary>
    /// <param name="buffer">The stream to append the request body to.</param>
    /// <param name="xidBe">The transaction ID (big-endian) devices should echo back in their response.</param>
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
