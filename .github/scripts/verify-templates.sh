#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
work_directory="$(mktemp -d)"
trap 'rm -rf "$work_directory"' EXIT

dotnet build "$repository_root/src/Laranet.Cli/Laranet.Cli.csproj" \
  --configuration Release \
  --nologo

cli="$repository_root/src/Laranet.Cli/bin/Release/net10.0/Laranet.Cli.dll"

templates=(
  minimal-api
  mvc
  layered
  hexagonal
  modular
  console
)

for template in "${templates[@]}"; do
  name="verify-${template}"

  echo "::group::Building $template template"

  dotnet "$cli" new "$name" \
    --template "$template" \
    --layout repository \
    --output "$work_directory"

  dotnet build "$work_directory/$name/$name.slnx" \
    --configuration Release \
    --nologo

  echo "::endgroup::"
done
