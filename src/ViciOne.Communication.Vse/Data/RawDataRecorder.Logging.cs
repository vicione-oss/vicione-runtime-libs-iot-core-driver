using System;
using IFM.SDK.VSE;

namespace ViciOne.Communication.Vse.Data;

// Logging helpers for the RawDataRecorder class. See RawDataRecorderLog for the actual [LoggerMessage] definitions.
public sealed partial class RawDataRecorder
{
    private void LogCannotSwitchToMeasureMode(byte sensor, SystemMode currentMode)
        => logger?.CannotSwitchToMeasureMode(sensor, currentMode, DeviceAddress);

    private void LogStoppingRecordingTimedOut(byte sensor)
        => logger?.StoppingRecordingTimedOut(sensor, DeviceAddress);

    private void LogRawDataRecordingFailed(Exception ex, byte sensor)
        => logger?.RawDataRecordingFailed(ex, sensor, DeviceAddress);

    private void LogRawDataRecordingFinished(byte sensor, int sampleCount)
        => logger?.RawDataRecordingFinished(sensor, sampleCount, DeviceAddress);

    private void LogRawDataRecordingInterrupted(byte sensor, string reason)
        => logger?.RawDataRecordingInterrupted(sensor, reason, DeviceAddress);

    private void LogStoppingMonitoringChannelFailed(Exception ex)
        => logger?.StoppingMonitoringChannelFailed(ex, "raw data", DeviceAddress);

    private void LogRestoringSuperviseModeFailed(Exception ex)
        => logger?.RestoringSuperviseModeFailed(ex, DeviceAddress);
}
