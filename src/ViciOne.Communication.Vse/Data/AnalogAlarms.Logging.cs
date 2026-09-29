namespace ViciOne.Communication.Vse.Data;

// Logging helpers for the AnalogAlarms class. See MonitoringChannelLog for the actual [LoggerMessage] definitions.
public sealed partial class AnalogAlarms
{
    private void LogStoppingMonitoringChannelFailed(Exception ex)
        => _logger?.StoppingMonitoringChannelFailed(ex, "analog alarm", _deviceAddress);
}
