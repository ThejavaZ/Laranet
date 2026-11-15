# Release Process

This document describes how Laranet packages are prepared and published.

## Versioning

Every package in a release uses the same [`MAJOR.MINOR.PATCH`](../../docs/compatibility/versioning.md)
version. The version is declared in each production `.csproj` under
`<Version>`.

Packages in a release:

- `Laranet`
- `Laranet.Data`
- `Laranet.EntityFrameworkCore`
- `Laranet.AspNetCore`
- `Laranet.AspNetCore.Validation`
- `Laranet.Admin`
- `Laranet.Admin.AspNetCore`
- `Laranet.Admin.EntityFrameworkCore`
- `Laranet.Http`
- `Laranet.Cli`

The release workflow fails when the tag does not match every declared version.

## Public API review

Before a release, review the public surface:

- Public types exist only in the package that needs them. `Laranet` remains empty
  until it has a justified shared abstraction.
- Extension methods live in their documented namespaces:
  `Laranet.Data.Extensions`, `Laranet.EntityFrameworkCore.Extensions`, and
  `Laranet.AspNetCore.Extensions`. `AddLaranetHttp` follows the native
  `Microsoft.Extensions.DependencyInjection` convention, and
  `Laranet.AspNetCore.Validation` uses its own namespace.
- Every public member has XML documentation. The build treats missing documentation
  as an error.
- `PagedResult<T>` remains the only pagination result type.
- No EF Core or ASP.NET Core dependency leaks into `Laranet.Data`.
- The CLI is distributed as a .NET tool and does not expose a runtime API.

## Package metadata

CI validates the generated `.nuspec` files for every release package:

- Package ID, synchronized version, description, and tags.
- MIT license expression, author, and repository URL.
- `README.md`, project URL, and release-notes URL.
- The shared package icon (`laranet_icon_out_wallpaper.png`).
- `Laranet.Data` has no framework dependencies.
- `Laranet.EntityFrameworkCore` depends on `Laranet.Data` and EF Core only.
- `Laranet.AspNetCore` references the ASP.NET Core shared framework and
  `Laranet.Data`.
- `Laranet.AspNetCore.Validation` references the ASP.NET Core shared framework and
  `FluentValidation`, and does not depend on other Laranet packages.
- `Laranet.Http` references `Microsoft.Extensions.Http` and does not depend on
  ASP.NET Core.
- `Laranet.Cli` is a `DotnetTool` package with the `laranet` command.

`Laranet.Admin` is an experimental prototype and is intentionally excluded from the
release train, CI package count, and NuGet publication until its real-application
validation and API review are complete.

## Preparing a release

1. Confirm the scope in the [roadmap](../roadmap/README.md).
2. Update `CHANGELOG.md` with the release notes and date.
3. Update `<Version>` in every production `.csproj`.
4. Run the full validation locally:

   ```bash
   dotnet build Laranet.slnx --configuration Release
   dotnet test Laranet.slnx --configuration Release --no-build
   ```

5. Pack and inspect the packages locally:

   ```bash
   mkdir -p artifacts/packages
   dotnet pack src/Laranet/Laranet.csproj --configuration Release --no-build --output artifacts/packages
   dotnet pack src/Laranet.Data/Laranet.Data.csproj --configuration Release --no-build --output artifacts/packages
   dotnet pack src/Laranet.EntityFrameworkCore/Laranet.EntityFrameworkCore.csproj --configuration Release --no-build --output artifacts/packages
   dotnet pack src/Laranet.AspNetCore/Laranet.AspNetCore.csproj --configuration Release --no-build --output artifacts/packages
   dotnet pack src/Laranet.AspNetCore.Validation/Laranet.AspNetCore.Validation.csproj --configuration Release --no-build --output artifacts/packages
   dotnet pack src/Laranet.Http/Laranet.Http.csproj --configuration Release --no-build --output artifacts/packages
   dotnet pack src/Laranet.Cli/Laranet.Cli.csproj --configuration Release --no-build --output artifacts/packages
   ```

6. Commit the version and changelog changes.
7. Tag the commit and push the tag:

   ```bash
   git tag v0.4.2
   git push origin v0.4.2
   ```

## Automated publication

`.github/workflows/release.yml` runs on `v*` tags and can also be started manually
with a version input.

The workflow:

1. Resolves the version from the tag (`v0.4.2` becomes `0.4.2`).
2. Verifies that every production project declares the same version.
3. Restores, builds, and runs the full test suite.
4. Packs the ten packages.
5. Verifies that exactly ten `.nupkg` files were produced with the expected
   version and valid metadata.
6. Requests a short-lived NuGet API key through GitHub OIDC.
7. Pushes the packages to NuGet.org with `--skip-duplicate`.

CI and the release workflow share `.github/scripts/verify-packages.sh`, so a package
that fails metadata or dependency validation in CI cannot be published.

### Trusted Publishing

The release workflow uses [NuGet Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing)
instead of a long-lived API key. The workflow requests an OIDC token, exchanges it
for a temporary key valid for one hour, and publishes the packages.

The NuGet.org policy must match the repository:

| Field | Value |
| --- | --- |
| Policy name | `Laranet-GitHub-Actions` |
| Package owner | `ThejavaZ` |
| Repository owner | `ThejavaZ` |
| Repository | `Laranet` |
| Workflow file | `release.yml` |
| Environment | Empty |
| Scopes | Push new packages and package versions |
| Glob patterns | `Laranet`, `Laranet.Data`, `Laranet.EntityFrameworkCore`, `Laranet.AspNetCore`, `Laranet.AspNetCore.Validation`, `Laranet.Cli`, `Laranet.Http` |

The `user` input of `NuGet/login@v1` must be the NuGet.org account username. The
workflow currently uses `ThejavaZ`.

No `NUGET_API_KEY` secret is required. Private repositories may show the policy as
temporarily active for seven days until the first successful publish provides the
repository and owner IDs.

No GitHub Packages feed is used. Publication targets NuGet.org only.

## Manual publication

If the workflow is unavailable, publish the locally packed files with a personal
NuGet.org API key. The temporary key from the workflow is not available outside
GitHub Actions.

```bash
dotnet nuget push artifacts/packages/Laranet.0.4.2.nupkg \
  --api-key "$NUGET_API_KEY" \
  --source https://api.nuget.org/v3/index.json

# Repeat for the other nine packages.
```

`--skip-duplicate` makes retries safe when a package version already exists.

## After publication

1. Verify the package pages on NuGet.org.
2. Install the packages in a clean directory and run a build.
3. Confirm the CLI installs and reports the expected version:

   ```bash
   dotnet tool install --global Laranet.Cli --version 0.4.2
   laranet --version
   ```

4. Update the changelog with the release date and link to the tag.
5. Close the corresponding roadmap items.

## Failed or incorrect releases

- Do not delete a published package version that users may already depend on.
- Unlist the version on NuGet.org when it should not be installed.
- Fix the issue and publish a new patch version.
- Document the problem and the replacement version in the changelog.

## Related

- [Versioning policy](../compatibility/versioning.md)
- [Support policy](../compatibility/support-policy.md)
- [Testing](testing.md)
