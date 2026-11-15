# Migration Guide

## Scope

This guide explains how to upgrade between Laranet releases and how to move an
application from a source checkout to the distributed packages.

Laranet follows [Semantic Versioning](../compatibility/versioning.md). During the
initial `0.x` period, public APIs may still evolve, but breaking changes are
documented in the [changelog](../../CHANGELOG.md) and in this guide.

## Current release status

The current release candidate is `0.4.1`. It adds the optional FluentValidation
Minimal API integration, CLI CQRS/database/configuration generators, and
`laranet doctor`. The new APIs are additive; the experimental Admin prototype is
not part of this release.

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

## Published migrations

### 0.2.1

- **Affected packages:** all six production packages. `Laranet.Http` is new.
- **What changed:** added `Laranet.Http`, optional EF Core auditing and soft-delete
  conventions, project templates, and `make:crud`. Provider verification now covers
  SQL Server, PostgreSQL, MySQL, and MariaDB.
- **Impact:** none for existing packages. The new APIs are additive and optional.
- **Migration:** update every Laranet package to `0.2.1` and add `Laranet.Http` only
  when the application calls external HTTP APIs.

### 0.3.0

- **Affected packages:** all seven production packages. `Laranet.AspNetCore.Validation`
  is new.
- **What changed:** added the opt-in FluentValidation Minimal API endpoint filter,
  CLI generators for CQRS, DbContext, and configuration, and `laranet doctor`.
- **Impact:** existing APIs remain additive and native ASP.NET Core/DataAnnotations
  validation remains the default.
- **Migration:** update every production package and the CLI tool to `0.3.0`.
  Install `Laranet.AspNetCore.Validation` only when using FluentValidation.

### 0.3.1

- **Affected packages:** `Laranet.Cli` and the synchronized production package line.
- **What changed:** `make:crud` introspects existing entities, supports
  `--from-model` and `--scaffold`, detects inherited properties and key types, and
  handles `CompanyId`, data annotations, and soft-delete conventions.
- **Impact:** existing scaffold behavior remains available through `--scaffold`;
  `--force` still only controls overwriting generated files.
- **Migration:** update every production package and the CLI tool to `0.3.1`.

### 0.1.1

- **Affected packages:** all five production packages.
- **What changed:** added the multi-column `Search` overload, the shared package icon,
  and documentation updates.
- **Impact:** none. Existing calls keep their behavior and no public API was removed or
  renamed.
- **Migration:** update every Laranet package to `0.1.1`.

## From source to packages

If the application currently references the projects in `src/`, replace the
`ProjectReference` entries with `PackageReference` entries after installing the
packages:

```xml
<ItemGroup>
  <PackageReference Include="Laranet.Data" Version="0.3.1" />
  <PackageReference Include="Laranet.EntityFrameworkCore" Version="0.3.1" />
  <PackageReference Include="Laranet.AspNetCore" Version="0.3.1" />
  <PackageReference Include="Laranet.AspNetCore.Validation" Version="0.3.1" />
  <PackageReference Include="Laranet.Http" Version="0.3.1" />
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
