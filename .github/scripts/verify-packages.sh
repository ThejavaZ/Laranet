#!/usr/bin/env bash
set -euo pipefail

package_dir="${1:-artifacts/packages}"
expected_version="${2:-}"

if [ ! -d "$package_dir" ]; then
  echo "::error::Package directory '$package_dir' does not exist."
  exit 1
fi

mapfile -t packages < <(
  find "$package_dir" -maxdepth 1 \
    \( -name 'Laranet.[0-9]*.nupkg' \
       -o -name 'Laranet.Data.[0-9]*.nupkg' \
       -o -name 'Laranet.EntityFrameworkCore.[0-9]*.nupkg' \
       -o -name 'Laranet.AspNetCore.[0-9]*.nupkg' \
       -o -name 'Laranet.Cli.[0-9]*.nupkg' \) \
    | sort
)

if [ "${#packages[@]}" -ne 5 ]; then
  echo "::error::Expected exactly five production packages, found ${#packages[@]}."
  exit 1
fi

core_package="$(find "$package_dir" -maxdepth 1 -name 'Laranet.[0-9]*.nupkg')"
data_package="$(find "$package_dir" -maxdepth 1 -name 'Laranet.Data.[0-9]*.nupkg')"
ef_package="$(find "$package_dir" -maxdepth 1 -name 'Laranet.EntityFrameworkCore.[0-9]*.nupkg')"
aspnet_package="$(find "$package_dir" -maxdepth 1 -name 'Laranet.AspNetCore.[0-9]*.nupkg')"
cli_package="$(find "$package_dir" -maxdepth 1 -name 'Laranet.Cli.[0-9]*.nupkg')"

core_nuspec="$(unzip -p "$core_package" '*.nuspec')"
data_nuspec="$(unzip -p "$data_package" '*.nuspec')"
ef_nuspec="$(unzip -p "$ef_package" '*.nuspec')"
aspnet_nuspec="$(unzip -p "$aspnet_package" '*.nuspec')"
cli_nuspec="$(unzip -p "$cli_package" '*.nuspec')"

for package in "$core_package" "$data_package"; do
  nuspec="$(unzip -p "$package" '*.nuspec')"

  if grep -Eq 'Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore' <<< "$nuspec"; then
    echo "::error::Forbidden dependency found in $package."
    exit 1
  fi
done

if grep -Eq 'Microsoft\.AspNetCore' <<< "$ef_nuspec"; then
  echo "::error::Forbidden ASP.NET Core dependency found in $ef_package."
  exit 1
fi

if grep -Eq 'Microsoft\.EntityFrameworkCore' <<< "$aspnet_nuspec"; then
  echo "::error::Forbidden EF Core dependency found in $aspnet_package."
  exit 1
fi

if ! grep -q '<frameworkReference name="Microsoft.AspNetCore.App" />' <<< "$aspnet_nuspec"; then
  echo "::error::$aspnet_package must reference the Microsoft.AspNetCore.App framework."
  exit 1
fi

if grep -Eq 'Microsoft\.EntityFrameworkCore|Microsoft\.AspNetCore' <<< "$cli_nuspec"; then
  echo "::error::Forbidden dependency found in $cli_package."
  exit 1
fi

if ! grep -q '<packageType name="DotnetTool" />' <<< "$cli_nuspec"; then
  echo "::error::$cli_package must be a .NET tool package."
  exit 1
fi

cli_tool_settings="$(unzip -p "$cli_package" 'tools/*/any/DotnetToolSettings.xml')"

if ! grep -q '<Command Name="laranet"' <<< "$cli_tool_settings"; then
  echo "::error::$cli_package must expose the laranet command."
  exit 1
fi

core_version="$(grep -oP '(?<=<version>)[^<]+' <<< "$core_nuspec" | head -n 1)"
data_version="$(grep -oP '(?<=<version>)[^<]+' <<< "$data_nuspec" | head -n 1)"
ef_version="$(grep -oP '(?<=<version>)[^<]+' <<< "$ef_nuspec" | head -n 1)"
aspnet_version="$(grep -oP '(?<=<version>)[^<]+' <<< "$aspnet_nuspec" | head -n 1)"
cli_version="$(grep -oP '(?<=<version>)[^<]+' <<< "$cli_nuspec" | head -n 1)"

if [ -n "$expected_version" ] && [ "$core_version" != "$expected_version" ]; then
  echo "::error::Expected package version '$expected_version', found '$core_version'."
  exit 1
fi

for package in "$ef_package" "$aspnet_package"; do
  nuspec="$(unzip -p "$package" '*.nuspec')"

  if ! grep -q "<dependency id=\"Laranet.Data\" version=\"$data_version\"" <<< "$nuspec"; then
    echo "::error::$package must depend on Laranet.Data $data_version."
    exit 1
  fi
done

if ! grep -q '<dependency id="Microsoft.EntityFrameworkCore" version="10.0.14"' <<< "$ef_nuspec"; then
  echo "::error::$ef_package must depend on Microsoft.EntityFrameworkCore 10.0.14."
  exit 1
fi

if [ "$core_version" != "$data_version" ] \
  || [ "$core_version" != "$ef_version" ] \
  || [ "$core_version" != "$aspnet_version" ] \
  || [ "$core_version" != "$cli_version" ]; then
  echo "::error::Package versions are not synchronized: Laranet=$core_version, Laranet.Data=$data_version, Laranet.EntityFrameworkCore=$ef_version, Laranet.AspNetCore=$aspnet_version, Laranet.Cli=$cli_version."
  exit 1
fi

for nuspec in "$core_nuspec" "$data_nuspec" "$ef_nuspec" "$aspnet_nuspec" "$cli_nuspec"; do
  grep -q '<readme>README.md</readme>' <<< "$nuspec"
  grep -q '<license type="expression">MIT</license>' <<< "$nuspec"
  grep -q '<authors>TheJavaZ</authors>' <<< "$nuspec"
  grep -q '<repository ' <<< "$nuspec"
  grep -q '<projectUrl>https://github.com/TheJavaZ/Laranet</projectUrl>' <<< "$nuspec"
  grep -q '<releaseNotes>https://github.com/TheJavaZ/Laranet/blob/main/CHANGELOG.md</releaseNotes>' <<< "$nuspec"
done

echo "Production packages passed validation (version $core_version)."
