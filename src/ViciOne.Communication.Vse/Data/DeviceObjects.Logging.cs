namespace ViciOne.Communication.Vse.Data;

// Logging helpers for the DeviceObjects class. See MonitoringChannelLog for the actual [LoggerMessage] definitions.
public sealed partial class DeviceObjects
{
    private void LogStoppingMonitoringChannelFailed(Exception ex)
        => _logger?.StoppingMonitoringChannelFailed(ex, "object", _deviceAddress);

    private void LogUpdatingMonitoringChannelFailed(Exception ex, int parameterId)
        => _logger?.UpdatingMonitoringChannelFailed(ex, "object", parameterId, _deviceAddress);
}
