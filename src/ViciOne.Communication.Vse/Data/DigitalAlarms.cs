using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// All digital alarms of a VSE device. Unlike <see cref="Counters"/>/<see cref="Inputs"/>, each alarm gets its
/// own monitoring channel - the SDK has no bulk-subscribe helper for alarms.
/// </summary>
public sealed class DigitalAlarms : IDisposable
{
    private readonly List<IMonitoringChannel<IIODataSample>> _channels;
    private readonly Dictionary<int, DigitalAlarm> _byId;
    private readonly ILogger? _logger;

    public IReadOnlyCollection<DigitalAlarm> All => _byId.Values;

    public DigitalAlarms(IVSEDevice device, ILogger? logger)
    {
        _logger = logger;

        IReadOnlyList<IDigitalAlarm> instances = device.Parameters.DigitalAlarms;

        _byId = instances.ToDictionary(instance => instance.Id, instance => new DigitalAlarm(instance));

        List<IMonitoringChannel<IIODataSample>> channels = [];
        foreach (IDigitalAlarm instance in instances)
        {
            IMonitoringChannel<IIODataSample> channel = device.DataLink.CreateMonitoringChannel<IIODataSample>(instance.AsChannelInfo());
            channel.Start(OnReceived);
            channels.Add(channel);
        }
        _channels = channels;
    }

    private void OnReceived(IIODataSample dataSample)
    {
        if (_byId.TryGetValue(dataSample.Id, out DigitalAlarm? alarm))
            alarm.Received(dataSample);
    }

    public void Dispose()
    {
        foreach (IMonitoringChannel<IIODataSample> channel in _channels)
        {
            try
            {
                channel.Stop();
            }
            catch (Exception ex)
            {
                _logger?.StoppingMonitoringChannelFailed(ex, "digital alarm");
            }
            finally
            {
                channel.Dispose();
            }
        }
    }
}
