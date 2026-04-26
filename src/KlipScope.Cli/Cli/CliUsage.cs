namespace KlipScope.Cli.Cli;

internal static class CliUsage
{
    public static string GetText() =>
        """
        klipscope [global-options] <command> [command-options]

        Global options:
          --host <hostname>                 Printer host or IP
          --transport <auto|moonraker|klipper-tcp>
          --port <number>                   Moonraker port only
          --klipper-port <number>           Direct Klipper tunnel port
          --scheme <http|https>             Moonraker scheme
          --api-key <key>                   Moonraker API key
          --timeout <seconds>               Request timeout, default 5
          --json                            Emit JSON only
          --verbose                         Emit extra details to stderr
          --allow-control                   Allow control-capable commands
          --version                         Print version
          -h, --help                        Print help

        Commands:
          info
          status
          temps
          endstops
          objects
          diag
          gcode <script>
          watch
          terminal
          bridge
        """;
}
