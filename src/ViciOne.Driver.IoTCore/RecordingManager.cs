using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.IoTCore.Recording;
using ViciOne.DeviceTree.Contracts;

namespace ViciOne.Driver.IoTCore;

/// <summary>
/// Queues and executes BLOB-channel raw-data recordings for one IO-Link master, one port at a time.
/// </summary>
public sealed partial class RecordingManager : IDisposable
{
    private readonly BlobRecorder _blobRecorder;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private bool _disposed;
    private bool _isRecording;
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<(string Identifier, int PortIndex, bool IgnoreTimedValueArray)> _recordingPorts = new();
    private readonly string _address;

    /// <summary>
    /// Raised with a finished recording's result.
    /// </summary>
    public event Action<Measurement>? BlobDataReceived;

    /// <summary>
    /// Creates a manager for the IO-Link master at <paramref name="address"/>.
    /// </summary>
    public RecordingManager(string address, ILogger logger)
    {
        _address = address;
        _logger = logger;
        _blobRecorder = new BlobRecorder(_address);
    }

    /// <summary>
    /// Discards any still-queued recording requests and releases the underlying <see cref="BlobRecorder"/>.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // Cancel a recording still in flight before disposing the recorder, so it ends cooperatively (see the
        // token threaded through ExecuteAsync) instead of only via the disposed device's requests throwing.
        _cancellationTokenSource.Cancel();
        _blobRecorder.Dispose();

        while (_recordingPorts.IsEmpty == false)
            _recordingPorts.TryDequeue(out _);

        BlobDataReceived = null;
        _cancellationTokenSource.Dispose();
        _disposed = true;
    }

    /// <summary>
    /// Queues a recording request for the given port, starting the recording loop if it isn't already running.
    /// <para>A duplicate request already queued under the same <paramref name="identifier"/>, <paramref name="portIndex"/>
    /// and <paramref name="ignoreTimedValueArray"/> is not queued again.</para>
    /// </summary>
    /// <param name="identifier">An identifier for correlating this request with its eventual result.</param>
    /// <param name="portIndex">The port to record from.</param>
    /// <param name="ignoreTimedValueArray">Whether to skip decoding the recorded raw data into timed values.</param>
    public void GetRecordingBlobData(string identifier, int portIndex, bool ignoreTimedValueArray)
    {
        if (_recordingPorts.Contains((identifier, portIndex, ignoreTimedValueArray)))
            return;

        _recordingPorts.Enqueue((identifier, portIndex, ignoreTimedValueArray));
        _ = RecordingAsync();
    }

    /// <summary>
    /// Assigns an evenly-spaced timestamp to each decoded raw-data sample, starting at <paramref name="startTime"/>.
    /// </summary>
    private static IEnumerable<TimedValue> GetTimedValuesFromRawData(IReadOnlyList<double> values, DateTime startTime, TimeSpan timeBetweenMeasurements)
    {
        DateTime measurementTime = startTime;

        foreach (double value in values)
        {
            yield return new TimedValue()
            {
                Timestamp = measurementTime,
                Value = value,
            };

            measurementTime += timeBetweenMeasurements;
        }
    }

    /// <summary>
    /// Works through the queued recording requests one at a time, executing each on the underlying
    /// <see cref="BlobRecorder"/> and raising <see cref="BlobDataReceived"/> for every successful one.
    /// </summary>
    private async Task RecordingAsync()
    {
        if (_isRecording)
            return;

        _isRecording = true;

        (string Identifier, int PortIndex, bool IgnoreTimedValueArray) pair = default;
        while (_recordingPorts.IsEmpty == false)
            try
            {
                if (_recordingPorts.TryPeek(out pair))
                {
                    (bool success, string? errorMessage, BlobCapture? capture) = await _blobRecorder.ExecuteAsync(pair.PortIndex, _logger, _cancellationTokenSource.Token);
                    if (success)
                    {
                        BlobCapture blobCapture = capture!;
                        Measurement measurement = new()
                        {
                            Duration = blobCapture.Duration,
                            Frequency = blobCapture.Frequency,
                            Identifier = pair.Identifier,
                            RawData = blobCapture.RawData,
                            RecordReasons = [],
                            Start = blobCapture.Start,
                            Values = pair.IgnoreTimedValueArray
                                ? []
                                : [.. GetTimedValuesFromRawData(blobCapture.Values, blobCapture.Start, TimeSpan.FromMilliseconds(1000.0 / blobCapture.Frequency))],
                        };

                        BlobDataReceived?.Invoke(measurement);

                        LogRecordingDataReceived(pair.PortIndex);
                    }
                    else
                        LogRecordingDataWarning(pair.PortIndex, errorMessage!);
                }
            }
            catch (ObjectDisposedException)
            { }
            catch (TaskCanceledException)
            { }
            catch (Exception ex)
            {
                LogRecordingDataError(ex, pair.PortIndex);
            }
            finally
            {
                _recordingPorts.TryDequeue(out _);
            }

        _isRecording = false;
    }
}
