namespace KlipScope.Moonraker.Http;

public sealed class MoonrakerHttpException(string message, int statusCode, string? body = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string? Body { get; } = body;
}
