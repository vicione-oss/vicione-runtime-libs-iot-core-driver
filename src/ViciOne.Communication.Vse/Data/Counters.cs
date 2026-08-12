using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// All counters of a VSE device, kept live via a single shared monitoring channel (the VSE-SDK
/// pushes changed samples to us - there is no need to poll).
/// </summary>
public sealed class Counters : IDisposable
{
    // Null when the device has no counters at all: DataChannelInfoHelpers.AsChannelInfo() throws on an empty
    // collection, and there would be nothing to monitor anyway.
    private readonly IMonitoringChannel<ICounterDataSample>? _channel;
    private readonly Dictionary<int, Counter> _byId;
    private readonly ILogger? _logger;

    public IReadOnlyCollection<Counter> All => _byId.Values;

    public Counters(IVSEDevice device, ILogger? logger)
    {
        _logger = logger;

        IReadOnlyList<ICounter> counters = device.Parameters.Counters;
        _byId = counters.ToDictionary(c => c.Id, c => new Counter(c));

        if (counters.Count == 0)
            return;

        _channel = device.DataLink.CreateMonitoringChannel<ICounterDataSample>(DataChannelInfoHelpers.AsChannelInfo(counters));
        _channel.Start(OnReceived);
    }

    private void OnReceived(ICounterDataSample dataSample)
    {
        if (_byId.TryGetValue(dataSample.Id, out Counter? counter))
            counter.Received(dataSample);
    }

    public void Dispose()
    {
        if (_channel is null)
            return;

        // Best-effort: we're tearing this down anyway, so any failure to stop the channel cleanly is only logged.
        try
        {
            _channel.Stop();
        }
        catch (Exception ex)
        {
            _logger?.StoppingMonitoringChannelFailed(ex, "counter");
        }
        finally
        {
            _channel.Dispose();
        }
    }
}
