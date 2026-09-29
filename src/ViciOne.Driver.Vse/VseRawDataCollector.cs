using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ViciOne.Communication.Vse.Data;
using ViciOne.DeviceTree.Contracts;

namespace ViciOne.Driver.Vse;

/// <summary>
/// Queues raw-data recording requests across all connected Vse devices onto a single background worker thread
/// (recordings from different devices/sensors are never run concurrently) and shapes the result into the cloud
/// adapter's <see cref="Measurement"/> contract. The actual recording mechanics (mode switch, blob read/clear)
/// live in <see cref="RawDataRecorder"/> - this class only queues, dispatches and reshapes.
/// </summary>
internal static class VseRawDataCollector
{
    private static readonly Lock s_lockObject = new();
    private static readonly ConcurrentQueue<VseRecordingInformation> s_recordings = new();
    private static Thread? s_threadWorker;

    internal static event Action<Measurement>? RawDataReceived;

    internal static void CollectRawData(VseDevice device, string identifier, int sensorIndex, int sampleRate, int duration, bool ignoreTimedValueArray,
        Action<Exception> handleError)
    {
        lock (s_lockObject)
        {
            if (s_recordings.Any(x => x.Identifier == identifier))
                return;

            s_recordings.Enqueue(new VseRecordingInformation()
            {
                Device = device,
                Duration = duration,
                IgnoreTimedValueArray = ignoreTimedValueArray,
                Identifier = identifier,
                SampleRate = sampleRate,
                SensorIndex = sensorIndex,
                HandleError = handleError,
            });

            if (s_threadWorker is null)
            {
                s_threadWorker = new Thread(Worker) { Name = $"{nameof(VseRawDataCollector)}.{nameof(Worker)}" };
                s_threadWorker.Start();
            }
        }
    }

    private static IEnumerable<TimedValue> GetTimedValuesFromRawData(IReadOnlyList<float> values, DateTime startTime, TimeSpan timeBetweenMeasurements)
    {
        DateTime measurementTime = startTime;

        foreach (float value in values)
        {
            yield return new TimedValue()
            {
                Timestamp = measurementTime,
                Value = value,
            };

            measurementTime += timeBetweenMeasurements;
        }
    }

    private static void Worker()
    {
        VseRecordingInformation? information = default;

        while (s_recordings.IsEmpty == false)
            try
            {
                if (s_recordings.TryPeek(out information) && information.Device.Connected)
                {
                    RawDataRecorder recorder = information.Device.Connection.RawDataRecorder;
                    RawDataCapture capture = recorder.CaptureBlocking((byte)information.SensorIndex, (uint)information.SampleRate, (uint)information.Duration);

                    RawDataReceived?.Invoke(new Measurement()
                    {
                        Duration = capture.Duration,
                        Frequency = capture.SampleRate,
                        Identifier = information.Identifier,
                        Start = capture.Start,
                        RawData = capture.RawData,
                        Values = information.IgnoreTimedValueArray
                                    ? []
                                    : [.. GetTimedValuesFromRawData(capture.Values, capture.Start, TimeSpan.FromSeconds(1f / capture.SampleRate))],
                    });
                }
            }
            catch (Exception ex)
            {
                information?.HandleError(ex);
            }
            finally
            {
                s_recordings.TryDequeue(out _);
            }

        lock (s_lockObject)
            s_threadWorker = null;
    }
}
