using ACX.ViciOne.Data.Driver.CloudDataAdapter.Contracts;
using ifmIoTCore.Common.Variant;
using ifmIoTCore.Elements;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ViciOne.Driver.IoTCore.Contracts.Constants;

namespace ViciOne.Driver.IoTCore;

public static class VseRawDataManager
{
    private static readonly Lock s_lockObject = new();
    private static readonly CompositeFormat s_rawDataSensorGetDataFormat = CompositeFormat.Parse(VseAddresses.RawDataSensorGetData);
    private static readonly CompositeFormat s_rawDataSensorClearFormat = CompositeFormat.Parse(VseAddresses.RawDataSensorClear);
    private static readonly ConcurrentQueue<VseRecordingInformation> s_recordings = new();
    private static Thread? s_threadWorker;

    public static event Action<Measurement>? RawDataReceived;

    public static void CollectRawData(VseDevice device, string identifier, int sensorIndex, int samplingRate, int duration, bool ignoreTimedValueArray,
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
                IgnoreTimedValueArra = ignoreTimedValueArray,
                Identifier = identifier,
                SamplingRate = samplingRate,
                SensorIndex = sensorIndex,
                HandleError = handleError,
            });

            if (s_threadWorker is null)
            {
                s_threadWorker = new Thread(Worker) { Name = $"{nameof(VseRawDataManager)}.{nameof(Worker)}" };
                s_threadWorker.Start();
            }
        }
    }

    private static IEnumerable<TimedValue> GetTimedValuesFromRawData(byte[] rawDataBytes, DateTime startTime, TimeSpan timeBetweenMeasurements)
    {
        // Aus den VSE IoTCore Detailed Specifications
        using var stream = new MemoryStream(rawDataBytes);
        using var reader = new BinaryReader(stream);
        DateTime measurementTime = startTime;

        while (stream.Position < stream.Length)
        {
            yield return new TimedValue()
            {
                Timestamp = measurementTime,
                Value = reader.ReadSingle(),
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
                    DateTime start = DateTime.UtcNow;

                    IServiceElement startRecordingService = information.Device.Core.GetElementByAddress(VseAddresses.RawDataStartRecording) as IServiceElement
                        ?? throw new InvalidDataException($"VSE data recording service does not seem to exist with address ending on '{VseAddresses.RawDataStartRecording}'.");

                    VariantObject recordingParams = new()
                    {
                        { "sensor", new VariantValue(information.SensorIndex) },
                        { "samplerate", new VariantValue(information.SamplingRate) },
                        { "duration", new VariantValue(information.Duration) },
                        { "waitUntilFinished", new VariantValue(true) }
                    };

                    startRecordingService.Invoke(recordingParams, null);

                    string getDataAddress = string.Format(CultureInfo.InvariantCulture, s_rawDataSensorGetDataFormat, information.SensorIndex);

                    IServiceElement getDataService = information.Device.Core.GetElementByAddress(getDataAddress) as IServiceElement
                        ?? throw new InvalidDataException($"VSE GetData service does not seem to exist with address ending on '{getDataAddress}'.");

                    if (getDataService.Invoke(new VariantObject(), null) is not VariantObject recordedData)
                        continue;

                    string? base64 = recordedData["values"].ToObject<string>();
                    byte[] rawData = Convert.FromBase64String(base64);
                    base64 = null;

                    string clearAddress = string.Format(CultureInfo.InvariantCulture, s_rawDataSensorClearFormat, information.SensorIndex);

                    IServiceElement clearService = information.Device.Core.GetElementByAddress(clearAddress) as IServiceElement
                        ?? throw new InvalidDataException($"VSE clear service does not seem to exist with address ending on '{clearAddress}'.");

                    clearService.Invoke(null, null);

                    RawDataReceived?.Invoke(new Measurement()
                    {
                        Duration = information.Duration,
                        Frequency = information.SamplingRate,
                        Identifier = information.Identifier,
                        IdentifierType = 1,
                        Start = start,
                        RawData = rawData,
                        Values = information.IgnoreTimedValueArra
                                    ? []
                                    : [.. GetTimedValuesFromRawData(rawData, start, TimeSpan.FromSeconds(1f / information.SamplingRate))],
                    });

                    // Einbremsen von Rohdaten herunterladen, damit der IoTCore-Baum sich erneut aufbauen kann, damit wir sinnvolle Werte anstatt von NaN bekommen
                    Thread.Sleep(1000);
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
