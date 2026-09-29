namespace ViciOne.Communication.IoTCore.Recording;

internal sealed class BlobHttpClient : IBlobHttpClient
{
    private readonly HttpClient _httpClient = new();
    private readonly SemaphoreSlim _mutex = new(1);
    private readonly CancellationTokenSource _cancelRequest = new();

    public CancellationToken Token => _cancelRequest.Token;

    public BlobHttpClient(Uri uri)
        => _httpClient.BaseAddress = uri;

    public void Dispose()
    {
        _httpClient.Dispose();
        _cancelRequest.Dispose();
        _mutex.Dispose();
    }

    public async Task<string> SendHttpRequestAsync(HttpRequestMessage request, CancellationToken token)
    {
        try
        {
            await _mutex.WaitAsync(token).ConfigureAwait(false);

            using var requestToken = CancellationTokenSource.CreateLinkedTokenSource(token, Token);
            HttpResponseMessage response = await _httpClient.SendAsync(request, requestToken.Token).ConfigureAwait(false);
            return await response.Content.ReadAsStringAsync(requestToken.Token).ConfigureAwait(false);
        }
        finally
        {
            _mutex.Release();
        }
    }
}
