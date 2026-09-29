using System.Diagnostics.CodeAnalysis;

// This is a console sample; its log arguments are cheap to build and clarity is worth more than gating them.
[assembly: SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Scope = "type", Target = "~T:VseSample.Program", Justification = "Sample console app; the log argument is a trivial mode-description lookup.")]
