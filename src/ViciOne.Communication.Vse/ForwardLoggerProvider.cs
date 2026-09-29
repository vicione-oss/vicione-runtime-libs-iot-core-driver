using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse;

/// <summary>
/// Wraps a single <see cref="ILogger"/> as an <see cref="ILoggerFactory"/> that always hands back that same
/// sink, regardless of the requested category - lets <see cref="VseConnection"/> accept a plain logger from its
/// caller while still satisfying the device APIs that only take a factory.
/// </summary>
internal sealed class ForwardLoggerProvider(ILogger sink) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => sink;

    public void Dispose() { }
}
