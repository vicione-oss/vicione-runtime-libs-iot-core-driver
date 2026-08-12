using System.Runtime.InteropServices;
using IFM.SDK.VSE;
using IFM.SDK.VSE.Monitoring;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.Vse.EventManager;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// Records raw (unprocessed) sample data from one of a VSE device's external sensors. Starting a recording
/// switches the device into <see cref="SystemMode.Measure"/>, which is mutually exclusive with normal
/// process-data monitoring (<see cref="SystemMode.Supervise"/>) - counters, objects, inputs and alarms stop
/// receiving fresh samples for the duration of a recording.
/// </summary>
public sealed class RawDataRecorder(IVSEDevice device, DeviceEventManager eventManager, ILogger? logger, int eventQueueSettleTimeoutMs = 5000, int eventQueueSettleWindowMs = 50) : IDisposable
{
    private sealed class SensorData(byte sensor)
    {
        public byte Sensor { get; } = sensor;
        public List<float> DataPoints { get; set; } = [];
        public DateTime RecordingStartTimeUtc { get; private set; }
        public string LastRecordingResult { get; set; } = string.Empty;

        public void MarkRecordingStarted() => RecordingStartTimeUtc = DateTime.UtcNow;

        public void Clear()
        {
            DataPoints.Clear();
            DataPoints.TrimExcess();
            LastRecordingResult = string.Empty;
            RecordingStartTimeUtc = default;
        }
    }

    private SensorData[]? _sensors;
    private readonly Lock _lock = new();
    private long _isRecording;
    private long _stopRequested;
    private SensorData? _currentSensor;
    private Task? _recordingTask;

    // No caller outside this class/its tests observes recording state directly - CaptureBlocking is the only
    // real entry point (VseRawDataCollector never inspects IsRecording/StateChanged, nor calls
    // StopRecording()).
    public bool IsRecording => Interlocked.Read(ref _isRecording) == 1;

    /// <summary>
    /// Raised whenever recording starts or stops (successfully, interrupted, or failed to start).
    /// </summary>
    public event Action? StateChanged;

    // CaptureBlocking uses RecordingStartTimeUtc/ClearBlob directly (via Sensor().DataPoints for the actual
    // sample buffer) - no caller outside this class/its tests needs to inspect a sensor's blob state directly.
    public int BlobSize(byte sensor) => Sensor(sensor).DataPoints.Count;

    internal DateTime? RecordingStartTimeUtc(byte sensor)
    {
        DateTime value = Sensor(sensor).RecordingStartTimeUtc;
        return value == default ? null : value;
    }

    internal void ClearBlob(byte sensor) => Sensor(sensor).Clear();

    public IReadOnlyList<float> BlobContents(byte sensor)
    {
        SensorData sensorData = Sensor(sensor);
        if (IsRecording && ReferenceEquals(_currentSensor, sensorData))
            throw new InvalidOperationException("The data of the currently recorded sensor can't be queried while still recording.");

        return sensorData.DataPoints;
    }

    /// <summary>
    /// Starts recording <paramref name="duration"/> milliseconds of raw samples from <paramref name="sensor"/> at
    /// <paramref name="sampleRate"/> samples/sec. Waits for the device's event queue to settle first, since
    /// switching system mode while another critical event (mode change, reboot, teach-in, ...) is still being
    /// processed can leave the device in an inconsistent state.
    /// </summary>
    internal void StartRecording(byte sensor, uint sampleRate, uint duration, bool waitUntilFinished = true)
    {
        SensorData sensorData;
        IMonitoringChannel<IRawDataSample> channel;

        // Only setup (through starting the channel) needs the lock, to keep two overlapping calls from both
        // passing the IsRecording precondition check below - WaitForMeasureModeAndLaunchRecording deliberately
        // runs unlocked, so it doesn't also block StopRecording() (same lock) for its up-to-5-second wait.
        lock (_lock)
        {
            int samplesToAcquire = ValidateStartPreconditions(sensor, sampleRate, duration);
            eventManager.WaitUntilQuiet(eventQueueSettleTimeoutMs, eventQueueSettleWindowMs);

            sensorData = Sensor(sensor);
            if (sensorData.DataPoints.Capacity == samplesToAcquire)
                sensorData.DataPoints.Clear();
            else
                sensorData.DataPoints = new List<float>(samplesToAcquire);

            _currentSensor = sensorData;

            RawChannelInfo channelInfo = new((Sensor)(sensor - 1), sampleRate == 50_000 ? SampleRate._50000 : SampleRate._100000);
            channel = device.DataLink.CreateMonitoringChannel<IRawDataSample>(channelInfo);
            StartChannelOrThrow(channel, sample => ReceiveSample(sensorData, sample));

            // Marks the recorder busy for the IsRecording precondition above, even though the device hasn't
            // reached Measure mode yet - reset back to 0 in WaitForMeasureModeAndLaunchRecording if that never
            // happens.
            Interlocked.Exchange(ref _isRecording, 1);
            Interlocked.Exchange(ref _stopRequested, 0);
        }

        if (WaitForMeasureModeAndLaunchRecording(sensor, sensorData, channel) == false || !waitUntilFinished)
            return;

        // RecordAsync catches all its own exceptions and reports failures through LastRecordingResult instead,
        // so this Wait() cannot fault - it only blocks until the recording loop above has finished.
        _recordingTask!.Wait();

        string result = Sensor(sensor).LastRecordingResult;
        if (!string.IsNullOrEmpty(result))
            throw new InvalidOperationException(result);
    }

