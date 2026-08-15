using IFM.SDK.VSE;
using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.Data;

internal static partial class RawDataRecorderLog
{
    [LoggerMessage(EventId = 9000, EventName = "CannotSwitchToMeasureMode", Level = LogLevel.Error, Message = "Device '{DeviceAddress}': cannot start raw data recording on sensor {Sensor} - the device did not switch to Measure mode within the expected time (current mode: {CurrentMode}).")]
    public static partial void CannotSwitchToMeasureMode(this ILogger logger, byte sensor, SystemMode currentMode, string deviceAddress);

    [LoggerMessage(EventId = 9001, EventName = "RawDataRecordingFailed", Level = LogLevel.Warning, Message = "Device '{DeviceAddress}': raw data recording on sensor {Sensor} failed.")]
    public static partial void RawDataRecordingFailed(this ILogger logger, Exception ex, byte sensor, string deviceAddress);

    [LoggerMessage(EventId = 9002, EventName = "RawDataRecordingFinished", Level = LogLevel.Information, Message = "Device '{DeviceAddress}': raw data recording on sensor {Sensor} finished with {SampleCount} samples.")]
    public static partial void RawDataRecordingFinished(this ILogger logger, byte sensor, int sampleCount, string deviceAddress);

    [LoggerMessage(EventId = 9003, EventName = "RawDataRecordingInterrupted", Level = LogLevel.Warning, Message = "Device '{DeviceAddress}': raw data recording on sensor {Sensor} was interrupted: {Reason}")]
    public static partial void RawDataRecordingInterrupted(this ILogger logger, byte sensor, string reason, string deviceAddress);

    [LoggerMessage(EventId = 9004, EventName = "RestoringSuperviseModeFailed", Level = LogLevel.Warning, Message = "Device '{DeviceAddress}': could not switch the device back to Supervise mode after a raw data recording.")]
    public static partial void RestoringSuperviseModeFailed(this ILogger logger, Exception ex, string deviceAddress);

    [LoggerMessage(EventId = 9005, EventName = "StoppingRecordingTimedOut", Level = LogLevel.Error, Message = "Device '{DeviceAddress}': raw data recording on sensor {Sensor} could not be stopped within the required time.")]
    public static partial void StoppingRecordingTimedOut(this ILogger logger, byte sensor, string deviceAddress);
}
