using System;

namespace ViciOne.Driver.IoTCore.DcpScan;

/// <summary>
/// Options for DcpScanner; no ILogger dependency.
/// </summary>
internal sealed class DcpScannerOptions
{
    /// <summary>Include loopback devices (default: false).</summary>
    public bool IncludeLoopbacks { get; set; }

    public bool IncludeVlanFrame { get; set; }

    /// <summary>
    /// Use narrow BPF filter limited to PN-DCP Identify (FrameId 0xFEFE/0xFEFF).
    /// If the filter fails to apply, scanner falls back to broad EtherType filter and suppresses noisy logs.
    /// (default: true)
    /// </summary>
    public bool NarrowFilter { get; set; } = true;

    /// <summary>Number of identify retries (default: 3).</summary>
    public int SendRetries { get; set; } = 3;

    /// <summary>Per-retry response window (default: 400 ms).</summary>
    public TimeSpan AttemptWindow { get; set; } = TimeSpan.FromMilliseconds(400);

    /// <summary>Extra time for straggling responses (default: 1200 ms).</summary>
    public TimeSpan StragglerWindow { get; set; } = TimeSpan.FromMilliseconds(1200);

    /// <summary>Max parallel adapters (default: Environment.ProcessorCount).</summary>
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
}
