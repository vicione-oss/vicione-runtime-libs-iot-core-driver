using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using PacketDotNet;
using SharpPcap;
using ViciOne.Driver.IoTCore.Contracts.Dcp;

namespace ViciOne.Driver.IoTCore.DcpScan;

internal static class DcpPacketParser
{
    private const int EthernetHeaderLength = 14;
    private const int MacAddressLength = 6;
    private const int VlanTagLength = 4;
    private const int MaxVlanTags = 4;
    private const int MinDcpPayloadLength = 12;

    private sealed class Scratch
    {
        public ushort? VendorId;
        public ushort? DeviceId;
        public string? DeviceName;
        public string? AliasName;
        public IPAddress? Ip;
        public IPAddress? Mask;
        public IPAddress? Gateway;
        public PhysicalAddress? Mac;
    }

    public static void TryHandlePacket(
        RawCapture raw,
        byte[] xid,
        int adapterId,
        string adapterName,
        ICollection<DcpDevice> sink,
        Action<string> logInfo,
        Action<string> logDebug,
        bool narrowFilterActive)
    {
        if (raw.LinkLayerType != LinkLayers.Ethernet)
            return;

        byte[] l2 = [.. raw.Data];
        if (!TryGetPnDcpPayload(l2, out ReadOnlySpan<byte> payload))
        {
            if (l2.Length >= EthernetHeaderLength)
            {
                ushort et = ReadU16(l2, 12);
                ConditionalPacketDebug(narrowFilterActive, $"Not PN-DCP. EtherType=0x{et:X4}", logDebug);
            }
            return;
        }

        if (payload.Length < MinDcpPayloadLength)
        {
            ConditionalPacketDebug(narrowFilterActive, "DCP payload too short", logDebug);
            return;
        }

        ushort frameId = BinaryPrimitives.ReadUInt16BigEndian(payload[..2]);
        if (frameId != Protocol.FrameIdIdentifyRes)
        {
            ConditionalPacketDebug(narrowFilterActive, $"Ignore FrameId 0x{frameId:X4}", logDebug);
            return;
        }

        byte serviceId = payload[2];
        byte serviceType = payload[3];
        if (serviceId != Protocol.ServiceIdIdentify || serviceType == Protocol.ServiceTypeRequest)
        {
            ConditionalPacketDebug(narrowFilterActive, "ServiceId/Type mismatch", logDebug);
            return;
        }

        if (!payload.Slice(4, 4).SequenceEqual(xid))
        {
            ConditionalPacketDebug(narrowFilterActive, "XID mismatch", logDebug);
            return;
        }

        ushort dcpLen = BinaryPrimitives.ReadUInt16BigEndian(payload.Slice(10, 2));
        ReadOnlySpan<byte> blocks = payload.Slice(12, Math.Min(dcpLen, payload.Length - 12));

        Scratch parsed = ParseBlocks(blocks, l2, logDebug, narrowFilterActive);
        PhysicalAddress? macOut = parsed.Mac ?? ExtractSourceMac(l2);

        sink.Add(new DcpDevice
        {
            AdapterId = adapterId.ToString(CultureInfo.InvariantCulture),
            AdapterName = adapterName,
            Address = parsed.Ip is not null ? new($"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{parsed.Ip}") : default!,
            DeviceId = parsed.DeviceId ?? 0,
            VendorId = parsed.VendorId ?? 0,
            DeviceName = parsed.DeviceName ?? string.Empty,
            Ip = parsed.Ip?.ToString() ?? string.Empty,
            Netmask = parsed.Mask?.ToString() ?? string.Empty,
            Gateway = parsed.Gateway?.ToString() ?? string.Empty,
            MacAddress = FormatMac(macOut) ?? string.Empty,
        });

        logInfo($"Device: MAC={macOut}, IP={parsed.Ip}, Name={parsed.DeviceName}");
    }

