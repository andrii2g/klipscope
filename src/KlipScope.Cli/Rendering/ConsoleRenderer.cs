using KlipScope.Core.Models;

namespace KlipScope.Cli.Rendering;

internal static class ConsoleRenderer
{
    public static void WriteLine(string text) => Console.WriteLine(text);
    public static void WriteError(string text) => Console.Error.WriteLine(text);
    public static void WriteJson<T>(CommandResult<T> result) => WriteLine(JsonOutputRenderer.Render(result));
}
