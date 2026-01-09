using System;
using System.IO;
using System.Net.Http;

namespace ViciOne.Driver.IoTCore.IoddManagement;

internal static class IoddDownloader
{
    public static MemoryStream DownloadIoddArchiveFile(ushort vendorId, uint deviceId)
    {
        Uri requestUri = new($"https://ioddfinder.io-link.com/api/ioddarchive?vendorId={vendorId}&deviceId={deviceId}");
        using HttpClient httpClient = new();
        using HttpRequestMessage request = new(HttpMethod.Get, requestUri);
        request.Headers.Add("X-API-KEY", "c81e728d-9d4c-2f63-6f06-7f89cc14862c");

        using HttpResponseMessage response = httpClient.Send(request);
        response.EnsureSuccessStatusCode();
        using Stream responseStream = response.Content.ReadAsStream();

        var returnStream = new MemoryStream();
        responseStream.CopyTo(returnStream);

        return returnStream;
    }
}
