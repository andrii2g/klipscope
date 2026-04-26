namespace KlipScope.Core.Safety;

public static class GcodeSafety
{
    public static bool ContainsEmergencyStop(string script) =>
        script.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(line => string.Equals(line, "M112", StringComparison.OrdinalIgnoreCase));
}
