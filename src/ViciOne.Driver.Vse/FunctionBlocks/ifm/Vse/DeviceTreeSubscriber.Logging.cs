using System;

namespace ifm.Vse;

// Logging helpers for the DeviceTreeSubscriber class. See DeviceTreeSubscriberLog for the actual [LoggerMessage] definitions.
public sealed partial class DeviceTreeSubscriber
{
    private void LogParsingError(Exception ex)
        => Logger.ParsingErrorOccurred(ex, _device!.DeviceAddress);
}
