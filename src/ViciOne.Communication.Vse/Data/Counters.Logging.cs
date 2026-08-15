namespace ViciOne.Communication.Vse.Data;

// Logging helpers for the Counters class. See MonitoringChannelLog for the actual [LoggerMessage] definitions.
public sealed partial class Counters
{
    private void LogStoppingMonitoringChannelFailed(Exception ex)
        => _logger?.StoppingMonitoringChannelFailed(ex, "counter", _deviceAddress);
}
