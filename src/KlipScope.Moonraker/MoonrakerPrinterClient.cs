using System.Text.Json.Nodes;
using KlipScope.Core.Abstractions;
using KlipScope.Core.Diagnostics;
using KlipScope.Core.Models;
using KlipScope.Core.Safety;
using KlipScope.Core.Utilities;
using KlipScope.Moonraker.Http;

namespace KlipScope.Moonraker;

public sealed class MoonrakerPrinterClient : IPrinterClient
{
    private readonly MoonrakerHttpClient _httpClient;
    private readonly ResolvedConnectionOptions _options;

    public MoonrakerPrinterClient(ResolvedConnectionOptions options)
    {
        _options = options;
        _httpClient = new MoonrakerHttpClient(new HttpClient(), options);
    }

    public TransportKind Transport => TransportKind.Moonraker;
    public string TransportDisplayName => "Moonraker";

    public async Task<PrinterInfoSnapshot> GetInfoAsync(CancellationToken cancellationToken)
    {
        var server = JsonHelpers.UnwrapResult(await _httpClient.GetJsonNodeAsync("/server/info", cancellationToken));
        var printer = JsonHelpers.UnwrapResult(await TryGetOptionalAsync("/printer/info", cancellationToken));
        return new PrinterInfoSnapshot(
            Transport,
            _options.Host,
            _options.MoonrakerPort,
            JsonHelpers.GetString(printer, "state") ?? JsonHelpers.GetString(server, "klippy_state"),
            JsonHelpers.GetString(printer, "state_message"),
            JsonHelpers.GetString(server, "moonraker_version"),
            JsonHelpers.GetString(server, "api_version_string"),
            JsonHelpers.GetBool(server, "klippy_connected"),
            ReadStringList(server?["components"]),
            ReadStringList(server?["failed_components"]),
            ReadStringList(server?["warnings"]),
            JsonHelpers.GetString(printer, "hostname"),
            JsonHelpers.GetString(printer, "software_version"),
            JsonHelpers.GetString(printer, "config_file"),
            JsonHelpers.GetString(printer, "log_file"),
            new JsonObject
            {
                ["server"] = server?.DeepClone(),
                ["printer"] = printer?.DeepClone()
            });
    }

    public async Task<IReadOnlyList<string>> ListObjectsAsync(CancellationToken cancellationToken)
    {
        var result = JsonHelpers.UnwrapResult(await _httpClient.GetJsonNodeAsync("/printer/objects/list", cancellationToken));
        return ReadStringList(result?["objects"]);
    }

    public Task<JsonNode?> QueryObjectsAsync(IReadOnlyDictionary<string, object?> objects, CancellationToken cancellationToken) =>
        _httpClient.PostJsonNodeAsync("/printer/objects/query", new Dictionary<string, object?> { ["objects"] = objects }, cancellationToken);

