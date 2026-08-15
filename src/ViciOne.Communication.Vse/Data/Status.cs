using IFM.SDK.VSE;
using ViciOne.Communication.Vse.EventManager;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// The Vse device's live connection/mode state - unlike <see cref="DeviceInfo"/>, everything here can change
/// while the device is connected.
/// </summary>
public sealed class Status : IDisposable
{
    private readonly IVSEDevice _device;
    private readonly DeviceEventManager _eventManager;

    public ConnectionStatus ConnectionStatus => _device.ConnectionStatus;

    public SystemMode SystemMode
    {
        get => _device.SystemMode;
        set => SetMode(value);
    }

    /// <summary>
    /// True once the device's event queue has been idle for a while, i.e. no critical event (mode change,
    /// reboot, teach-in, ...) has occurred recently.
    /// </summary>
    public bool TreeStable => _eventManager.TreeStable;

    /// <summary>
    /// Raised whenever <see cref="ConnectionStatus"/> or <see cref="SystemMode"/> changes.
    /// </summary>
    public event Action? Changed;

    public Status(IVSEDevice device, DeviceEventManager eventManager)
    {
        _device = device;
        _eventManager = eventManager;
        _eventManager.ConnectionStatusChanged += OnConnectionStatusChanged;
        _eventManager.SystemModeChanged += OnSystemModeChanged;
    }

    private void OnConnectionStatusChanged(ConnectionEvent evt) => Changed?.Invoke();

    private void OnSystemModeChanged(ModeChangedEvent evt) => Changed?.Invoke();

    private void SetMode(SystemMode mode)
    {
        if (_device.ConnectionStatus != ConnectionStatus.Connected)
            throw new InvalidOperationException("Cannot change the system mode while the device is not connected.");

        // Wait for any in-flight critical event (mode change, reboot, teach-in, ...) to settle first, to avoid
        // racing it with this one.
        _eventManager.WaitUntilQuiet();
        _device.Commands.SetSystemMode(mode);
    }

    public void Dispose()
    {
        _eventManager.ConnectionStatusChanged -= OnConnectionStatusChanged;
        _eventManager.SystemModeChanged -= OnSystemModeChanged;
    }
}
