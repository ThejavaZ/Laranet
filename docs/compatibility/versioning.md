# Versioning Policy

## 1. Introduction

Laranet follows Semantic Versioning (SemVer) to provide predictable package releases and communicate the impact of changes.

The project maintains its own versioning lifecycle independently of .NET release numbers.

This policy applies to Laranet's public packages and their documented public APIs.

## 2. Version Format

Laranet uses the following version format:

`MAJOR.MINOR.PATCH`

Each component has a specific meaning.

### MAJOR

Introduces incompatible public API changes.

Examples:

- Removing public methods or types.
- Changing public method signatures incompatibly.
- Changing established behavior in a way that breaks existing applications.
- Removing previously supported functionality.

### MINOR

Introduces new functionality in a backward-compatible manner.

Examples:

- Adding new public methods.
- Introducing new optional configuration.
- Adding new extension methods.
- Introducing additional compatible integrations.

### PATCH

Introduces backward-compatible bug fixes.

Examples:

- Fixing incorrect behavior.
- Correcting internal implementation defects.
- Improving reliability without changing public contracts.

## 3. Pre-release Development

Laranet may publish pre-release versions using standard SemVer suffixes.

Examples:

- `0.1.0-alpha.1`
- `0.1.0-beta.1`
- `0.1.0-rc.1`

Pre-release versions are intended for testing, feedback, and early adoption.

They should not be considered equivalent to stable production releases.

During the initial `0.x` development period, APIs may still evolve.

However, Laranet will not use its pre-1.0 status as justification for silently introducing breaking changes.

Breaking changes must be explicitly documented and reflected in the version number.

## 4. Initial Release

The initial public release is planned as:

`0.1.0`

This release will represent the first published implementation of Laranet's initial functionality.

Its scope will be defined by the features included in the release and documented in the changelog.

The first stable release is planned as `1.0.0` in [Version 5](../roadmap/v5.md). It is a
compatibility promise over the frozen runtime surface, not a statement that every
roadmap capability is complete.

## 5. Package Version Consistency

Packages belonging to the same Laranet release should use consistent version numbers.

For example:

| Package                     | Version |
| --------------------------- | ------- |
| Laranet                     | 0.1.0   |
| Laranet.Data                | 0.1.0   |
| Laranet.EntityFrameworkCore | 0.1.0   |
| Laranet.AspNetCore          | 0.1.0   |
| Laranet.Cli                 | 0.1.0   |

Packages that are not part of a particular release do not need to be published.

Independent package versioning may be considered in the future if it provides a meaningful benefit and can be managed without creating unnecessary compatibility complexity. The exception process, including the triggers that justify independent runtime and tooling lanes, is defined in [ADR-0008](../architecture/decisions/0008-runtime-and-tooling-versioning.md).

## 6. .NET Version Independence

Laranet versions do not correspond directly to .NET versions.

For example:

- Laranet `0.1.0` may target .NET 10.
- Laranet `0.2.0` may continue targeting .NET 10.
- A future Laranet release may support multiple .NET target frameworks.

A new .NET release does not automatically require a new Laranet major or minor version.

See the [Support Policy](support-policy.md) for supported framework versions.

## 7. Breaking Changes

Breaking changes require special consideration.

Before introducing one, contributors should evaluate:

- Whether the change is strictly necessary.
- Whether backward compatibility can be preserved.
- Whether an alternative API can be introduced.
- Whether the affected API should first be deprecated.
- The migration effort required for existing applications.

Breaking changes must:

- Be clearly identified in the changelog.
- Include an explanation of their impact.
- Provide migration guidance when appropriate.
- Follow the project's major versioning policy.

Whenever practical, public APIs should be deprecated before removal.

## 8. Deprecation Policy

Deprecation communicates that an API is no longer recommended and may be removed in a future major release.

When deprecating an API:

- Use the appropriate .NET deprecation attributes, such as `[Obsolete]`.
- Explain why the API is deprecated.
- Identify the recommended alternative.
- Document migration guidance.
- Avoid removing the API before the appropriate breaking release.

Deprecation warnings should provide actionable information.

## 9. Changelog Requirements

Every release must be documented in `CHANGELOG.md`.

The changelog should communicate:

- Added functionality.
- Changed behavior.
- Deprecated APIs.
- Removed functionality.
- Bug fixes.
- Security-related changes.

Breaking changes must be explicitly highlighted.

The changelog should describe relevant user-facing changes rather than every internal implementation detail.

## 10. Release Process

A release should follow a consistent process:

1. Review the changes included in the release.
2. Verify compatibility with supported target frameworks.
3. Execute the relevant automated tests.
4. Review public API changes.
5. Update the changelog.
6. Update package versions.
7. Build and validate NuGet packages.
8. Publish the release and its documentation.

Release automation may be introduced as the project matures.

## 11. Policy Evolution

This policy may evolve as Laranet gains users, packages, and a more established public API.

Changes to versioning rules must be documented and communicated.

Significant changes to this policy should be recorded through an Architecture Decision Record.

## 12. Summary

Laranet uses Semantic Versioning to communicate changes, preserve compatibility, and establish predictable release expectations.

**Version numbers are contracts with developers who depend on Laranet.**
