using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.Driver.IoTCore.BlobRecording;

internal static class Extensions
{
    private const int REQUEST_CODE = 10;

    public static HttpRequestMessage AddIotCoreSingleRequest(this HttpRequestMessage message, string adr, object? data = null)
        => message.AddJsonContent(new
        {
            code = REQUEST_CODE,
            cid = Convert.ToInt16(RandomNumberGenerator.GetInt32(short.MaxValue)),
            adr,
            data
        });

    private static HttpRequestMessage AddJsonContent(this HttpRequestMessage message, object content)
    {
        message.Content = new StringContent(JsonConvert.SerializeObject(content), Encoding.UTF8, "application/json");
        return message;
    }

    public static T GetJsonPropertyValue<T>(this string input, string property)
    {
        string result = JObject.Parse(input)[property]?.ToString() ?? string.Empty;
        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }

    public static T GetJsonPropertyValue<T>(this string input, string property1, string property2)
    {
        string result = JObject.Parse(input)[property1]?[property2]?.ToString() ?? string.Empty;
        return (T)Convert.ChangeType(result, typeof(T), CultureInfo.InvariantCulture);
    }

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
