using System.Text.Json;
using KlipScope.Core.Models;
using KlipScope.Core.Utilities;

namespace KlipScope.Cli.Rendering;

internal static class JsonOutputRenderer
{
    public static string Render<T>(CommandResult<T> result) =>
        JsonSerializer.Serialize(result, JsonHelpers.JsonOptions);
}
