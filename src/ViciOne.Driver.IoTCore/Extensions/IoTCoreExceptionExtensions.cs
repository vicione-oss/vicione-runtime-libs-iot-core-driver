using System;
using ifmIoTCore.Exceptions;

namespace ViciOne.Driver.IoTCore.Extensions;

internal static class IoTCoreExceptionExtensions
{
    extension(IoTCoreException exception)
    {
        public bool IsTimeout
            => exception.ResponseCode == ifmIoTCore.Messages.ResponseCodes.RemoteServiceTimeout ||
               (exception.ResponseCode == ifmIoTCore.Messages.ResponseCodes.InternalError && exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }
}
