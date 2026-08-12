using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// Shared log messages for the VSE-SDK monitoring channels each collection (<see cref="Counters"/>,
/// <see cref="DeviceObjects"/>, ...) owns.
/// </summary>
internal static partial class MonitoringChannelLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to stop the {ChannelKind} monitoring channel while disposing.")]
    public static partial void StoppingMonitoringChannelFailed(this ILogger logger, Exception ex, string channelKind);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to update the {ChannelKind} monitoring channel for id {ParameterId}.")]
    public static partial void UpdatingMonitoringChannelFailed(this ILogger logger, Exception ex, string channelKind, int parameterId);
}
