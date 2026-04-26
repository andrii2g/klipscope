using System.Text.Json.Nodes;

namespace KlipScope.Core.Models;

public sealed record PrinterStatusSnapshot(
    TransportKind Transport,
    string? KlippyState,
    string? KlippyMessage,
    string? PrintState,
    string? Filename,
    double? Progress,
    double? PrintDuration,
    double? TotalDuration,
    double? EstimatedPrintTime,
    IReadOnlyList<double>? ToolheadPosition,
    TemperatureSnapshot[] Temperatures,
    JsonNode? Raw);
