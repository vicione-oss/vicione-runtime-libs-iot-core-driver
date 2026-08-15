using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.EventManager;

// Logging helpers for the DeviceEventManager class. See DeviceEventManagerLog for the actual [LoggerMessage] definitions.
public sealed partial class DeviceEventManager
{
    private void LogEventEnqueued(Type eventType)
        => _logger?.EventEnqueued(eventType, DeviceAddress);

    private void LogEventDequeued(Type eventType)
        => _logger?.EventDequeued(eventType, DeviceAddress);

    private void LogEventDataConnectionState(ConnectionStatus status)
        => _logger?.EventDataConnectionState(status, DeviceAddress);

    private void LogEventDataModeChanged(SystemMode mode, Origin origin)
        => _logger?.EventDataModeChanged(mode, origin, DeviceAddress);

    private void LogProcessingEvent(Type eventType)
        => _logger?.ProcessingEvent(eventType, DeviceAddress);

    private void LogProcessingTook(Type eventType, double processingTimeMs)
        => _logger?.ProcessingTook(eventType, processingTimeMs, DeviceAddress);

    private void LogProcessingEventFailed(Exception ex, Type eventType)
        => _logger?.ProcessingEventFailed(ex, eventType, DeviceAddress);

    private void LogProcessEventQueueFailed(Exception ex)
        => _logger?.ProcessEventQueueFailed(ex, DeviceAddress);
}
