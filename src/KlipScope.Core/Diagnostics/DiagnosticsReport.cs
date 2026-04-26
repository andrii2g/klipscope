using KlipScope.Core.Models;

namespace KlipScope.Core.Diagnostics;

public sealed record DiagnosticsReport(
    string Host,
    TransportKind Transport,
    DateTimeOffset CreatedAt,
    IReadOnlyList<DiagnosticCheck> Checks,
    IReadOnlyList<string> SuggestedNextSteps);
