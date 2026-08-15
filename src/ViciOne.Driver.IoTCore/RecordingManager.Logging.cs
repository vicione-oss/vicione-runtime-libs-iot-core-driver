using System;

namespace ViciOne.Driver.IoTCore;

// Logging helpers for the RecordingManager class. See RecordingManagerLog for the actual [LoggerMessage] definitions.
public sealed partial class RecordingManager
{
    private void LogRecordingDataReceived(int devicePort)
        => _logger.RecordingDataReceived(devicePort, _address);

    private void LogRecordingDataWarning(int devicePort, string errorMessage)
        => _logger.RecordingDataWarningOccurred(devicePort, _address, errorMessage);

    private void LogRecordingDataError(Exception ex, int devicePort)
        => _logger.RecordingDataErrorOccurred(ex, devicePort, _address);
}
