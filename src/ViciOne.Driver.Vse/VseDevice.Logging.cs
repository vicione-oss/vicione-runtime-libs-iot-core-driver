using System;

namespace ViciOne.Driver.Vse;

// Logging helpers for the VseDevice class. See VseDeviceLog for the actual [LoggerMessage] definitions.
public sealed partial class VseDevice
{
    private void LogConnectionReEstablished(ref string lastLoggedMessage)
    {
        if (string.IsNullOrWhiteSpace(lastLoggedMessage))
            return;
        lastLoggedMessage = string.Empty;
        _logger.ConnectionReEstablished(DeviceAddress);
    }

    /// <summary>
    /// Logs an action exception, suppressing repeats of the same message to avoid flooding the log while a
    /// condition persists across many cycles.
    /// </summary>
    private void LogError(ref string lastLoggedMessage, Exception ex, string action)
    {
        string error = ex.Message;
        if (string.Equals(lastLoggedMessage, error, StringComparison.OrdinalIgnoreCase))
            return;
        lastLoggedMessage = error;
        _logger.ExecutionErrorOccurred(ex, action, DeviceAddress);
    }
}
