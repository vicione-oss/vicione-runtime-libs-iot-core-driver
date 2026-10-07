using System.Net.Sockets;

namespace ViciOne.Communication.Vse.Tests;

public class VseConnectionTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.0.10")]
    [InlineData("::1")]
    public void ResolveIpAddress_IpAddress_ReturnedUnchanged(string ip)
        => Assert.Equal(ip, VseConnection.ResolveIpAddress(ip));

    [Fact]
    public void ResolveIpAddress_Localhost_PrefersIPv4()
        => Assert.Equal("127.0.0.1", VseConnection.ResolveIpAddress("localhost"));

    [Fact]
    public void ResolveIpAddress_UnknownHost_Throws()
        // ".invalid" is reserved (RFC 2606) and never resolves.
        => Assert.ThrowsAny<SocketException>(() => VseConnection.ResolveIpAddress("vse.invalid"));
}
