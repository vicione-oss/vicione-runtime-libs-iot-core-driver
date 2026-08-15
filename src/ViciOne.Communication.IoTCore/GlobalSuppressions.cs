using System.Diagnostics.CodeAnalysis;

// The DCP scanner routes its log messages through delegates across its packet pipeline and is deliberately kept
// on direct logging rather than migrated to source-generated messages.
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Scope = "type", Target = "~T:ViciOne.Communication.IoTCore.DcpScan.DcpScanner", Justification = "The scanner logs via delegates threaded through its packet pipeline; source-generated messages do not fit that shape.")]
[assembly: SuppressMessage("Usage", "CA2254:Template should be a static expression", Scope = "type", Target = "~T:ViciOne.Communication.IoTCore.DcpScan.DcpScanner", Justification = "The scanner logs free-form diagnostic messages routed through delegates; a static template is not applicable.")]
[assembly: SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Scope = "type", Target = "~T:ViciOne.Communication.IoTCore.DcpScan.DcpScanner", Justification = "The scan is an occasional operation; its interpolated diagnostic arguments are cheap and only built during a scan.")]

// The logger adapter forwards already-formatted messages from another logging interface, so it cannot use
// source-generated LoggerMessage delegates and its message is inherently dynamic.
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Scope = "type", Target = "~T:ViciOne.Communication.IoTCore.MicrosoftLoggerAdapter", Justification = "A pass-through adapter cannot use source-generated LoggerMessage delegates.")]
[assembly: SuppressMessage("Usage", "CA2254:Template should be a static expression", Scope = "type", Target = "~T:ViciOne.Communication.IoTCore.MicrosoftLoggerAdapter", Justification = "A pass-through adapter forwards an already-formatted, inherently dynamic message.")]
