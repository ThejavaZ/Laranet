# Support Policy

## 1. Introduction

Laranet aims to provide reliable and predictable compatibility with the .NET ecosystem.

This document defines the project's supported .NET versions, compatibility expectations, and general support principles.

The support policy will evolve as Laranet gains functionality and additional integrations.

## 2. Supported .NET Versions

Laranet initially targets:

| .NET Version   | Support Status    |
| -------------- | ----------------- |
| .NET 10        | Supported         |
| Other versions | Not yet supported |

.NET 10 is the initial development and release target.

Additional target frameworks may be introduced when they can be properly implemented, tested, and maintained.

A new .NET release does not automatically become a supported Laranet target.

## 3. SDK and Runtime Compatibility

The .NET SDK and .NET Runtime serve different purposes.

- The **SDK** is used to develop, build, test, and package Laranet.
- The **Runtime** is used to execute applications built with Laranet.

The repository may pin a specific SDK version through `global.json` to ensure consistent development environments.

However, SDK pinning does not necessarily mean that applications must use that exact SDK version in production.

Runtime requirements are determined by the target frameworks and dependencies of each distributed package.

## 4. Integration Compatibility

Optional integrations must document their supported dependency versions.

For example:

- Laranet.EntityFrameworkCore must document compatible Entity Framework Core versions.
- Laranet.AspNetCore must document compatible ASP.NET Core and .NET versions.

Integrations should use compatible dependency versions and avoid introducing unnecessary framework dependencies.

Compatibility must be validated through automated tests whenever practical.

## 5. Compatibility Guarantees

Laranet aims to maintain the following guarantees:

- Public APIs should behave consistently within compatible releases.
- Supported target frameworks must pass the project's validation process.
- Breaking changes must be documented.
- Deprecated APIs should provide migration guidance whenever appropriate.
- Optional integrations must not introduce unexpected dependencies into unrelated packages.

Compatibility guarantees apply to officially supported package versions and documented target frameworks.

### Published versions

The current release line is `0.1.x`:

| Version | Packages | Status |
| --- | --- | --- |
| `0.1.0` | All five production packages | Published |
| `0.1.1` | All five production packages | Prepared |

Within a version line, patch releases are backward compatible and keep package
versions synchronized.

## 6. .NET Release Evaluation

When a new .NET version becomes available, Laranet will evaluate whether to support it.

The evaluation should consider:

- Framework compatibility.
- Dependency availability.
- Integration compatibility.
- Required code changes.
- Build and test results.
- Maintenance effort.

Multi-targeting may be introduced when there is a concrete need and the project can maintain reliable testing across all supported targets.

Supporting a new .NET version should not require unnecessary rewrites of existing functionality.

## 7. End of Support

A .NET target framework may be removed from the supported matrix when:

- It reaches the end of its official support lifecycle.
- Its dependencies are no longer maintained.
- Maintaining compatibility introduces unreasonable complexity.
- Security or compatibility requirements make continued support impractical.

Such changes should be communicated through release notes and migration documentation.

## 8. Support Channels

Users should report compatibility problems through GitHub Issues.

Reports should include:

- Laranet package version.
- .NET target framework.
- SDK version when relevant.
- Integration package versions.
- Operating system when relevant.
- Error messages or reproduction steps.

Feature requests and compatibility questions should follow the repository's contribution guidelines.

## 9. Policy Evolution

This policy may change as Laranet evolves.

Changes affecting supported frameworks or compatibility guarantees should be documented in the changelog and, when architecturally significant, through an Architecture Decision Record.

## 10. Summary

Laranet prioritizes reliable compatibility over supporting every available .NET version immediately.

**A framework version is supported when Laranet can build, test, and maintain it reliably.**
