# KlipScope

Dual-transport CLI diagnostics and monitoring client for Klipper printers.

## Supported transports

- `moonraker`
- `klipper-tcp`

## Build

```powershell
$env:DOTNET_CLI_HOME="$PWD\.dotnet"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE="1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT="1"
dotnet build .\KlipScope.slnx
```
