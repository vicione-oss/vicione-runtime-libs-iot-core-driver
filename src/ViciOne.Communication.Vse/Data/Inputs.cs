using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// All inputs of a VSE device. Only the <see cref="Input.DynamicValueMonitoring"/> subset (External inputs) is
/// subscribed on a shared monitoring channel - there is nothing to push for the rest.
/// </summary>
public sealed class Inputs : IDisposable
{
    // Null when no input is dynamically monitored (no External inputs on this device) - there would be nothing
    // to create a channel for.
    private readonly IMonitoringChannel<IIODataSample>? _channel;
    private readonly Dictionary<int, Input> _dynamicallyMonitored;
    private readonly ILogger? _logger;

    public IReadOnlyList<Input> All { get; }

    public Inputs(IVSEDevice device, ILogger? logger)
    {
        _logger = logger;

        List<Input> inputs = [];
        List<Tuple<InputType, int>> toMonitor = [];
        Dictionary<int, Input> dynamicallyMonitored = [];
        foreach (IInput instance in device.Parameters.Inputs)
        {
            Input input = new(instance, device.DataLink);
            inputs.Add(input);

            if (input.DynamicValueMonitoring)
            {
                toMonitor.Add(new Tuple<InputType, int>(instance.Type, instance.Id));
                dynamicallyMonitored.Add(input.Id, input);
            }
        }
        All = inputs;
        _dynamicallyMonitored = dynamicallyMonitored;

        if (toMonitor.Count == 0)
            return;

        _channel = device.DataLink.CreateMonitoringChannel<IIODataSample>(new InputChannelInfo(toMonitor));
        _channel.Start(OnReceived);
    }

    private void OnReceived(IIODataSample dataSample)
    {
        if (_dynamicallyMonitored.TryGetValue(dataSample.Id, out Input? input))
            input.Received(dataSample);
    }

    public void Dispose()
    {
        if (_channel is null)
            return;

        try
        {
            _channel.Stop();
        }
        catch (Exception ex)
        {
            _logger?.StoppingMonitoringChannelFailed(ex, "input");
        }
        finally
        {
            _channel.Dispose();
        }
    }
}
