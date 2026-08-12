using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ViciOne.Communication.IoTCore.Recording;

/// <summary>
/// Helpers for building the IoTCore "single request" JSON envelope used by the BLOB channel's
/// iolreadacyclic/iolwriteacyclic requests, and for parsing property values back out of the JSON responses.
/// </summary>
internal static class HttpRequestMessageExtensions
{
    private const int REQUEST_CODE = 10;

    /// <summary>
    /// Attaches an IoTCore "single request" JSON envelope (request code, a random correlation ID, the target
    /// address, and optional payload data) as <paramref name="message"/>'s content.
    /// </summary>
    /// <param name="message">The request to attach the envelope to.</param>
    /// <param name="adr">The IoTCore element address the request targets.</param>
    /// <param name="data">Optional request-specific payload data.</param>
    /// <returns><paramref name="message"/>, for chaining.</returns>
    public static HttpRequestMessage AddIotCoreSingleRequest(this HttpRequestMessage message, string adr, object? data = null)
        => message.AddJsonContent(new
        {
            code = REQUEST_CODE,
            cid = Convert.ToInt16(RandomNumberGenerator.GetInt32(short.MaxValue)),
            adr,
            data
        });

    /// <summary>
    /// Serializes <paramref name="content"/> to JSON and sets it as <paramref name="message"/>'s request body.
    /// </summary>
    /// <param name="message">The request to attach the content to.</param>
    /// <param name="content">The object to serialize as the request body.</param>
    /// <returns><paramref name="message"/>, for chaining.</returns>
    private static HttpRequestMessage AddJsonContent(this HttpRequestMessage message, object content)
    {
        message.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
        return message;
    }

    /// <summary>
    /// Parses <paramref name="input"/> as JSON and converts the value of its top-level <paramref name="property"/> to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="input">The JSON response to parse.</param>
    /// <param name="property">The top-level property to read.</param>
    /// <returns>The property's value, converted to <typeparamref name="T"/>.</returns>
    public static T GetJsonPropertyValue<T>(this string input, string property)
    {
        string result = JObject.Parse(input)[property]?.ToString() ?? string.Empty;
        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses <paramref name="input"/> as JSON and converts the value of its nested
    /// <paramref name="property1"/>.<paramref name="property2"/> to <typeparamref name="T"/>.
    /// </summary>
    /// <param name="input">The JSON response to parse.</param>
    /// <param name="property1">The top-level property to descend into.</param>
    /// <param name="property2">The nested property, under <paramref name="property1"/>, to read.</param>
    /// <returns>The nested property's value, converted to <typeparamref name="T"/>.</returns>
    public static T GetJsonPropertyValue<T>(this string input, string property1, string property2)
    {
        string result = JObject.Parse(input)[property1]?[property2]?.ToString() ?? string.Empty;
        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Best-effort variant of <see cref="GetJsonPropertyValue{T}(string, string, string)"/> - returns
    /// <see langword="null"/> instead of throwing if <paramref name="input"/> isn't valid JSON or the nested
    /// property is missing.
    /// </summary>
    /// <param name="input">The JSON response to parse.</param>
    /// <param name="property1">The top-level property to descend into.</param>
    /// <param name="property2">The nested property, under <paramref name="property1"/>, to read.</param>
    /// <returns>The nested property's value as a string, or <see langword="null"/> if it couldn't be read.</returns>
    public static string? GetJsonPropertyValueAsString(this string input, string property1, string property2)
    {
        try
        {
            return input.GetJsonPropertyValue<string>(property1, property2);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
