using System.Text.Json;
using System.Text.Json.Nodes;

namespace KlipScope.Core.Utilities;

public static class JsonHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static JsonNode? UnwrapResult(JsonNode? node)
    {
        if (node is JsonObject obj && obj.TryGetPropertyValue("result", out var result))
        {
            return result;
        }

        return node;
    }

    public static string? GetString(JsonNode? node, params string[] path) =>
        Traverse(node, path)?.GetValue<string>();

    public static double? GetDouble(JsonNode? node, params string[] path)
    {
        var value = Traverse(node, path);
        return value is JsonValue jsonValue && jsonValue.TryGetValue<double>(out var number) ? number : null;
    }

    public static bool? GetBool(JsonNode? node, params string[] path)
    {
        var value = Traverse(node, path);
        return value is JsonValue jsonValue && jsonValue.TryGetValue<bool>(out var result) ? result : null;
    }

    public static JsonObject? GetObject(JsonNode? node, params string[] path) => Traverse(node, path) as JsonObject;

    public static JsonArray? GetArray(JsonNode? node, params string[] path) => Traverse(node, path) as JsonArray;

    public static IReadOnlyList<double>? GetDoubleArray(JsonNode? node, params string[] path)
    {
        var array = GetArray(node, path);
        if (array is null)
        {
            return null;
        }

        return array
            .Select(item => item is JsonValue jsonValue && jsonValue.TryGetValue<double>(out var number) ? (double?)number : null)
            .Where(number => number.HasValue)
            .Select(number => number!.Value)
            .ToArray();
    }

    private static JsonNode? Traverse(JsonNode? node, IReadOnlyList<string> path)
    {
        var current = node;
        foreach (var segment in path)
        {
            current = current?[segment];
            if (current is null)
            {
                return null;
            }
        }

        return current;
    }
}
