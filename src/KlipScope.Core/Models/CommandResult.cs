namespace KlipScope.Core.Models;

public sealed record CommandResult<T>(
    bool Ok,
    string Command,
    string Transport,
    T? Data,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ApiError> Errors)
{
    public static CommandResult<T> Success(string command, TransportKind transport, T? data, IReadOnlyList<string>? warnings = null) =>
        new(true, command, transport.ToWireValue(), data, warnings ?? Array.Empty<string>(), Array.Empty<ApiError>());
}
