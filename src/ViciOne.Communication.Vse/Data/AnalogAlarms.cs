using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// All analog alarms of a VSE device. Each alarm gets its own monitoring channel, same as <see cref="DigitalAlarms"/>.
/// </summary>
public sealed class AnalogAlarms : IDisposable
{
    private readonly List<IMonitoringChannel<IIODataSample>> _channels;
    private readonly Dictionary<int, AnalogAlarm> _byId;
    private readonly ILogger? _logger;

    public IReadOnlyCollection<AnalogAlarm> All => _byId.Values;

    public AnalogAlarms(IVSEDevice device, ILogger? logger)
    {
        _logger = logger;

        IReadOnlyList<IAnalogAlarm> instances = device.Parameters.AnalogAlarms;

        _byId = instances.ToDictionary(instance => instance.Id, instance => new AnalogAlarm(instance));

        List<IMonitoringChannel<IIODataSample>> channels = [];
        foreach (IAnalogAlarm instance in instances)
        {
            IMonitoringChannel<IIODataSample> channel = device.DataLink.CreateMonitoringChannel<IIODataSample>(instance.AsChannelInfo());
            channel.Start(OnReceived);
            channels.Add(channel);
        }
        _channels = channels;
    }

    private void OnReceived(IIODataSample dataSample)
    {
        if (_byId.TryGetValue(dataSample.Id, out AnalogAlarm? alarm))
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
                _logger?.StoppingMonitoringChannelFailed(ex, "analog alarm");
            }
            finally
            {
                channel.Dispose();
            }
        }
    }
}
