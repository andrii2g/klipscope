using KlipScope.Core.Abstractions;
using KlipScope.Core.Models;

namespace KlipScope.Core.Diagnostics;

public sealed class DiagnosticsService(IPrinterClient printerClient, ResolvedConnectionOptions options, IClock clock)
    : IPrinterDiagnosticsService
{
    public async Task<DiagnosticsReport> RunAsync(CancellationToken cancellationToken)
    {
        var checks = await printerClient.RunDiagnosticsAsync(cancellationToken);
        var nextSteps = checks
            .Where(check => check.Status is DiagnosticCheckStatus.Fail or DiagnosticCheckStatus.Warn)
            .Select(check => $"Review {check.Name.ToLowerInvariant()}: {check.Message}")
            .DefaultIfEmpty("No follow-up actions suggested.")
            .ToArray();

        return new DiagnosticsReport(options.Host, options.Transport, clock.UtcNow, checks, nextSteps);
    }
}
