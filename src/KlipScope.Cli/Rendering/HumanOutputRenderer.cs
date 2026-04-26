using System.Text;
using KlipScope.Core.Diagnostics;
using KlipScope.Core.Models;

namespace KlipScope.Cli.Rendering;

internal static class HumanOutputRenderer
{
    public static string RenderInfo(PrinterInfoSnapshot data)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Transport: {data.Transport.ToWireValue()}");
        builder.AppendLine($"Host: {data.Host}:{data.Port}");
        builder.AppendLine($"Klippy: {data.KlippyState ?? "unknown"}");
        if (!string.IsNullOrWhiteSpace(data.KlippyMessage))
        {
            builder.AppendLine($"Message: {data.KlippyMessage}");
        }

        if (!string.IsNullOrWhiteSpace(data.MoonrakerVersion))
        {
            builder.AppendLine($"Moonraker: {data.MoonrakerVersion} (API {data.MoonrakerApiVersion ?? "unknown"})");
        }

        if (!string.IsNullOrWhiteSpace(data.SoftwareVersion))
        {
            builder.AppendLine($"Software: {data.SoftwareVersion}");
        }

        if (!string.IsNullOrWhiteSpace(data.Hostname))
        {
            builder.AppendLine($"Hostname: {data.Hostname}");
        }

        return builder.ToString().TrimEnd();
    }

    public static string RenderStatus(PrinterStatusSnapshot data)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Transport: {data.Transport.ToWireValue()}");
        builder.AppendLine($"Klippy: {data.KlippyState ?? "unknown"}");
        builder.AppendLine($"Print: {data.PrintState ?? "unknown"}");
        if (!string.IsNullOrWhiteSpace(data.Filename))
        {
            builder.AppendLine($"File: {data.Filename}");
        }

        if (data.Progress.HasValue)
        {
            builder.AppendLine($"Progress: {data.Progress:P1}");
        }

        if (data.ToolheadPosition is { Count: > 0 })
        {
            var labels = new[] { "X", "Y", "Z", "E" };
            builder.AppendLine($"Toolhead: {string.Join(" ", data.ToolheadPosition.Select((value, index) => $"{labels[Math.Min(index, labels.Length - 1)]}={value:0.00}"))}");
        }

        if (data.Temperatures.Length > 0)
        {
            builder.AppendLine("Temperatures:");
            foreach (var item in data.Temperatures)
            {
                builder.AppendLine($"  {item.Name,-16} {FormatTemperature(item)}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    public static string RenderEndstops(EndstopSnapshot data)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Endstops:");
        foreach (var item in data.States)
        {
            builder.AppendLine($"  {item.Key}: {item.Value}");
        }

        return builder.ToString().TrimEnd();
    }

    public static string RenderObjects(IReadOnlyList<string> objects) =>
        string.Join(Environment.NewLine, objects);

    public static string RenderTemperatures(IEnumerable<TemperatureSnapshot> temperatures)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Temperatures:");
        foreach (var item in temperatures)
        {
            builder.AppendLine($"  {item.Name,-16} {FormatTemperature(item)}");
        }

        return builder.ToString().TrimEnd();
    }

    public static string RenderDiagnostics(DiagnosticsReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Diagnostics for {report.Host} [{report.Transport.ToWireValue()}]");
        builder.AppendLine();
        foreach (var check in report.Checks)
        {
            builder.AppendLine($"{check.Status.ToString().ToUpperInvariant(),-4} {check.Name,-18} {check.Message}");
        }

        builder.AppendLine();
        builder.AppendLine("Suggested next steps:");
        foreach (var item in report.SuggestedNextSteps)
        {
            builder.AppendLine($"- {item}");
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatTemperature(TemperatureSnapshot snapshot) =>
        snapshot.Target.HasValue
            ? $"{snapshot.Temperature:0.0} / {snapshot.Target:0.0} C"
            : snapshot.Temperature?.ToString("0.0 C") ?? "n/a";
}
