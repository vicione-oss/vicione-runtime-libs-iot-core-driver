using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.Data;

internal static partial class MonitoringChannelLog
{
    [LoggerMessage(EventId = 8000, EventName = "StoppingMonitoringChannelFailed", Level = LogLevel.Warning, Message = "Device '{DeviceAddress}': failed to stop the {ChannelKind} monitoring channel while disposing.")]
    public static partial void StoppingMonitoringChannelFailed(this ILogger logger, Exception ex, string channelKind, string deviceAddress);

    [LoggerMessage(EventId = 8001, EventName = "UpdatingMonitoringChannelFailed", Level = LogLevel.Warning, Message = "Device '{DeviceAddress}': failed to update the {ChannelKind} monitoring channel for ID {ParameterId}.")]
    public static partial void UpdatingMonitoringChannelFailed(this ILogger logger, Exception ex, string channelKind, int parameterId, string deviceAddress);
}
