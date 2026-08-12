using IFM.SDK.VSE;
using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.Data;

internal static partial class RawDataRecorderLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Cannot start raw data recording on sensor {Sensor}: the device did not switch to Measure mode within the expected time (current mode: {CurrentMode}).")]
    public static partial void CannotSwitchToMeasureMode(this ILogger logger, byte sensor, SystemMode currentMode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Raw data recording on sensor {Sensor} failed.")]
    public static partial void RawDataRecordingFailed(this ILogger logger, Exception ex, byte sensor);

    [LoggerMessage(Level = LogLevel.Information, Message = "Raw data recording on sensor {Sensor} finished with {SampleCount} samples.")]
    public static partial void RawDataRecordingFinished(this ILogger logger, byte sensor, int sampleCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Raw data recording on sensor {Sensor} was interrupted: {Reason}")]
    public static partial void RawDataRecordingInterrupted(this ILogger logger, byte sensor, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not switch the device back to Supervise mode after a raw data recording.")]
    public static partial void RestoringSuperviseModeFailed(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Raw data recording on sensor {Sensor} could not be stopped within the required time.")]
    public static partial void StoppingRecordingTimedOut(this ILogger logger, byte sensor);
}
