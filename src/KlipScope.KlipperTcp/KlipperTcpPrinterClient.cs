using System.Text.Json.Nodes;
using KlipScope.Core.Abstractions;
using KlipScope.Core.Diagnostics;
using KlipScope.Core.Models;
using KlipScope.Core.Safety;
using KlipScope.Core.Utilities;

namespace KlipScope.KlipperTcp;

public sealed class KlipperTcpPrinterClient(ResolvedConnectionOptions options) : IPrinterClient
{
    private readonly KlipperTcpRpcClient _rpcClient = new(options);

    public TransportKind Transport => TransportKind.KlipperTcp;
    public string TransportDisplayName => "Klipper TCP";

    public async Task<PrinterInfoSnapshot> GetInfoAsync(CancellationToken cancellationToken)
    {
        var node = JsonHelpers.UnwrapResult(await _rpcClient.CallAsync("info", new { }, cancellationToken));
        return new PrinterInfoSnapshot(
            Transport,
            options.Host,
            options.KlipperPort,
            JsonHelpers.GetString(node, "state"),
            JsonHelpers.GetString(node, "state_message"),
            null,
            null,
            null,
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            JsonHelpers.GetString(node, "hostname"),
            JsonHelpers.GetString(node, "software_version"),
            JsonHelpers.GetString(node, "config_file"),
            JsonHelpers.GetString(node, "log_file"),
            node?.DeepClone());
    }

    public async Task<IReadOnlyList<string>> ListObjectsAsync(CancellationToken cancellationToken)
    {
        var node = JsonHelpers.UnwrapResult(await _rpcClient.CallAsync("objects/list", null, cancellationToken));
        return (node?["objects"] as JsonArray)?
            .Select(item => item?.GetValue<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToArray()
            ?? Array.Empty<string>();
    }

    public Task<JsonNode?> QueryObjectsAsync(IReadOnlyDictionary<string, object?> objects, CancellationToken cancellationToken) =>
        _rpcClient.CallAsync("objects/query", new { objects }, cancellationToken);

    public async Task<PrinterStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken)
    {
        var objects = await ListObjectsAsync(cancellationToken);
        var temperatureObjects = TemperatureMapper.DiscoverTemperatureObjectNames(objects);
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

        var result = await QueryObjectsAsync(query, cancellationToken);
        return PrinterStatusMapper.FromKlipper(result, temperatureObjects);
    }

    public async Task<TemperatureSnapshot[]> GetTemperaturesAsync(CancellationToken cancellationToken)
    {
        var objects = await ListObjectsAsync(cancellationToken);
        var temperatureObjects = TemperatureMapper.DiscoverTemperatureObjectNames(objects);
        var result = await QueryObjectsAsync(temperatureObjects.ToDictionary(name => name, _ => (object?)null), cancellationToken);
        var status = JsonHelpers.GetObject(JsonHelpers.UnwrapResult(result), "status") ?? JsonHelpers.UnwrapResult(result) as JsonObject;
        return TemperatureMapper.FromStatus(status, temperatureObjects);
    }

    public async Task<EndstopSnapshot> QueryEndstopsAsync(CancellationToken cancellationToken)
    {
        var node = JsonHelpers.UnwrapResult(await _rpcClient.CallAsync("query_endstops/status", null, cancellationToken));
        var source = node as JsonObject ?? JsonHelpers.GetObject(node, "status");
        var states = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (source is not null)
        {
            foreach (var pair in source)
            {
                if (pair.Value is not null)
                {
                    states[pair.Key] = pair.Value.ToJsonString().Trim('"');
                }
            }
        }

        return new EndstopSnapshot(states);
    }

    public async Task<IReadOnlyList<DiagnosticCheck>> RunDiagnosticsAsync(CancellationToken cancellationToken)
    {
        var checks = new List<DiagnosticCheck>();
        try
        {
            await GetInfoAsync(cancellationToken);
            checks.Add(new DiagnosticCheck("Klipper tunnel", DiagnosticCheckStatus.Pass, "Connected to the Klipper TCP tunnel."));
        }
        catch (Exception ex)
        {
            checks.Add(new DiagnosticCheck("Klipper tunnel", DiagnosticCheckStatus.Fail, ex.Message));
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

        return checks;
    }

    public async Task<string> RunGcodeScriptAsync(string script, CancellationToken cancellationToken)
    {
        var method = GcodeSafety.ContainsEmergencyStop(script) ? "emergency_stop" : "gcode/script";
        var parameters = method == "gcode/script" ? new { script } : null;
        var result = JsonHelpers.UnwrapResult(await _rpcClient.CallAsync(method, parameters, cancellationToken));
        return result?.ToJsonString() ?? "ok";
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
}
