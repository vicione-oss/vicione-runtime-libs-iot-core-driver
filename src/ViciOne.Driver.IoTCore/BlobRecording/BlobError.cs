namespace ViciOne.Driver.IoTCore.BlobRecording;

internal sealed class BlobError(int code, string message, object? data = null)
{
    public int Code { get; } = code;
    public object? Data { get; } = data;
    public string Message { get; } = message;

    public BlobError(BlobErrorState code, string message, object? data = null)
        : this((int)code, message, data) { }

    public override string ToString()
        => $"{GetType().Name}: {Message} ({Code})";
}
