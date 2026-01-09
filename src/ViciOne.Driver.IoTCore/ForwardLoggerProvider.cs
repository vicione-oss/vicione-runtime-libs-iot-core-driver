using Microsoft.Extensions.Logging;

namespace ViciOne.Driver.IoTCore;

internal class ForwardLoggerProvider(ILogger sink) : ILoggerProvider
{
    private readonly ILogger _sink = sink;

    public ILogger CreateLogger(string categoryName)
        => _sink;

    public void Dispose() { }
}
