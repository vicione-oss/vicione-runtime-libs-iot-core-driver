namespace ViciOne.Driver.IoTCore.DcpScan;

internal static class DcpFilterBuilder
{
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
