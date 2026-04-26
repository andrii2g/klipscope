using KlipScope.Core.Models;

namespace KlipScope.Cli.Cli;

internal sealed record CliGlobalOptions(
    string? Host,
    string? Scheme,
    int? Port,
    string? ApiKey,
    int TimeoutSeconds,
    bool Json,
    bool Verbose,
    bool AllowControl,
    string Transport,
    int? KlipperPort,
    string Command,
    IReadOnlyList<string> CommandArguments);

internal static class CliGlobalOptionResolver
{
    public static ResolvedConnectionOptions Resolve(CliGlobalOptions options)
    {
        var host = options.Host ?? Environment.GetEnvironmentVariable("KLIPSCOPE_HOST");
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new InvalidOperationException("Missing --host and KLIPSCOPE_HOST.");
        }

        var timeout = options.TimeoutSeconds > 0
            ? options.TimeoutSeconds
            : int.TryParse(Environment.GetEnvironmentVariable("KLIPSCOPE_TIMEOUT"), out var envTimeout) ? envTimeout : 5;

        var transport = ResolveTransport(options);
        if (transport == TransportKind.KlipperTcp)
        {
            if (options.KlipperPort is null)
            {
                throw new InvalidOperationException("Direct Klipper transport requires --klipper-port.");
            }

            return new ResolvedConnectionOptions(host, transport, timeout, null, null, null, options.KlipperPort, options.Json, options.Verbose, options.AllowControl);
        }

        var port = options.Port ?? (int.TryParse(Environment.GetEnvironmentVariable("KLIPSCOPE_PORT"), out var envPort) ? envPort : 7125);
        var scheme = options.Scheme ?? Environment.GetEnvironmentVariable("KLIPSCOPE_SCHEME") ?? "http";
        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("KLIPSCOPE_API_KEY");
        return new ResolvedConnectionOptions(host, transport, timeout, port, scheme, apiKey, null, options.Json, options.Verbose, options.AllowControl);
    }

    private static TransportKind ResolveTransport(CliGlobalOptions options) =>
        options.Transport.ToLowerInvariant() switch
        {
            "moonraker" => TransportKind.Moonraker,
            "klipper-tcp" => TransportKind.KlipperTcp,
            "auto" when options.KlipperPort.HasValue => TransportKind.KlipperTcp,
            "auto" => TransportKind.Moonraker,
            _ => throw new InvalidOperationException("Unsupported --transport. Use auto, moonraker, or klipper-tcp.")
        };
}
