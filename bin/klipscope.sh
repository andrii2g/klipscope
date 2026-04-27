#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CONFIGURATION="${KLIPSCOPE_CONFIGURATION:-Debug}"
TARGET_FRAMEWORK="${KLIPSCOPE_TARGET_FRAMEWORK:-net10.0}"
CLI_PROJECT="${REPO_ROOT}/src/KlipScope.Cli/KlipScope.Cli.csproj"
OUTPUT_DIR="${REPO_ROOT}/src/KlipScope.Cli/bin/${CONFIGURATION}/${TARGET_FRAMEWORK}"
APP_DLL="${OUTPUT_DIR}/klipscope.dll"

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-${REPO_ROOT}/.dotnet}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE="${DOTNET_SKIP_FIRST_TIME_EXPERIENCE:-1}"
export DOTNET_CLI_TELEMETRY_OPTOUT="${DOTNET_CLI_TELEMETRY_OPTOUT:-1}"
export MSBuildEnableWorkloadResolver="${MSBuildEnableWorkloadResolver:-false}"

if [[ ! -f "${APP_DLL}" ]]; then
  dotnet build "${CLI_PROJECT}" --configuration "${CONFIGURATION}" --no-restore -m:1 >/dev/null
fi

exec dotnet "${APP_DLL}" "$@"
