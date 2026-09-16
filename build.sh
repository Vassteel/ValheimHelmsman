#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "$0")"
helmsman_dotnet="${DOTNET:-dotnet}"
if ! command -v "$helmsman_dotnet" >/dev/null 2>&1; then
  if [ -x /tmp/wildglow-dotnet/dotnet ]; then helmsman_dotnet=/tmp/wildglow-dotnet/dotnet;
  else printf '%s\n' 'Install .NET SDK 8 or set DOTNET to its executable.' >&2; exit 1; fi
fi
export DOTNET_CLI_HOME="$PWD/.build/dotnet-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
"$helmsman_dotnet" build src/Helmsman/Helmsman.csproj -c Release --nologo "$@"
"$helmsman_dotnet" run --project tests/Helmsman.Tests/Helmsman.Tests.csproj -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Interaction.Tests/Interaction.Tests.csproj -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Whistle.Tests/Whistle.Tests.csproj -c Release --no-launch-profile
mkdir -p dist/ValheimHelmsman
cp src/Helmsman/bin/Release/netstandard2.1/ValheimHelmsman.dll dist/ValheimHelmsman/
cp src/Helmsman.Core/bin/Release/netstandard2.1/Helmsman.Core.dll dist/ValheimHelmsman/
cp packaging/icon.png dist/ValheimHelmsman/icon.png
printf '%s\n' 'Built dist/ValheimHelmsman (game/framework assemblies are not included).'
