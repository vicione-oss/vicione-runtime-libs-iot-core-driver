using System;

namespace ViciOne.Driver.IoTCore;

internal interface IManageableDevice : IDisposable
{
    string Address { get; }
    bool IgnoreReleaseDelay { get; }
    DateTime? ReleaseDate { get; set; }
}
