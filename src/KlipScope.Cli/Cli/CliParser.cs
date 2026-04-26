namespace KlipScope.Cli.Cli;

internal static class CliParser
{
    public static CliGlobalOptions Parse(string[] args)
    {
        string? host = null;
        string? scheme = null;
        int? port = null;
        string? apiKey = null;
        var timeout = 5;
        var json = false;
        var verbose = false;
        var allowControl = false;
        var transport = "auto";
        int? klipperPort = null;
        var remaining = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--host":
                    host = ReadValue(args, ref index);
                    break;
                case "--scheme":
                    scheme = ReadValue(args, ref index);
                    break;
                case "--port":
                    port = int.Parse(ReadValue(args, ref index));
                    break;
                case "--api-key":
                    apiKey = ReadValue(args, ref index);
                    break;
                case "--timeout":
                    timeout = int.Parse(ReadValue(args, ref index));
                    break;
                case "--json":
                    json = true;
                    break;
                case "--verbose":
                    verbose = true;
                    break;
                case "--allow-control":
                    allowControl = true;
                    break;
                case "--transport":
                    transport = ReadValue(args, ref index);
                    break;
                case "--klipper-port":
                    klipperPort = int.Parse(ReadValue(args, ref index));
                    break;
                case "--version":
                    return new CliGlobalOptions(host, scheme, port, apiKey, timeout, json, verbose, allowControl, transport, klipperPort, "version", Array.Empty<string>());
                case "-h":
                case "--help":
                    return new CliGlobalOptions(host, scheme, port, apiKey, timeout, json, verbose, allowControl, transport, klipperPort, "help", Array.Empty<string>());
                default:
                    remaining.Add(args[index]);
                    break;
            }
        }

        var command = remaining.FirstOrDefault() ?? "help";
        return new CliGlobalOptions(host, scheme, port, apiKey, timeout, json, verbose, allowControl, transport, klipperPort, command, remaining.Skip(1).ToArray());
    }

    private static string ReadValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new InvalidOperationException($"Missing value for {args[index]}.");
        }

        index++;
        return args[index];
    }
}