    private void StartChannelOrThrow(IMonitoringChannel<IRawDataSample> channel, Action<IRawDataSample> onReceived)
    {
        try
        {
            channel.Start(onReceived);
        }
        catch (Exception ex)
        {
            StopChannelBestEffort(channel);
            throw new InvalidOperationException($"Cannot start raw data recording: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Waits (unlocked) for the device to confirm Measure mode after <see cref="StartChannelOrThrow"/> above,
    /// then launches <see cref="RecordAsync"/>. Returns false if Measure mode was never reached; either way,
    /// <see cref="_isRecording"/> is reset back to 0 unless the recording actually got underway.
    /// </summary>
    private bool WaitForMeasureModeAndLaunchRecording(byte sensor, SensorData sensorData, IMonitoringChannel<IRawDataSample> channel)
    {
        bool started = false;
        try
        {
            DateTime waitDeadline = DateTime.Now.AddMilliseconds(5000);
            while (DateTime.Now < waitDeadline && device.SystemMode != SystemMode.Measure)
                Thread.Sleep(100);

            if (device.SystemMode != SystemMode.Measure)
            {
                logger?.CannotSwitchToMeasureMode(sensor, device.SystemMode);
                StopChannelBestEffort(channel);
                return false;
            }

            _recordingTask = RecordAsync(sensorData, channel);
            started = true;
            StateChanged?.Invoke();
            return true;
        }
        catch (Exception ex)
        {
            StopChannelBestEffort(channel);
            throw new InvalidOperationException($"Cannot start raw data recording: {ex.Message}", ex);
        }
        finally
        {
            if (!started)
                Interlocked.Exchange(ref _isRecording, 0);
        }
    }

    /// <summary>
    /// Records <paramref name="duration"/> milliseconds of raw samples from <paramref name="sensor"/> at
    /// <paramref name="sampleRate"/> samples/sec, blocking until finished, then copies out and clears the
    /// resulting blob - the common one-shot "record then read the result" flow.
    /// </summary>
    public RawDataCapture CaptureBlocking(byte sensor, uint sampleRate, uint duration)
    {
        StartRecording(sensor, sampleRate, duration, waitUntilFinished: true);

        // Read the tracked start time before ClearBlob() resets it.
        DateTime start = RecordingStartTimeUtc(sensor) ?? DateTime.UtcNow;

        // Goes straight from the sample buffer's own backing array to bytes - no intermediate float[] copy of
        // the whole capture, since RawDataCapture only needs one array (RawData) and derives Values from it
        // lazily if a caller actually reads it.
        byte[] rawData = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(Sensor(sensor).DataPoints)).ToArray();
        ClearBlob(sensor);

        return new RawDataCapture(start, duration, sampleRate, rawData);
    }

    internal void StopRecording()
    {
        lock (_lock)
        {
            if (!IsRecording)
                return;

            Interlocked.Exchange(ref _stopRequested, 1);
            if (_recordingTask is null || !_recordingTask.Wait(2000))
            {
                logger?.StoppingRecordingTimedOut(_currentSensor?.Sensor ?? 0);
                throw new TimeoutException("Raw data recording could not be stopped within the required time.");
            }
        }
    }

    private int ValidateStartPreconditions(byte sensor, uint sampleRate, uint duration)
    {
        if (IsRecording)
            throw new InvalidOperationException("Raw data recording is already in progress.");

        if (device.SystemMode != SystemMode.Supervise)
            throw new InvalidOperationException("Raw data recording can only be started from supervise mode.");

        _ = Sensor(sensor);

        if (duration < 100)
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration below 100 ms is not allowed.");

        if (sampleRate is not (50_000 or 100_000))
            throw new ArgumentOutOfRangeException(nameof(sampleRate), sampleRate, "Only 50000 or 100000 samples per second are supported.");

        ulong samplesToAcquire = (ulong)duration * sampleRate / 1000;
        if (samplesToAcquire > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "The requested recording would require more samples than fit into a single recording buffer.");

        return (int)samplesToAcquire;
    }

    private SensorData Sensor(byte sensor)
    {
        SensorData[] sensors = GetOrCreateSensors();
        if (sensor < 1 || sensor > sensors.Length)
            throw new ArgumentOutOfRangeException(nameof(sensor), sensor, $"Sensor out of bounds [1..{sensors.Length}].");

        return sensors[sensor - 1];
    }

    private SensorData[] GetOrCreateSensors()
    {
        lock (_lock)
        {
            _sensors ??= [.. Enumerable.Range(1, device.Parameters.Inputs.Count(i => i.Type == InputType.Dynamic)).Select(i => new SensorData((byte)i))];
            return _sensors;
        }
    }

    private static void ReceiveSample(SensorData sensorData, IRawDataSample dataSample)
    {
        List<float> dataPoints = sensorData.DataPoints;
        foreach (float value in dataSample.GetValues())
        {
            if (dataPoints.Count < dataPoints.Capacity)
                dataPoints.Add(value);
        }
    }

    private async Task RecordAsync(SensorData sensorData, IMonitoringChannel<IRawDataSample> channel)
    {
        const int PollIntervalMs = 250;
        const int ValueTimeoutMs = 5000;

        try
        {
            sensorData.MarkRecordingStarted();
            sensorData.LastRecordingResult = string.Empty;

            int lastCount = sensorData.DataPoints.Count;
            int msecsSinceLastValue = 0;

            while (sensorData.DataPoints.Count < sensorData.DataPoints.Capacity)
            {
                await Task.Delay(PollIntervalMs).ConfigureAwait(false);

                if (device.ConnectionStatus != ConnectionStatus.Connected)
                {
                    sensorData.LastRecordingResult = "Raw data recording was interrupted by a device connection error.";
                    break;
                }

                SystemMode systemMode = device.SystemMode;
                if (systemMode != SystemMode.Measure)
                {
                    sensorData.LastRecordingResult = $"Raw data recording was interrupted by a system mode change to '{ReflectionUtils.SysMode2Str(systemMode)}'.";
                    break;
                }

                if (Interlocked.Read(ref _stopRequested) == 1)
                {
                    sensorData.LastRecordingResult = "Raw data recording was interrupted by an explicit stop request.";
                    break;
                }

                if (sensorData.DataPoints.Count == lastCount)
                {
                    msecsSinceLastValue += PollIntervalMs;
                    if (msecsSinceLastValue > ValueTimeoutMs)
                    {
                        sensorData.LastRecordingResult = $"Raw data recording was interrupted by a timeout while waiting for values - no data received for approx. {msecsSinceLastValue} ms.";
                        break;
                    }
                }
                else
                {
                    msecsSinceLastValue = 0;
                    lastCount = sensorData.DataPoints.Count;
                }
            }
        }
        catch (Exception ex)
        {
            sensorData.LastRecordingResult = $"Raw data recording was interrupted by an exception ({ex.Message}).";
            logger?.RawDataRecordingFailed(ex, sensorData.Sensor);
        }

        await StopChannelBestEffortAsync(channel).ConfigureAwait(false);
        TryRestoreSuperviseMode();

        if (string.IsNullOrEmpty(sensorData.LastRecordingResult))
            logger?.RawDataRecordingFinished(sensorData.Sensor, sensorData.DataPoints.Count);
        else
            logger?.RawDataRecordingInterrupted(sensorData.Sensor, sensorData.LastRecordingResult);

        Interlocked.Exchange(ref _isRecording, 0);
        _currentSensor = null;
        StateChanged?.Invoke();
    }

    private void StopChannelBestEffort(IMonitoringChannel<IRawDataSample> channel)
    {
        try
        {
            channel.Stop();
        }
        catch (Exception ex)
        {
            logger?.StoppingMonitoringChannelFailed(ex, "raw data");
        }
        finally
        {
            channel.Dispose();
        }
    }

    private async Task StopChannelBestEffortAsync(IMonitoringChannel<IRawDataSample> channel)
    {
        try
        {
            await channel.StopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.StoppingMonitoringChannelFailed(ex, "raw data");
        }
        finally
        {
            channel.Dispose();
        }
    }

    // The device is expected to fall back to Supervise mode on its own once no raw-data channel is active
    // anymore, but that behavior is undocumented - request it explicitly instead of just hoping process-data
    // monitoring (Counters/Objects/Inputs/Alarms) comes back by itself.
    private void TryRestoreSuperviseMode()
    {
        if (device.ConnectionStatus != ConnectionStatus.Connected || device.SystemMode == SystemMode.Supervise)
            return;

        try
        {
            eventManager.WaitUntilQuiet(eventQueueSettleTimeoutMs, eventQueueSettleWindowMs);
            device.Commands.SetSystemMode(SystemMode.Supervise);
        }
        catch (Exception ex)
        {
            logger?.RestoringSuperviseModeFailed(ex);
        }
    }

    public void Dispose()
    {
        if (_recordingTask is null)
            return;

        Interlocked.Exchange(ref _stopRequested, 1);
        _recordingTask.Wait();
        _recordingTask.Dispose();
    }
}
