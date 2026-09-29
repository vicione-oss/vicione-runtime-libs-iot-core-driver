using IFM.SDK.VSE;
using ViciOne.Communication.Vse.EventManager;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// All variants configured on a Vse device, plus the currently active one.
/// </summary>
public sealed class Variants : IDisposable
{
    private readonly IVSEDevice _device;
    private readonly DeviceEventManager _eventManager;

    public IReadOnlyList<Variant> All { get; }

    public int ActiveVariantId
    {
        get => _device.Variant.Id;
        set => _device.Commands.SetVariant(value);
    }

    /// <summary>
    /// Raised whenever the device switches its active variant.
    /// </summary>
    public event Action<Variant>? ActiveVariantChanged;

    public Variants(IVSEDevice device, DeviceEventManager eventManager)
    {
        _device = device;
        _eventManager = eventManager;
        All = [.. device.Parameters.Variants.Select(v => new Variant(v))];

        _eventManager.VariantChanged += OnVariantChanged;
    }

    private void OnVariantChanged(VariantChangedEvent evt)
    {
        Variant variant = All.FirstOrDefault(v => v.Id == evt.Variant.Id) ?? new Variant(evt.Variant);
        ActiveVariantChanged?.Invoke(variant);
    }

    public void Dispose() => _eventManager.VariantChanged -= OnVariantChanged;
}
