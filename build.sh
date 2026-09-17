#!/usr/bin/env bash
set -euo pipefail
cd -- "$(dirname -- "$0")"
helmsman_dotnet="${DOTNET:-dotnet}"
if ! command -v "$helmsman_dotnet" >/dev/null 2>&1; then
  if [ -x /home/deck/.cache/helmsman-dotnet/dotnet ]; then helmsman_dotnet=/home/deck/.cache/helmsman-dotnet/dotnet;
  elif [ -x /tmp/wildglow-dotnet/dotnet ]; then helmsman_dotnet=/tmp/wildglow-dotnet/dotnet;
  else printf '%s\n' 'Install .NET SDK 8 or set DOTNET to its executable.' >&2; exit 1; fi
fi
python3 tests/FinalFleet/run.py
python3 tools/pack_gullcall_model.py
export DOTNET_CLI_HOME="$PWD/.build/dotnet-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
DOTNET="$helmsman_dotnet" python3 tests/MaritimeMaterials/run.py
"$helmsman_dotnet" run --project tests/FinalFleet.Metadata.Tests -c Release --no-launch-profile
"$helmsman_dotnet" build src/Helmsman/Helmsman.csproj -c Release --nologo "$@"
"$helmsman_dotnet" run --project tests/Helmsman.Tests/Helmsman.Tests.csproj -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Interaction.Tests/Interaction.Tests.csproj -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Whistle.Tests/Whistle.Tests.csproj -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Carpenter.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Category.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Placement.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Launch.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Sail.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Workshop.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Scouting.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Clearing.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Navigation.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Network.Tests -c Release --no-launch-profile
"$helmsman_dotnet" run --project tests/Summon.Tests/Summon.Tests.csproj -c Release --no-launch-profile
helmsman_game="$("$helmsman_dotnet" msbuild src/Helmsman/Helmsman.csproj -getProperty:ValheimDir "$@")"
"$helmsman_dotnet" run --project tests/Whistle.ApiCheck -c Release "$@" -- "$PWD" "$helmsman_game"
mkdir -p dist/ValheimHelmsman
cp src/Helmsman/bin/Release/netstandard2.1/ValheimHelmsman.dll dist/ValheimHelmsman/
cp src/Helmsman.Core/bin/Release/netstandard2.1/Helmsman.Core.dll dist/ValheimHelmsman/
cp packaging/icon.png dist/ValheimHelmsman/icon.png
printf '%s\n' 'Built dist/ValheimHelmsman (game/framework assemblies are not included).'
