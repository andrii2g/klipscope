using System.Text.Json.Nodes;

namespace KlipScope.Core.Models;

public sealed record PrinterInfoSnapshot(
    TransportKind Transport,
    string Host,
    int? Port,
    string? KlippyState,
    string? KlippyMessage,
    string? MoonrakerVersion,
    string? MoonrakerApiVersion,
    bool? KlippyConnected,
    IReadOnlyList<string> Components,
    IReadOnlyList<string> FailedComponents,
    IReadOnlyList<string> Warnings,
    string? Hostname,
    string? SoftwareVersion,
    string? ConfigFile,
    string? LogFile,
    JsonNode? Raw);
