using IFM.SDK.VSE;
using IFM.SDK.VSE.DataChannels;
using IFM.SDK.VSE.Parameters;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// A single VSE input. Only <see cref="InputType.External"/> inputs carry a value at all (VSE writes external
/// speed/reference values in through them rather than measuring them itself) - Dynamic/Analog/Digital inputs
/// feed into <see cref="DeviceObject"/>s instead and have no value of their own here.
/// </summary>
public sealed class Input
{
    private readonly IInput _instance;
    private readonly IDataLink? _dataLink;
    private float _value = float.NaN;

    public int Id => _instance.Id;
    public string Name => _instance.Name;
    public string Type => ReflectionUtils.InType2Str(_instance.Type);
    public InputType RawType => _instance.Type;
    public string Unit => _instance.Unit;

    /// <summary>
    /// Whether this input has a live value at all (only true for <see cref="InputType.External"/>).
    /// </summary>
    public bool HasValue { get; }

    /// <summary>
    /// Whether this input participates in the shared monitoring channel - currently always equal to
    /// <see cref="HasValue"/>, kept as its own property since <see cref="DeviceObject.DynamicValuesMonitoring"/>
    /// has the analogous meaning for objects.
    /// </summary>
    public bool DynamicValueMonitoring => HasValue;

    public float Value
    {
        get => _value;
        set => SetValue(value);
    }

    /// <summary>
    /// Raised whenever the VSE-SDK monitoring channel delivers a new sample that changes <see cref="Value"/>.
    /// </summary>
    public event Action? Changed;

    public Input(IInput instance, IDataLink dataLink)
    {
        _instance = instance;

        if (instance.Type != InputType.External)
            return;

        _dataLink = dataLink;
        HasValue = true;
        IReadDataChannel<IIODataSample> readChannel = dataLink.GetReadDataChannel(instance);
        _value = readChannel.Read().GetValue();
    }

    private void SetValue(float value)
    {
        if (_dataLink is null)
            throw new InvalidOperationException($"Cannot set the value of a '{Type}' input - only {nameof(InputType.External)} inputs are writable.");

        IWriteDataChannel writeChannel = _dataLink.GetWriteDataChannel(WriteDataType.Rpm, _instance.Id);
        writeChannel.Write(value);
    }

    internal void Received(IIODataSample dataSample)
    {
        float value = dataSample.GetValue();
        if (value.IsNearlyEqualTo(_value))
            return;

        _value = value;
        Changed?.Invoke();
    }
}
