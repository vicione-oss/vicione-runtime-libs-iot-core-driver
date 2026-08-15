using IFM.SDK.VSE;
using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.EventManager;

internal static partial class DeviceEventManagerLog
{
    [LoggerMessage(EventId = 10000, EventName = "EventEnqueued", Level = LogLevel.Debug, Message = "Device '{DeviceAddress}': event enqueued: {EventType}")]
    public static partial void EventEnqueued(this ILogger logger, Type eventType, string deviceAddress);

    [LoggerMessage(EventId = 10001, EventName = "EventDequeued", Level = LogLevel.Debug, Message = "Device '{DeviceAddress}': event dequeued: {EventType}")]
    public static partial void EventDequeued(this ILogger logger, Type eventType, string deviceAddress);

    [LoggerMessage(EventId = 10002, EventName = "EventDataConnectionState", Level = LogLevel.Debug, Message = "Device '{DeviceAddress}': connection state changed: {Status}")]
    public static partial void EventDataConnectionState(this ILogger logger, ConnectionStatus status, string deviceAddress);

    [LoggerMessage(EventId = 10003, EventName = "EventDataModeChanged", Level = LogLevel.Debug, Message = "Device '{DeviceAddress}': system mode changed: {Mode} (origin: {Origin})")]
    public static partial void EventDataModeChanged(this ILogger logger, SystemMode mode, Origin origin, string deviceAddress);

    [LoggerMessage(EventId = 10004, EventName = "ProcessingEvent", Level = LogLevel.Debug, Message = "Device '{DeviceAddress}': processing event {EventType}")]
    public static partial void ProcessingEvent(this ILogger logger, Type eventType, string deviceAddress);

    [LoggerMessage(EventId = 10005, EventName = "ProcessingTook", Level = LogLevel.Debug, Message = "Device '{DeviceAddress}': processing event {EventType} took {ProcessingTimeMs} ms")]
    public static partial void ProcessingTook(this ILogger logger, Type eventType, double processingTimeMs, string deviceAddress);

    [LoggerMessage(EventId = 10006, EventName = "ProcessingEventFailed", Level = LogLevel.Warning, Message = "Device '{DeviceAddress}': an error occurred while processing {EventType}")]
    public static partial void ProcessingEventFailed(this ILogger logger, Exception ex, Type eventType, string deviceAddress);

    [LoggerMessage(EventId = 10007, EventName = "ProcessEventQueueFailed", Level = LogLevel.Error, Message = "Device '{DeviceAddress}': the event queue could not be processed.")]
    public static partial void ProcessEventQueueFailed(this ILogger logger, Exception ex, string deviceAddress);
}
