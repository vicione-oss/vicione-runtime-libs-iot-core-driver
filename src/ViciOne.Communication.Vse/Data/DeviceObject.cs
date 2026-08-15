using IFM.SDK.VSE;
using IFM.SDK.VSE.Parameters;
using ViciOne.Communication.Vse.DataProcessing;

namespace ViciOne.Communication.Vse.Data;

/// <summary>
/// A single Vse object (a monitored vibration/frequency source), kept up to date by <see cref="DeviceObjects"/>
/// from the device's own monitoring channel push. Average/Maximum/Minimum are derived from a sliding window
/// of received samples rather than read directly from the device.
/// </summary>
public sealed class DeviceObject : IDisposable
{
    private const string AlgoMean = "mean";
    private const string AlgoMax = "max";
    private const string AlgoMin = "min";

    private readonly IObject _instance;
    private readonly SlidingWindowDataProcessor _processor = new(0);
    private readonly ValueUpdateFrequencyCollector _frequencyCollector = new(1000);
    private uint _averagingPeriod;
    private ObjectFlags _flags;
    private bool _hasFlags;
    private bool _dynamicValuesMonitoring;

    public int Id => _instance.Id;
    public string Name => _instance.Name;
    public string Type => ReflectionUtils.ObjectType2Str(_instance.Type);
    public string Unit => _instance.Unit;
    public int InputId => _instance.InputId;
    public string InputType => ReflectionUtils.InType2Str(_instance.InputType);
    public int RotValueId => _instance.RotTriggerId;
    public int? RefValueId => _instance.RefTriggerId;
    public float Warning => _instance.Warning;
    public float Damage => _instance.Damage;
    public float BaseLine => _instance.BaseLine;
    public Averaging Averaging => _instance.Averaging;
    public FrequencyResolution? FrequencyResolution => _instance.FrequencyResolution;
    public MeasureTime? MeasureTime => _instance.MeasureTime;
    public ushort ResponseDelay => _instance.ResponseDelay;

    /// <summary>
    /// Whether this is a "fast" object, i.e. one that is not dynamically monitored by default (see
    /// <see cref="DynamicValuesMonitoring"/>).
    /// </summary>
    public bool IsFastObject => _instance.IsFastObject;

    public uint AveragingPeriod
    {
        get => _averagingPeriod;
        set
        {
            _averagingPeriod = value;
            _processor.TimespanInMs = value;
        }
    }

    /// <summary>
    /// A human-readable description of the current <see cref="ObjectFlags"/> reported by the device.
    /// </summary>
    public string Flags => _flags.AsDescription();

    /// <summary>
    /// Set by <see cref="DeviceObjects"/> when the active variant deactivates this object - reflected in <see cref="State"/>.
    /// </summary>
    public bool DisabledByVariant { get; set; }

    public string State => DisabledByVariant switch
    {
        true => "inactive-by-variant",
        false when _flags.IsError() => "error",
        false => "ok",
    };

    public float RefValue => _processor.Calculate<ObjectDataSample>(AlgoMean).RefValue;
    public float RotSpeed => _processor.Calculate<ObjectDataSample>(AlgoMean).RotSpeed;

    // NaN while dynamic monitoring is off for this object (e.g. a fast object nobody enabled monitoring for)
    // rather than a stale/misleading last-known value.
    public float Average => DynamicValuesMonitoring ? _processor.Calculate<ObjectDataSample>(AlgoMean).Value : float.NaN;
    public float Maximum => DynamicValuesMonitoring ? _processor.Calculate<ObjectDataSample>(AlgoMax).Value : float.NaN;
    public float Minimum => DynamicValuesMonitoring ? _processor.Calculate<ObjectDataSample>(AlgoMin).Value : float.NaN;

    public float ValueOccuranceFrequency => _frequencyCollector.ValueFrequency;
    public uint DataAge => _frequencyCollector.LastDataAge;

    public bool DynamicValuesMonitoring
    {
        get => _dynamicValuesMonitoring;
        set
        {
            if (_dynamicValuesMonitoring == value)
                return;

            _dynamicValuesMonitoring = value;
            MonitoringStateChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// Raised whenever folding a new sample into the sliding window actually changes <see cref="Average"/>,
    /// <see cref="Maximum"/>, <see cref="Minimum"/>, <see cref="RefValue"/> or <see cref="RotSpeed"/> - compared
    /// before/after, since an unchanged raw sample can still shift these as older samples age out of the window.
    /// <see cref="DataAge"/>/<see cref="ValueOccuranceFrequency"/> are updated regardless but don't gate the
    /// event, being metadata about the sampling itself rather than the object's own reading.
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Raised whenever the device reports a change in <see cref="Flags"/>/<see cref="State"/> (e.g. entering or leaving an error state).
    /// </summary>
    public event Action? FlagsChanged;

    /// <summary>
    /// Raised when <see cref="DynamicValuesMonitoring"/> is toggled - <see cref="DeviceObjects"/> owns the shared
    /// monitoring channel and must add/remove this object's id from it accordingly.
    /// </summary>
    public event Action<bool>? MonitoringStateChanged;

    public DeviceObject(IObject instance, uint averagingPeriod)
    {
        _instance = instance;
        _processor.AddAlgorithm(new AlgorithmDeviceObjectMax(), AlgoMax);
        _processor.AddAlgorithm(new AlgorithmDeviceObjectMin(), AlgoMin);
        _processor.AddAlgorithm(new AlgorithmDeviceObjectMean(), AlgoMean);
        AveragingPeriod = averagingPeriod;

        _dynamicValuesMonitoring = !instance.IsFastObject;
    }

    internal void Received(IObjectDataSample dataSample)
    {
        float averageBefore = Average;
        float maximumBefore = Maximum;
        float minimumBefore = Minimum;
        float refValueBefore = RefValue;
        float rotSpeedBefore = RotSpeed;

        _processor.AddSamples(ObjectDataSample.FromDeviceObjectDataSample(dataSample));
        _frequencyCollector.AddValue();

        bool changed = !Average.IsNearlyEqualTo(averageBefore)
            || !Maximum.IsNearlyEqualTo(maximumBefore)
            || !Minimum.IsNearlyEqualTo(minimumBefore)
            || !RefValue.IsNearlyEqualTo(refValueBefore)
            || !RotSpeed.IsNearlyEqualTo(rotSpeedBefore);

        if (changed)
            Changed?.Invoke();

        // Gated on _hasFlags rather than comparing against _flags' own default (NoFlag): the device could
        // legitimately report NoFlag as its very first sample, which must still count as a change.
        if (_hasFlags && dataSample.Flags == _flags)
            return;

        _hasFlags = true;
        _flags = dataSample.Flags;
        FlagsChanged?.Invoke();
    }

    public void Dispose() => _processor.Dispose();
}
