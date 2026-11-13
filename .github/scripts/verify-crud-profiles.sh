#!/usr/bin/env bash
set -euo pipefail

repository_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
work_directory="$(mktemp -d)"
trap 'rm -rf "$work_directory"' EXIT

dotnet build "$repository_root/src/Laranet.Cli/Laranet.Cli.csproj" \
  --configuration Release \
  --nologo

cli="$repository_root/src/Laranet.Cli/bin/Release/net10.0/Laranet.Cli.dll"

efcore_version="$(
  sed -n \
    's/.*<PackageVersion Include="Microsoft.EntityFrameworkCore" Version="\([^"]*\)".*/\1/p' \
    "$repository_root/Directory.Packages.props"
)"

if [[ -z "$efcore_version" ]]; then
  echo "Could not determine the Microsoft.EntityFrameworkCore version." >&2
  exit 1
fi

create_project() {
  local directory="$1"

  mkdir -p "$directory"

  cat > "$directory/App.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="__EFCORE_VERSION__" />
  </ItemGroup>
</Project>
EOF

  sed -i "s/__EFCORE_VERSION__/$efcore_version/" "$directory/App.csproj"

  cat > "$directory/Program.cs" <<'EOF'
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
EOF

  cat > "$directory/AppDbContext.cs" <<'EOF'
using Microsoft.EntityFrameworkCore;

namespace App;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
EOF
}

profiles=(
  simple
  standard
  clean
  modular
)

for profile in "${profiles[@]}"; do
  echo "::group::Building $profile CRUD profile"

  project="$work_directory/$profile"
  create_project "$project"

  dotnet "$cli" make:crud Product \
    --profile "$profile" \
    --fields "Name:string,Price:decimal,Description:string?,Stock:int" \
    --namespace App \
    --output "$project"

  dotnet build "$project/App.csproj" \
    --configuration Release \
    --nologo \
    --warnaserror

  echo "::endgroup::"
done

echo "::group::Building flat clean CRUD profile"

flat_project="$work_directory/flat"
create_project "$flat_project"

dotnet "$cli" make:crud Product \
  --profile clean \
  --context App.AppDbContext \
  --fields "Name:string,Price:decimal" \
  --namespace App \
  --flat \
  --output "$flat_project"

dotnet build "$flat_project/App.csproj" \
  --configuration Release \
  --nologo \
  --warnaserror

echo "::endgroup::"
