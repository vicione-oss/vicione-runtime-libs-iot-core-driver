namespace ViciOne.Communication.IoTCore.DcpScan;

/// <summary>
/// Builds BPF (Berkeley Packet Filter) capture filter expressions for PN-DCP Identify frames.
/// </summary>
internal static class DcpFilterBuilder
{
    /// <summary>
    /// Builds a BPF filter expression matching PN-DCP Identify frames - restricted to <paramref name="xidBe"/>
    /// for a narrow filter, or matching any PN-DCP EtherType frame otherwise.
    /// </summary>
    /// <param name="includeVlanFrame">Whether to also match VLAN-tagged frames.</param>
    /// <param name="narrowFilter">Whether to build a narrow filter restricted to <paramref name="xidBe"/>, or a broad EtherType-only filter.</param>
    /// <param name="xidBe">The transaction ID (big-endian) to match when <paramref name="narrowFilter"/> is <see langword="true"/>.</param>
    /// <returns>The BPF filter expression, and whether it is a narrow (transaction-ID-specific) filter.</returns>
    public static (string filter, bool isNarrow) Build(bool includeVlanFrame, bool narrowFilter, uint xidBe)
    {
        if (narrowFilter)
        {
            string xidHex = $"0x{xidBe:X8}";
            if (includeVlanFrame)
            {
                string narrow = $"((vlan and ether proto 0x8892 and ether[18:2] = 0xFEFF and ether[22:4] = {xidHex}) or (ether proto 0x8892 and ether[14:2] = 0xFEFF and ether[18:4] = {xidHex}))";
                return (narrow, true);
            }
            else
            {
                string narrow = $"(ether proto 0x8892 and ether[14:2] = 0xFEFF and ether[18:4] = {xidHex})";
                return (narrow, true);
            }
        }
        else
        {
            if (includeVlanFrame)
            {
                return ("((ether proto 0x8892) or (vlan and ether proto 0x8892))", false);
            }
            return ("(ether proto 0x8892)", false);
        }
    }
}