    private static Scratch ParseBlocks(ReadOnlySpan<byte> data, byte[] fullEthernetFrame, Action<string> logDebug, bool narrow)
    {
        var scratch = new Scratch();
        int off = 0;
        while (off + 4 <= data.Length)
        {
            byte opt = data[off + 0];
            byte sub = data[off + 1];
            ushort blen = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(off + 2, 2));
            int valueStart = off + 4;
            int valueEnd = Math.Min(valueStart + blen, data.Length);
            if (valueEnd < valueStart) break;
            ReadOnlySpan<byte> valueSpan = data[valueStart..valueEnd];
            if (valueSpan.Length >= 2) valueSpan = valueSpan[2..];
            int code = (opt << 8) | sub;
            ConditionalPacketDebug(narrow, $"Block 0x{code:X4} (len={blen})", logDebug);
            switch (code)
            {
                case 0x0201:
                    scratch.DeviceName = SafeAscii(valueSpan);
                    break;
                case 0x0202:
                    scratch.AliasName = SafeAscii(valueSpan);
                    break;
                case 0x0203:
                    if (valueSpan.Length >= 2)
                        scratch.VendorId = BinaryPrimitives.ReadUInt16BigEndian(valueSpan[..2]);
                    if (valueSpan.Length >= 4)
                        scratch.DeviceId = BinaryPrimitives.ReadUInt16BigEndian(valueSpan.Slice(2, 2));
                    break;
                case 0x0101:
                    if (valueSpan.Length >= MacAddressLength)
                        scratch.Mac = new PhysicalAddress(valueSpan[..MacAddressLength].ToArray());
                    break;
                case 0x0102:
                case 0x0103:
                    if (valueSpan.Length >= 12)
                    {
                        var ip = new IPAddress(valueSpan[..4].ToArray());
                        var mask = new IPAddress(valueSpan.Slice(4, 4).ToArray());
                        var gw = new IPAddress(valueSpan.Slice(8, 4).ToArray());
                        if (!IsZero(ip))
                        {
                            scratch.Ip = ip; scratch.Mask = mask; scratch.Gateway = gw;
                        }
                    }
                    break;
                default:
                    ConditionalPacketDebug(narrow, $"Unknown block 0x{code:X4} data={ToHex(valueSpan, 32)}", logDebug);
                    break;
            }
            off = valueEnd;
            if ((off & 1) == 1) off++;
        }
        scratch.Mac ??= ExtractSourceMac(fullEthernetFrame);
        return scratch;
    }

    private static bool TryGetPnDcpPayload(byte[] frame, out ReadOnlySpan<byte> payload)
    {
        payload = default;
        if (frame.Length < EthernetHeaderLength) return false;
        int offset = EthernetHeaderLength;
        ushort etherType = ReadU16(frame, 12);
        int tags = 0;
        while ((etherType == Protocol.TpidVlan || etherType == Protocol.TpidQinQ || etherType == Protocol.TpidProvider) && offset + VlanTagLength <= frame.Length && tags < MaxVlanTags)
        {
            etherType = ReadU16(frame, offset + 2);
            offset += VlanTagLength; tags++;
        }
        if (etherType != Protocol.EtherTypeProfinet || offset > frame.Length) return false;
        payload = frame.AsSpan(offset);
        return true;
    }

    private static ushort ReadU16(byte[] buf, int i) => (ushort)((buf[i] << 8) | buf[i + 1]);

    private static PhysicalAddress? ExtractSourceMac(byte[] frame)
    {
        if (frame.Length < 12) return null;
        byte[] src = new byte[MacAddressLength];
        Buffer.BlockCopy(frame, 6, src, 0, MacAddressLength);
        return new(src);
    }

    private static string FormatMac(PhysicalAddress? mac)
    {
        if (mac == null) return string.Empty;
        byte[] bytes = mac.GetAddressBytes();
        return string.Join("-", bytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
    }

    private static string? SafeAscii(ReadOnlySpan<byte> span)
    {
        if (span.Length == 0) return null;
        string s = Encoding.ASCII.GetString(span).TrimEnd('\0');
        string filtered = new([.. s.Where(c => c is >= (char)32 and <= (char)126)]);
        return string.IsNullOrWhiteSpace(filtered) ? null : filtered;
    }

    private static bool IsZero(IPAddress ip) => ip.GetAddressBytes().All(b => b == 0);

    private static string ToHex(ReadOnlySpan<byte> span, int maxLen = -1)
    {
        byte[] data = span.ToArray();
        if (maxLen >= 0 && data.Length > maxLen) data = [.. data.Take(maxLen)];
        var sb = new StringBuilder(data.Length * 2);
        foreach (byte b in data) sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        if (maxLen >= 0 && span.Length > maxLen) sb.Append('…');
        return sb.ToString();
    }

    private static void ConditionalPacketDebug(bool narrowFilterActive, string msg, Action<string> logDebug)
    {
        if (narrowFilterActive || !IsNonDcpNoise(msg))
            logDebug(msg);
    }

    private static bool IsNonDcpNoise(string msg)
        => msg.StartsWith("Ignore FrameId", StringComparison.Ordinal) ||
           msg.StartsWith("ServiceId/Type mismatch", StringComparison.Ordinal) ||
           msg.StartsWith("XID mismatch", StringComparison.Ordinal) ||
           msg.StartsWith("Not PN-DCP", StringComparison.Ordinal);
}
