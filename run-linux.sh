#!/usr/bin/env bash
set -euo pipefail
export PATH="$HOME/.dotnet:$PATH"
exec dotnet run --project "$(dirname "$0")/src/OpenDimmer.Linux/OpenDimmer.Linux.csproj" -- "$@"
