using ACX.ViciOne.Data.Driver.CloudDataAdapter.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ViciOne.Driver.IoTCore.BlobRecording;

namespace ViciOne.Driver.IoTCore;

internal sealed class RecordingManager : IDisposable
{
    private readonly BlobRecordingModule _blobRecordingModule;
    private readonly IoTCoreDevice _device;
    private bool _disposed;
    private bool _isRecording;
    private readonly ILogger _logger;
    private readonly ConcurrentQueue<(string Identifier, int PortIndex, bool IgnoreTimedValueArray)> _recordingPorts = new();
    private readonly string _address;

    public event Action<Measurement>? BlobDataReceived;

    public RecordingManager(string address, ILogger logger)
    {
        _address = address;
        _logger = logger;
        _device = new IoTCoreDevice(new Uri($"http://{_address}"));
        _blobRecordingModule = new BlobRecordingModule(_device);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _device.Dispose();

        while (_recordingPorts.IsEmpty == false)
            _recordingPorts.TryDequeue(out _);

        BlobDataReceived = null;
        _disposed = true;
    }

    public void GetRecordingBlobData(string identifier, int portIndex, bool ignoreTimedValueArray)
    {
        if (_recordingPorts.Contains((identifier, portIndex, ignoreTimedValueArray)))
            return;

        _recordingPorts.Enqueue((identifier, portIndex, ignoreTimedValueArray));
        _ = RecordingAsync();
    }

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
                    (bool success, BlobError? error, Measurement? measurement) = await _blobRecordingModule.ExecuteAsync(pair.PortIndex, pair.IgnoreTimedValueArray, _logger, _device.Token);
                    if (success)
                    {
                        measurement!.Identifier = pair.Identifier;
                        measurement!.IdentifierType = 0;
                        BlobDataReceived?.Invoke(measurement);

                        _logger.RecordingDataReceived(pair.PortIndex, _address);
                    }
                    else
                        _logger.RecordingDataWarningOccurred(pair.PortIndex, _address, error!.Message);
                }
            }
            catch (ObjectDisposedException)
            { }
            catch (TaskCanceledException)
            { }
            catch (Exception ex)
            {
                _logger.RecordingDataErrorOccurred(ex, pair.PortIndex, _address);
            }
            finally
            {
                _recordingPorts.TryDequeue(out _);
            }

        _isRecording = false;
    }
}
