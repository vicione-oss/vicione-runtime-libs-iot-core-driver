using System;
using System.Security.Cryptography.X509Certificates;
using ifm.IoTCore.Contracts;
using ifm.IoTCore.MessageConverter.Json;
using ifm.IoTCore.NetAdapter.Http.Client;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// Builds the client net adapter a connection uses to reach a master, and registers it on the connection.
/// </summary>
internal static class ClientNetAdapters
{
    /// <summary>
    /// Registers the client net adapter for the requested transport on <paramref name="core"/> and returns it
    /// for later disposal. HTTP is left unvalidated; HTTPS accepts the master's certificate rather than
    /// validating it against a trust chain it has no root in, since an authenticating master presents a
    /// self-signed certificate - the confidentiality still comes from the TLS session, the server just is not
    /// authenticated in return.
    /// </summary>
    /// <param name="core">The connection to register the adapter on.</param>
    /// <param name="useHttps">Whether to reach the master over HTTPS.</param>
    /// <param name="timeout">The per-request timeout the adapter uses.</param>
    /// <param name="logger">The logger passed to the HTTP adapter.</param>
    /// <returns>The registered adapter, whose disposal is the caller's responsibility.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Ownership of the created adapter transfers to the caller, which disposes it.")]
    public static IDisposable Register(IIoTCore core, bool useHttps, TimeSpan timeout, ifm.Logger.Contracts.ILogger logger)
    {
        if (useHttps)
        {
            HttpsClientNetAdapterFactory https = new(
                new MessageConverter(), timeout, true,
                (_, _, _, _) => true,
                Array.Empty<X509Certificate2>);
            core.ClientNetAdapterManager.RegisterClientNetAdapterFactory(https);
            return https;
        }

        HttpClientNetAdapterFactory http = new(new MessageConverter(), timeout, true, logger);
        core.ClientNetAdapterManager.RegisterClientNetAdapterFactory(http);
        return http;
    }
}
