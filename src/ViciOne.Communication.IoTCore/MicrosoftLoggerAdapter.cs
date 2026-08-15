using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.IoTCore;

/// <summary>
/// Adapts a <see cref="ILogger"/> to the
/// <see cref="ifm.Logger.Contracts.ILogger"/> that every <c>ifm.IoTCore.*</c> type requires instead.
/// </summary>
public sealed class MicrosoftLoggerAdapter(ILogger logger) : ifm.Logger.Contracts.ILogger
{
    /// <inheritdoc/>
    public void Debug(string message)
        => logger.LogDebug(message);

    /// <inheritdoc/>
    public void Info(string message)
        => logger.LogInformation(message);

    /// <inheritdoc/>
    public void Warning(string message)
        => logger.LogWarning(message);

    /// <inheritdoc/>
    public void Error(string message)
        => logger.LogError(message);

    /// <inheritdoc/>
    public void Error(Exception ex)
        => logger.LogError(ex, ex.Message);
}
