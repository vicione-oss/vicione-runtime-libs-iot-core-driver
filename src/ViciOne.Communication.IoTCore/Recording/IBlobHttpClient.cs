namespace ViciOne.Communication.IoTCore.Recording;

internal interface IBlobHttpClient : IDisposable
{
    Task<string> SendHttpRequestAsync(HttpRequestMessage request, CancellationToken token);
}
