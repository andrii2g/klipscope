namespace KlipScope.Core.Models;

public sealed record EndstopSnapshot(IReadOnlyDictionary<string, string> States);
