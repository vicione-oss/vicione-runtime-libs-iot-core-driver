using IFM.SDK.VSE;
using IFM.SDK.VSE.Exceptions;
using IFM.SDK.VSE.Parameters;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.Vse.EventManager;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// All objects of a VSE device. Unlike <see cref="Counters"/>, only objects with
/// <see cref="DeviceObject.DynamicValuesMonitoring"/> enabled are subscribed on the shared monitoring channel -
/// that set can change at runtime, so the channel subscription is updated live rather than fixed at construction.
/// </summary>
public sealed class DeviceObjects : IDisposable
{
    // The original adapter left this at 0 until some external system wrote a valid value through the (never
    // actually wired up in ViciOne) writable tree node - meaning the sliding window's TimespanInMs was 0 and
    // RemoveOldSamples() evicted every sample immediately, silently turning Average/Maximum/Minimum into just
    // the latest single reading. Defaulting to the documented minimum here keeps the window meaningful from
    // the start instead of reproducing that bug.
    public const uint AveragingPeriodMin = 500;
    public const uint AveragingPeriodMax = 10_000;

    private readonly IVSEDevice _device;
    private readonly DeviceEventManager _eventManager;
    private readonly IMonitoringChannel<IObjectDataSample> _channel;
    private readonly List<DeviceObject> _objects;
    private readonly Dictionary<int, DeviceObject> _dynamicallyMonitored;
    private readonly Lock _dynamicallyMonitoredLock = new();
    private readonly ILogger? _logger;
    private uint _averagingPeriod = AveragingPeriodMin;

    public IReadOnlyList<DeviceObject> All => _objects;

    public uint AveragingPeriod
    {
        get => _averagingPeriod;
        set
        {
            if (value < AveragingPeriodMin || value > AveragingPeriodMax)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Valid value range: {AveragingPeriodMin}ms-{AveragingPeriodMax}ms.");

            foreach (DeviceObject obj in _objects)
                obj.AveragingPeriod = value;

            _averagingPeriod = value;
        }
    }

    public DeviceObjects(IVSEDevice device, DeviceEventManager eventManager, ILogger? logger)
    {
        _device = device;
        _eventManager = eventManager;
        _logger = logger;

        ObjectChannelInfo channelInfo = new([]);
        List<DeviceObject> objects = [];
        Dictionary<int, DeviceObject> dynamicallyMonitored = [];
        foreach (IObject instance in device.Parameters.Objects)
        {
            DeviceObject deviceObject = new(instance, _averagingPeriod);
            objects.Add(deviceObject);

            if (deviceObject.DynamicValuesMonitoring)
            {
                channelInfo.ParameterIds.Add(instance.Id);
                dynamicallyMonitored.Add(deviceObject.Id, deviceObject);
            }

            deviceObject.MonitoringStateChanged += enabled => OnMonitoringStateChanged(deviceObject, enabled);
        }
        _objects = objects;
        _dynamicallyMonitored = dynamicallyMonitored;

        _channel = device.DataLink.CreateMonitoringChannel<IObjectDataSample>(channelInfo);
        _channel.Start(OnReceived);

        _eventManager.VariantChanged += OnVariantChanged;
    }

    private void OnVariantChanged(VariantChangedEvent evt)
    {
        foreach (DeviceObject obj in _objects)
            obj.DisabledByVariant = !evt.Variant.ActivatedObjects.Contains(obj.Id);
    }

    private void OnMonitoringStateChanged(DeviceObject deviceObject, bool enabled)
    {
        lock (_dynamicallyMonitoredLock)
        {
            if (enabled)
                _dynamicallyMonitored[deviceObject.Id] = deviceObject;
            else
                _dynamicallyMonitored.Remove(deviceObject.Id);
        }

        DataChannelInfo[] channelInfo = _channel.ChannelInfo;
        foreach (DataChannelInfo info in channelInfo)
        {
            if (info.Type != DataType.Object)
                continue;

            if (enabled && !info.ParameterIds.Contains(deviceObject.Id))
                info.ParameterIds.Add(deviceObject.Id);
            else if (!enabled)
                info.ParameterIds.Remove(deviceObject.Id);
        }

        try
        {
            _channel.Update(channelInfo);
        }
        catch (ChannelDisabledException)
        {
            // The device dropped the channel from under us (e.g. reconnect in progress) - the next
            // reconnect/tree-rebuild cycle re-establishes monitoring, nothing to do here.
        }
        catch (Exception ex)
        {
            _logger?.UpdatingMonitoringChannelFailed(ex, "object", deviceObject.Id);
        }
    }

    private void OnReceived(IObjectDataSample dataSample)
    {
        lock (_dynamicallyMonitoredLock)
        {
            if (_dynamicallyMonitored.TryGetValue(dataSample.Id, out DeviceObject? obj))
                obj.Received(dataSample);
        }
    }

    public void Dispose()
    {
        _eventManager.VariantChanged -= OnVariantChanged;

        // Stopping the channel while disconnected can hang/fail outright - just release the client-side
        // resources in that case, the device itself doesn't need to be told to stop sending.
        if (_device.ConnectionStatus == ConnectionStatus.Connected)
        {
            try
            {
                _channel.Stop();
            }
            catch (Exception ex)
            {
                _logger?.StoppingMonitoringChannelFailed(ex, "object");
            }
        }

        _channel.Dispose();

        foreach (DeviceObject obj in _objects)
            obj.Dispose();
    }
}
