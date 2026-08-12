namespace ViciOne.Communication.IoTCore.Recording;

internal sealed class BlobError
{
    public int Code { get; }
    public object? Data { get; }
    public string Message { get; }

    internal BlobError(int code, string message, object? data = null)
    {
        Code = code;
        Message = message;
        Data = data;
    }

    internal BlobError(BlobErrorState code, string message, object? data = null)
        : this((int)code, message, data) { }

    public override string ToString()
        => $"{GetType().Name}: {Message} ({Code})";
}
