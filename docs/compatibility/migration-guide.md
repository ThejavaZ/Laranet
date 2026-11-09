# Migration Guide

## Scope

This guide explains how to upgrade between Laranet releases and how to move an
application from a source checkout to the distributed packages.

Laranet follows [Semantic Versioning](../compatibility/versioning.md). During the
initial `0.x` period, public APIs may still evolve, but breaking changes are
documented in the [changelog](../../CHANGELOG.md) and in this guide.

## Current release status

The repository prepares the `0.1.0` packages described in the
[installation guide](../getting-started/installation.md). No stable release has been
published yet, so there are no historical version-to-version migrations.

This page is the place where each published migration will be recorded.

## Upgrading between versions

1. Read the [changelog](../../CHANGELOG.md) for the target version.
2. Update every Laranet package to the same version.
3. Restore and build the application.
4. Run the application's test suite.
5. Review the sections below for breaking changes.

Update all packages together:

```bash
dotnet add package Laranet.Data --version <version>
dotnet add package Laranet.EntityFrameworkCore --version <version>
dotnet add package Laranet.AspNetCore --version <version>
```

For the CLI:

```bash
dotnet tool update --global Laranet.Cli --version <version>
```

Package versions belonging to the same release are kept synchronized. Mixing
versions from different releases is not supported.

## From source to packages

If the application currently references the projects in `src/`, replace the
`ProjectReference` entries with `PackageReference` entries after installing the
packages:

```xml
<ItemGroup>
  <PackageReference Include="Laranet.Data" Version="0.1.0" />
  <PackageReference Include="Laranet.EntityFrameworkCore" Version="0.1.0" />
  <PackageReference Include="Laranet.AspNetCore" Version="0.1.0" />
</ItemGroup>
```

Then remove the local package source if it is no longer needed:

```bash
dotnet nuget remove source laranet-local
```

Generated projects from `laranet new --with-*` already use package references.

## Breaking changes

A breaking change is any change that requires an application to modify code or
configuration when upgrading. Examples:

- Removing or renaming a public type or member.
- Changing a method signature incompatibly.
- Changing established behavior in a way that affects results.
- Raising the minimum supported .NET or integration version.

Breaking changes are listed under **Changed**, **Removed**, or a dedicated
**Breaking** section in the changelog. When migration steps are required, they are
documented here with the affected version.

### Migration record template

```markdown
## <version>

### <change>

- **Affected packages:** ...
- **What changed:** ...
- **Impact:** ...
- **Migration:** ...
```

## Upgrading .NET or integration versions

Laranet versions are independent of .NET versions. A new .NET release does not
automatically require a Laranet upgrade, and a Laranet upgrade does not automatically
require a new .NET version.

Before upgrading the target framework or an integration such as EF Core or ASP.NET
Core, check the [support policy](support-policy.md) and the
[provider compatibility](providers.md) matrix.

## Deprecations

Deprecated APIs are marked with `[Obsolete]`, explain the recommended alternative,
and remain available until the appropriate breaking release. Deprecation warnings
are part of the migration path; do not suppress them without a plan to migrate.

## Getting help

If a migration step is unclear or a documented path does not work, open an issue using
the [bug report template](../../.github/ISSUE_TEMPLATE/bug_report.yml) and include:

- The source and target Laranet versions.
- The .NET target framework.
- The integration package versions.
- The relevant error output.
