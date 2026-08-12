using IFM.SDK;
using IFM.SDK.VSE;
using Microsoft.Extensions.Logging;
using ViciOne.Communication.Vse.Data;
using ViciOne.Communication.Vse.EventManager;

namespace ViciOne.Communication.Vse;

/// <summary>
/// Owns the connection to a single VSE device and the lifecycle of its live data categories (<see cref="Counters"/>,
/// <see cref="DeviceObjects"/>, <see cref="Inputs"/>, <see cref="DigitalAlarms"/>/<see cref="AnalogAlarms"/>,
/// <see cref="Variants"/>). Those are rebuilt from scratch whenever the device (re)connects, reports its
/// parameters have changed (e.g. after a teach-in or a variant switch), or returns to Supervise mode after a raw
/// data recording (<see cref="RawDataRecorder"/>) - the device genuinely can report a different set of
/// counters/objects/inputs/alarms afterwards, or may simply have stopped pushing samples on the old monitoring
/// channels while in Measure mode, so they would otherwise go stale.
/// </summary>
public sealed class VseConnection : IDisposable
{
    // The IFM-SDK logs most of its own connection/device chatter at Information - raising just its categories to
    // Warning keeps that noise out without affecting the caller's own log messages, which go through the same
    // sink but under different categories (see ForwardLoggerProvider).
    private static readonly LoggerFilterOptions s_sdkLoggerFilterOptions = new()
    {
        Rules = { new LoggerFilterRule(null, "IFM.SDK", LogLevel.Warning, null) },
    };

    private readonly ILogger? _logger;
    private readonly LoggerFactory? _loggerFactory;
    private readonly DeviceEventManager _eventManager;
    private bool _disposedValue;

    public IVSEDevice Device { get; }
    public DeviceInfo DeviceInfo { get; }
    public Status Status { get; }
    public RawDataRecorder RawDataRecorder { get; }

    public Counters? Counters { get; private set; }
    public DeviceObjects? DeviceObjects { get; private set; }
    public Inputs? Inputs { get; private set; }
    public DigitalAlarms? DigitalAlarms { get; private set; }
    public AnalogAlarms? AnalogAlarms { get; private set; }
    public Variants? Variants { get; private set; }

    /// <summary>
    /// Raised after the data categories above have been (re)built - once after every successful connection, and
    /// again whenever the device reports its parameters have changed.
    /// </summary>
    public event Action? Rebuilt;

    public VseConnection(string host, int port, ILogger? logger = null)
        : this(BuildDevice(host, port, logger, out LoggerFactory? loggerFactory), logger, loggerFactory)
    { }

    /// <summary>
    /// Wraps an already-built <see cref="IVSEDevice"/> directly, skipping the builder - lets tests drive a real
    /// <see cref="VseConnection"/> (categories, rebuild-on-connect/parameters-changed, ...) against a mocked
    /// device instead of a real hardware connection.
    /// </summary>
    public VseConnection(IVSEDevice device, ILogger? logger = null)
        : this(device, logger, CreateLoggerFactory(logger))
    { }

    private VseConnection(IVSEDevice device, ILogger? logger, LoggerFactory? loggerFactory)
    {
        Device = device;
        _logger = logger;
        _loggerFactory = loggerFactory;

        _eventManager = new DeviceEventManager(Device, _loggerFactory);
        _eventManager.Connected += OnConnected;
        _eventManager.ParametersChanged += OnParametersChanged;
        _eventManager.SystemModeChanged += OnSystemModeChanged;

        DeviceInfo = new DeviceInfo(Device);
        Status = new Status(Device, _eventManager);
        RawDataRecorder = new RawDataRecorder(Device, _eventManager, _logger);
    }

    // The IFM-SDK builder and DeviceEventManager both want an ILoggerFactory, but callers only ever have a single
    // ILogger (matching the rest of this codebase's logging convention) - wrap it so every category it creates a
    // logger for ends up writing to that same sink.
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Objekte verwerfen, bevor Bereich verloren geht", Justification = "ForwardLoggerProvider.Dispose() is a no-op (nothing to release) - ownership genuinely transfers into the returned LoggerFactory, which the constructor stores in _loggerFactory and disposes in Dispose().")]
    private static LoggerFactory? CreateLoggerFactory(ILogger? logger)
        => logger is null ? null : new LoggerFactory([new ForwardLoggerProvider(logger)], s_sdkLoggerFilterOptions);

    private static IVSEDevice BuildDevice(string host, int port, ILogger? logger, out LoggerFactory? loggerFactory)
    {
        loggerFactory = CreateLoggerFactory(logger);

        ILoggingSpecification<IVSEDevice> deviceBuilder = new DeviceBuilder().VSEDevice()
            .WithConnectionInfo(new ConnectionInfo(host, port))
            .WithDefaultDataMonitoringOptions();
        return (logger is null ? deviceBuilder.WithNoLogging() : deviceBuilder.WithLoggingConfiguration(loggerFactory!)).Build();
    }

    public void Connect()
        => Device.ConnectAsync();

    public void Disconnect()
        => Device.Disconnect();

    /// <summary>
    /// Reboots the device. Waits for the device's event queue to settle first, to avoid racing an in-flight
    /// critical event (mode change, teach-in, ...).
    /// </summary>
    public void Reboot()
    {
        _eventManager.WaitUntilQuiet();
        Device.Commands.Reboot();
    }

    private void OnConnected(IVSEDevice device)
        => Rebuild();

    private void OnParametersChanged(ParameterChangedEvent evt)
        => Rebuild();

    // Measure mode (raw data recording) stops the process-data monitoring channels from delivering samples -
    // once the device returns to Supervise, they need to be rebuilt rather than just left for dead, since
    // nothing else re-establishes them (Connected/ParametersChanged don't fire on a plain mode change).
    private void OnSystemModeChanged(ModeChangedEvent evt)
    {
        if (evt.Mode == SystemMode.Supervise)
            Rebuild();
    }

    private void Rebuild()
    {
        DisposeCategories();

        Counters = new Counters(Device, _logger);
        DeviceObjects = new DeviceObjects(Device, _eventManager, _logger);
        Inputs = new Inputs(Device, _logger);
        DigitalAlarms = new DigitalAlarms(Device, _logger);
        AnalogAlarms = new AnalogAlarms(Device, _logger);
        Variants = new Variants(Device, _eventManager);

        Rebuilt?.Invoke();
    }

    private void DisposeCategories()
    {
        Counters?.Dispose();
        DeviceObjects?.Dispose();
        Inputs?.Dispose();
        DigitalAlarms?.Dispose();
        AnalogAlarms?.Dispose();
        Variants?.Dispose();
    }

    public void Dispose()
    {
        if (_disposedValue)
            return;

        _eventManager.Connected -= OnConnected;
        _eventManager.ParametersChanged -= OnParametersChanged;
        _eventManager.SystemModeChanged -= OnSystemModeChanged;

        DisposeCategories();
        RawDataRecorder.Dispose();
        Status.Dispose();
        _eventManager.Dispose();

        if (Device.ConnectionStatus != ConnectionStatus.Disconnected)
            Device.Disconnect();
        Device.Dispose();

        _loggerFactory?.Dispose();

        _disposedValue = true;
    }
}
