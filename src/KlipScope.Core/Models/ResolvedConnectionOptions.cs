namespace KlipScope.Core.Models;

public sealed record ResolvedConnectionOptions(
    string Host,
    TransportKind Transport,
    int TimeoutSeconds,
    int? MoonrakerPort,
    string? Scheme,
    string? ApiKey,
    int? KlipperPort,
    bool JsonOutput,
    bool Verbose,
    bool AllowControl)
{
    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);

    public Uri BuildMoonrakerUri(string pathAndQuery)
    {
        var cleanPath = pathAndQuery.StartsWith('/') ? pathAndQuery[1..] : pathAndQuery;
        var builder = new UriBuilder(Scheme ?? "http", Host, MoonrakerPort ?? 7125, cleanPath);
        return builder.Uri;
    }
}
