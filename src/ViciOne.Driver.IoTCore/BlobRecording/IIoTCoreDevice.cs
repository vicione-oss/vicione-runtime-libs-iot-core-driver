using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.Driver.IoTCore.BlobRecording;

internal interface IIoTCoreDevice : IDisposable
{
    Task<string> SendHttpRequestAsync(HttpRequestMessage request, CancellationToken token);
}
