using System.Text.Json.Nodes;
using KlipScope.Core.Models;

namespace KlipScope.Core.Utilities;

public static class PrinterStatusMapper
{
    public static PrinterStatusSnapshot FromMoonraker(JsonNode? result, IEnumerable<string> temperatureObjects)
    {
        var status = JsonHelpers.GetObject(JsonHelpers.UnwrapResult(result), "status");
        return BuildSnapshot(TransportKind.Moonraker, status, temperatureObjects);
    }

    public static PrinterStatusSnapshot FromKlipper(JsonNode? result, IEnumerable<string> temperatureObjects)
    {
        var status = JsonHelpers.GetObject(JsonHelpers.UnwrapResult(result), "status")
            ?? JsonHelpers.UnwrapResult(result) as JsonObject;
        return BuildSnapshot(TransportKind.KlipperTcp, status, temperatureObjects);
    }

    private static PrinterStatusSnapshot BuildSnapshot(TransportKind transport, JsonNode? status, IEnumerable<string> temperatureObjects)
    {
        return new PrinterStatusSnapshot(
            transport,
            JsonHelpers.GetString(status, "webhooks", "state"),
            JsonHelpers.GetString(status, "webhooks", "state_message") ?? JsonHelpers.GetString(status, "webhooks", "message"),
            JsonHelpers.GetString(status, "print_stats", "state"),
            JsonHelpers.GetString(status, "print_stats", "filename"),
            JsonHelpers.GetDouble(status, "virtual_sdcard", "progress"),
            JsonHelpers.GetDouble(status, "print_stats", "print_duration"),
            JsonHelpers.GetDouble(status, "print_stats", "total_duration"),
            JsonHelpers.GetDouble(status, "toolhead", "estimated_print_time"),
            JsonHelpers.GetDoubleArray(status, "toolhead", "position"),
            TemperatureMapper.FromStatus(status, temperatureObjects),
            status?.DeepClone());
    }
}
