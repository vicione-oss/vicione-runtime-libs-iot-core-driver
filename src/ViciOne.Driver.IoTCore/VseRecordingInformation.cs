using System;

namespace ViciOne.Driver.IoTCore;

internal sealed class VseRecordingInformation
{
    public required VseDevice Device { get; set; }
    public int Duration { get; set; }
    public required Action<Exception> HandleError { get; set; }
    public required string Identifier { get; set; }
    public bool IgnoreTimedValueArra { get; set; }
    public int SamplingRate { get; set; }
    public int SensorIndex { get; set; }
}
