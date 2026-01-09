using System;
using ifmIoTCore.Exceptions;

namespace ViciOne.Driver.IoTCore.Extensions;

internal static class IoTCoreExceptionExtensions
{
    public static bool IsTimeout(this IoTCoreException exception)
        => exception.ResponseCode == ifmIoTCore.Messages.ResponseCodes.RemoteServiceTimeout ||
           (exception.ResponseCode == ifmIoTCore.Messages.ResponseCodes.InternalError && exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
}
