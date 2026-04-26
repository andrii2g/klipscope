namespace KlipScope.Core.Models;

public sealed record TemperatureSnapshot(
    string Name,
    double? Temperature,
    double? Target,
    double? Power,
    double? Speed);
