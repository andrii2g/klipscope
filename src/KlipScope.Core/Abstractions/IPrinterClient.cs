using System.Text.Json.Nodes;
using KlipScope.Core.Diagnostics;
using KlipScope.Core.Models;

namespace KlipScope.Core.Abstractions;

public interface IPrinterClient
{
    TransportKind Transport { get; }
    string TransportDisplayName { get; }
    Task<PrinterInfoSnapshot> GetInfoAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListObjectsAsync(CancellationToken cancellationToken);
    Task<JsonNode?> QueryObjectsAsync(IReadOnlyDictionary<string, object?> objects, CancellationToken cancellationToken);
    Task<PrinterStatusSnapshot> GetStatusAsync(CancellationToken cancellationToken);
    Task<TemperatureSnapshot[]> GetTemperaturesAsync(CancellationToken cancellationToken);
    Task<EndstopSnapshot> QueryEndstopsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DiagnosticCheck>> RunDiagnosticsAsync(CancellationToken cancellationToken);
    Task<string> RunGcodeScriptAsync(string script, CancellationToken cancellationToken);
}
