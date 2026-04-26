namespace KlipScope.Core.Diagnostics;

public sealed record DiagnosticCheck(
    string Name,
    DiagnosticCheckStatus Status,
    string Message,
    string? Details = null);