    public async Task<PrinterStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken)
    {
        var objects = await ListObjectsAsync(cancellationToken);
        var temperatureObjects = TemperatureMapper.DiscoverTemperatureObjectNames(objects);
        var query = BuildStatusQuery(temperatureObjects);
        var result = await QueryObjectsAsync(query, cancellationToken);
        return PrinterStatusMapper.FromMoonraker(result, temperatureObjects);
    }

    public async Task<TemperatureSnapshot[]> GetTemperaturesAsync(CancellationToken cancellationToken)
    {
        var objects = await ListObjectsAsync(cancellationToken);
        var temperatureObjects = TemperatureMapper.DiscoverTemperatureObjectNames(objects);
        var query = temperatureObjects.ToDictionary(name => name, _ => (object?)null);
        var result = await QueryObjectsAsync(query, cancellationToken);
        var status = JsonHelpers.GetObject(JsonHelpers.UnwrapResult(result), "status");
        return TemperatureMapper.FromStatus(status, temperatureObjects);
    }

    public async Task<EndstopSnapshot> QueryEndstopsAsync(CancellationToken cancellationToken)
    {
        var node = JsonHelpers.UnwrapResult(await _httpClient.GetJsonNodeAsync("/printer/query_endstops/status", cancellationToken));
        return new EndstopSnapshot(ReadStateDictionary(node));
    }

    public async Task<IReadOnlyList<DiagnosticCheck>> RunDiagnosticsAsync(CancellationToken cancellationToken)
    {
        var checks = new List<DiagnosticCheck>();
        try
        {
            var info = await GetInfoAsync(cancellationToken);
            checks.Add(new DiagnosticCheck("Moonraker API", DiagnosticCheckStatus.Pass, "Moonraker responded."));
            checks.Add(new DiagnosticCheck("Klippy state", info.KlippyConnected is false ? DiagnosticCheckStatus.Fail : DiagnosticCheckStatus.Pass, info.KlippyState ?? "Unknown state."));
        }
        catch (Exception ex)
        {
            checks.Add(new DiagnosticCheck("Moonraker API", DiagnosticCheckStatus.Fail, ex.Message));
            return checks;
        }

        await AddCheckAsync(checks, "Objects", async () =>
        {
            var objects = await ListObjectsAsync(cancellationToken);
            return new DiagnosticCheck("Objects", objects.Count > 0 ? DiagnosticCheckStatus.Pass : DiagnosticCheckStatus.Warn, $"Loaded objects: {objects.Count}.");
        });

        await AddCheckAsync(checks, "Status", async () =>
        {
            await GetStatusAsync(cancellationToken);
            return new DiagnosticCheck("Status", DiagnosticCheckStatus.Pass, "Status query succeeded.");
        });

        await AddCheckAsync(checks, "Endstops", async () =>
        {
            await QueryEndstopsAsync(cancellationToken);
            return new DiagnosticCheck("Endstops", DiagnosticCheckStatus.Pass, "Endstop query succeeded.");
        });

        await AddCheckAsync(checks, "G-code store", async () =>
        {
            await _httpClient.GetJsonNodeAsync("/server/gcode_store?count=20", cancellationToken);
            return new DiagnosticCheck("G-code store", DiagnosticCheckStatus.Info, "Fetched recent Moonraker messages.");
        });

        return checks;
    }

    public async Task<string> RunGcodeScriptAsync(string script, CancellationToken cancellationToken)
    {
        if (GcodeSafety.ContainsEmergencyStop(script))
        {
            await _httpClient.PostJsonNodeAsync("/printer/emergency_stop", new { }, cancellationToken);
            return "Emergency stop requested.";
        }

        var result = JsonHelpers.UnwrapResult(await _httpClient.PostJsonNodeAsync("/printer/gcode/script", new { script }, cancellationToken));
        return result?.ToJsonString() ?? "ok";
    }

    private async Task<JsonNode?> TryGetOptionalAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetJsonNodeAsync(path, cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private static async Task AddCheckAsync(List<DiagnosticCheck> checks, string name, Func<Task<DiagnosticCheck>> action)
    {
        try
        {
            checks.Add(await action());
        }
        catch (Exception ex)
        {
            checks.Add(new DiagnosticCheck(name, DiagnosticCheckStatus.Fail, ex.Message));
        }
    }

    private static IReadOnlyList<string> ReadStringList(JsonNode? node) =>
        node is JsonArray array
            ? array.Select(item => item?.GetValue<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Cast<string>().ToArray()
            : Array.Empty<string>();

    private static IReadOnlyDictionary<string, string> ReadStateDictionary(JsonNode? node)
    {
        var source = node as JsonObject ?? JsonHelpers.GetObject(node, "status");
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (source is null)
        {
            return result;
        }

        foreach (var pair in source)
        {
            if (pair.Value is not null)
            {
                result[pair.Key] = pair.Value.ToJsonString().Trim('"');
            }
        }

        return result;
    }

    private static IReadOnlyDictionary<string, object?> BuildStatusQuery(IReadOnlyList<string> temperatureObjects)
    {
        var query = new Dictionary<string, object?>
        {
            ["webhooks"] = null,
            ["print_stats"] = null,
            ["virtual_sdcard"] = null,
            ["toolhead"] = new[] { "position", "estimated_print_time" }
        };

        foreach (var name in temperatureObjects)
        {
            query[name] = null;
        }

        return query;
    }
}
