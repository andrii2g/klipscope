using System.Text.Json.Nodes;
using KlipScope.Core.Models;

namespace KlipScope.Core.Utilities;

public static class TemperatureMapper
{
    public static IReadOnlyList<string> DiscoverTemperatureObjectNames(IEnumerable<string> objectNames)
    {
        return objectNames
            .Where(name =>
                name.StartsWith("extruder", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("heater_bed", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("heater_generic ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("temperature_sensor ", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("temperature_fan ", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static TemperatureSnapshot[] FromStatus(JsonNode? status, IEnumerable<string> objectNames)
    {
        return objectNames
            .Select(name => CreateSnapshot(name, status?[name]))
            .Where(snapshot => snapshot is not null)
            .Cast<TemperatureSnapshot>()
            .ToArray();
    }

    private static TemperatureSnapshot? CreateSnapshot(string objectName, JsonNode? node)
    {
        if (node is not JsonObject)
        {
            return null;
        }

        var current = JsonHelpers.GetDouble(node, "temperature");
        var target = JsonHelpers.GetDouble(node, "target");
        var power = JsonHelpers.GetDouble(node, "power");
        var speed = JsonHelpers.GetDouble(node, "speed");
        if (current is null && target is null && power is null && speed is null)
        {
            return null;
        }

        return new TemperatureSnapshot(GetDisplayName(objectName), current, target, power, speed);
    }

    private static string GetDisplayName(string objectName)
    {
        var parts = objectName.Split(' ', 2, StringSplitOptions.TrimEntries);
        return parts.Length == 2 ? parts[1] : objectName;
    }
}
