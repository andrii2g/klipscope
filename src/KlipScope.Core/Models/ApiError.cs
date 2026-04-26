namespace KlipScope.Core.Models;

public sealed record ApiError(string Code, string Message, string? Details = null);
