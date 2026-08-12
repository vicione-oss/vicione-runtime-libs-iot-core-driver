using IFM.SDK.VSE;
using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.EventManager;

internal static partial class DeviceEventManagerLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Event enqueued: {EventType}")]
    public static partial void EventEnqueued(this ILogger logger, Type eventType);
    [LoggerMessage(Level = LogLevel.Debug, Message = "Event dequeued: {EventType}")]
    public static partial void EventDequeued(this ILogger logger, Type eventType);
    [LoggerMessage(Level = LogLevel.Debug, Message = "Event data: Connection state: {Status}")]
    public static partial void EventDataConnectionState(this ILogger logger, ConnectionStatus status);
    [LoggerMessage(Level = LogLevel.Debug, Message = "Event data: Mode: {Mode}, Origin: {Origin}")]
    public static partial void EventDataModeChanged(this ILogger logger, SystemMode mode, Origin origin);
    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing event {EventType}")]
    public static partial void ProcessingEvent(this ILogger logger, Type eventType);
    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing event {EventType} took {ProcessingTimeMs} ms")]
    public static partial void ProcessingTook(this ILogger logger, Type eventType, double processingTimeMs);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception while processing {EventType}")]
    public static partial void ProcessingEventFailed(this ILogger logger, Exception ex, Type eventType);
    [LoggerMessage(Level = LogLevel.Error, Message = "DeviceEventManager failed to process the event queue.")]
    public static partial void ProcessEventQueueFailed(this ILogger logger, Exception ex);
}
