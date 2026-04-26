using System.Text.Json.Nodes;
using KlipScope.Cli.Rendering;
using KlipScope.Core.Abstractions;
using KlipScope.Core.Diagnostics;
using KlipScope.Core.Models;
using KlipScope.KlipperTcp;
using KlipScope.Moonraker;

namespace KlipScope.Cli.Cli;

internal static class CliApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = CliParser.Parse(args);

            if (options.Command == "help")
            {
                ConsoleRenderer.WriteLine(CliUsage.GetText());
                return ExitCodes.Success;
            }

            if (options.Command == "version")
            {
                ConsoleRenderer.WriteLine("0.1.0");
                return ExitCodes.Success;
            }

            var connection = CliGlobalOptionResolver.Resolve(options);
            var client = CreateClient(connection);
            return await ExecuteAsync(options, connection, client);
        }
        catch (InvalidOperationException ex)
        {
            ConsoleRenderer.WriteError(ex.Message);
            ConsoleRenderer.WriteLine(CliUsage.GetText());
            return ExitCodes.UsageError;
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteError(ex.Message);
            return ExitCodes.InternalError;
        }
    }

    private static IPrinterClient CreateClient(ResolvedConnectionOptions options) =>
        options.Transport switch
        {
            TransportKind.Moonraker => new MoonrakerPrinterClient(options),
            TransportKind.KlipperTcp => new KlipperTcpPrinterClient(options),
            _ => throw new InvalidOperationException("Unsupported transport.")
        };

    private static async Task<int> ExecuteAsync(CliGlobalOptions options, ResolvedConnectionOptions connection, IPrinterClient client)
    {
        using var cts = new CancellationTokenSource(connection.Timeout);
        switch (options.Command)
        {
            case "info":
            {
                var data = await client.GetInfoAsync(cts.Token);
                return Write(options, CommandResult<PrinterInfoSnapshot>.Success("info", client.Transport, data), HumanOutputRenderer.RenderInfo(data));
            }
            case "status":
            {
                var data = await client.GetStatusAsync(cts.Token);
                return Write(options, CommandResult<PrinterStatusSnapshot>.Success("status", client.Transport, data), HumanOutputRenderer.RenderStatus(data));
            }
            case "temps":
            {
                var data = await client.GetTemperaturesAsync(cts.Token);
                return Write(options, CommandResult<TemperatureSnapshot[]>.Success("temps", client.Transport, data), HumanOutputRenderer.RenderTemperatures(data));
            }
            case "endstops":
            {
                var data = await client.QueryEndstopsAsync(cts.Token);
                return Write(options, CommandResult<EndstopSnapshot>.Success("endstops", client.Transport, data), HumanOutputRenderer.RenderEndstops(data));
            }
            case "objects":
            {
                if (options.CommandArguments.Count > 1 && options.CommandArguments[0] == "--query")
                {
                    var queryResult = await client.QueryObjectsAsync(new Dictionary<string, object?> { [options.CommandArguments[1]] = null }, cts.Token);
                    return Write(options, CommandResult<JsonNode?>.Success("objects", client.Transport, queryResult), queryResult?.ToJsonString() ?? "null");
                }

                var data = await client.ListObjectsAsync(cts.Token);
                return Write(options, CommandResult<IReadOnlyList<string>>.Success("objects", client.Transport, data), HumanOutputRenderer.RenderObjects(data));
            }
            case "diag":
            {
                var diagnostics = new DiagnosticsService(client, connection, new SystemClock());
                var data = await diagnostics.RunAsync(cts.Token);
                return Write(options, CommandResult<DiagnosticsReport>.Success("diag", client.Transport, data), HumanOutputRenderer.RenderDiagnostics(data));
            }
            case "gcode":
            {
                if (!connection.AllowControl)
                {
                    ConsoleRenderer.WriteError("gcode requires --allow-control.");
                    return ExitCodes.SafetyBlocked;
                }

                var script = string.Join(' ', options.CommandArguments);
                if (string.IsNullOrWhiteSpace(script))
                {
                    throw new InvalidOperationException("gcode requires a script argument.");
                }

                var data = await client.RunGcodeScriptAsync(script, cts.Token);
                return Write(options, CommandResult<string>.Success("gcode", client.Transport, data), data);
            }
            case "watch":
            case "terminal":
            case "bridge":
                ConsoleRenderer.WriteError($"{options.Command} is not implemented in this iteration.");
                return ExitCodes.UsageError;
            default:
                throw new InvalidOperationException($"Unknown command '{options.Command}'.");
        }
    }

    private static int Write<T>(CliGlobalOptions options, CommandResult<T> result, string text)
    {
        if (options.Json)
        {
            ConsoleRenderer.WriteJson(result);
        }
        else
        {
            ConsoleRenderer.WriteLine(text);
        }

        return ExitCodes.Success;
    }
}
