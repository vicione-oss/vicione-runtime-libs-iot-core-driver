namespace ViciOne.Communication.Vse.Data;

// Logging helpers for the DigitalAlarms class. See MonitoringChannelLog for the actual [LoggerMessage] definitions.
public sealed partial class DigitalAlarms
{
    private void LogStoppingMonitoringChannelFailed(Exception ex)
        => _logger?.StoppingMonitoringChannelFailed(ex, "digital alarm", _deviceAddress);
}
