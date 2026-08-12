namespace ViciOne.Communication.IoTCore.IoddStore;

/// <summary>
/// Downloads IODD archives from the public IODDfinder API (ioddfinder.io-link.com).
/// </summary>
internal static class IoddDownloader
{
    /// <summary>
    /// Downloads a device's IODD archive (a zip containing its XML files and images) from IODDfinder.
    /// </summary>
    /// <param name="vendorId">The device's vendor ID.</param>
    /// <param name="deviceId">The device's device ID.</param>
    /// <returns>The downloaded archive's raw bytes, as a zip file.</returns>
    /// <exception cref="HttpRequestException">The request failed or IODDfinder returned a non-success status.</exception>
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
