using KlipScope.Core.Diagnostics;

namespace KlipScope.Core.Abstractions;

public interface IPrinterDiagnosticsService
{
    Task<DiagnosticsReport> RunAsync(CancellationToken cancellationToken);
}
