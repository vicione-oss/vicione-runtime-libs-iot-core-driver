namespace ViciOne.Communication.Vse.Data;

// Logging helpers for the Inputs class. See MonitoringChannelLog for the actual [LoggerMessage] definitions.
public sealed partial class Inputs
{
    private void LogStoppingMonitoringChannelFailed(Exception ex)
        => _logger?.StoppingMonitoringChannelFailed(ex, "input", _deviceAddress);
}
