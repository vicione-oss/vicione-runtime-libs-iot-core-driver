using System;

namespace ViciOne.Driver.IoTCore;

// Logging helpers for the IoLinkMaster class. See IoLinkMasterLog for the actual [LoggerMessage] definitions.
public sealed partial class IoLinkMaster
{
    private void LogUnknownDeviceFound(ushort vendorId, uint deviceId)
        => _logger.UnknownDeviceFound(vendorId, deviceId, Uri);

    private void LogMirroringCompleted()
        => _logger.MirroringCompleted(Uri);

    private void LogMirroringError(Exception ex)
        => _logger.MirroringErrorOccurred(ex, Uri);

    private void LogSecurityQueryFailed(Exception ex)
        => _logger.SecurityQueryFailed(ex, Uri);

    private void LogPollingStarted()
        => _logger.PollingStarted(Uri);

    private void LogPollingStopped()
        => _logger.PollingStopped(Uri);

    /// <summary>
    /// Logs that the connection was re-established, but only if a connection error was previously logged.
    /// </summary>
    private void LogConnectionReEstablished()
    {
        if (string.IsNullOrWhiteSpace(_lastLoggedMessage))
            return;
        _lastLoggedMessage = string.Empty;
        _logger.ConnectionReEstablished(Uri);
    }

    /// <summary>
    /// Logs a polling-cycle exception, suppressing repeats of the same message so a condition that persists
    /// across many polling cycles does not flood the log.
    /// </summary>
    private void LogPollingError(Exception ex)
    {
        string error = ex.Message;
        if (string.Equals(_lastLoggedMessage, error, StringComparison.OrdinalIgnoreCase))
            return;
        _lastLoggedMessage = error;
        _logger.PollingErrorOccurred(ex, Uri);
    }
}
