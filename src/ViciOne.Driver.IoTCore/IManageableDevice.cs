using System;

namespace ViciOne.Driver.IoTCore;

internal interface IManageableDevice : IDisposable
{
    string Address { get; }
    DateTime? ReleaseDate { get; set; }
}
