using ifm.IoTCore.Common;
using ifm.IoTCore.Common.Exceptions;

namespace ViciOne.Communication.IoTCore.Extensions;

/// <summary>
/// Helpers for interpreting <see cref="IoTCoreException"/>.
/// </summary>
// TODO: Suppression should be removed when https://github.com/dotnet/sdk/issues/51681 is fixed and delivered.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Extension members syntax generates nested types")]
public static class IoTCoreExceptionExtensions
{
    extension(IoTCoreException exception)
    {
        /// <summary>
        /// Whether <paramref name="exception"/> represents a device timeout - either an explicit remote-service
        /// timeout response code, or an internal-error response whose message mentions a timeout.
        /// </summary>
        public bool IsTimeout
            => exception.ResponseCode == ResponseCodes.Timeout ||
               (exception.ResponseCode == ResponseCodes.InternalError && exception.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase));
    }
}
