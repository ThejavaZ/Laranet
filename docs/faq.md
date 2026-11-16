# FAQ

Answers to common questions about installing, using, and extending Laranet.

## General

### Is Laranet a framework?

No. Laranet is a modular toolkit: it does not prescribe a folder structure, a
host, or a base class. Its tooling can feel framework-like while the runtime
stays optional. See [Project Philosophy](concepts/philosophy.md) and
[ADR-0006](architecture/decisions/0006-toolkit-with-framework-tooling.md).

### Does Laranet replace ASP.NET Core or Entity Framework Core?

No. Laranet builds on the native building blocks you already use: dependency
injection, LINQ, EF Core, and ASP.NET Core Minimal APIs and MVC. Framework
behavior lives in optional packages — see
[Optional integrations](concepts/philosophy.md#optional-integrations).

### Is Laranet stable?

Laranet is in early development on the `0.4.x` line, and APIs may still evolve.
The [support policy](compatibility/support-policy.md) documents the current
guarantees and the [roadmap](roadmap/README.md) tracks planned work.

### Are there runnable samples?

Yes:

- [Console sample](../samples/Laranet.Samples.Console) — `Laranet.Data` over LINQ
  to Objects.
- [Web API sample](../samples/Laranet.Samples.WebApi) — the runtime packages with
  EF Core and SQLite.
- [Admin sample](../samples/Laranet.Samples.Admin) — the experimental admin
  panel with its complete authentication wiring.

## Installation and the CLI

### Which package do I need?

See [Which package do I need?](packages/README.md#which-package-do-i-need) for the
scenario table. In short: `Laranet.Data` for query utilities over any
`IQueryable<T>`, `Laranet.EntityFrameworkCore` for async pagination and EF Core
conventions, `Laranet.AspNetCore` for pagination headers, and `Laranet.Cli` for
project scaffolding.

### `laranet: command not found`

The CLI is a global .NET tool and is not installed with the runtime packages:

```bash
dotnet tool install --global Laranet.Cli
```

Verify with `laranet --help`, and see the [CLI guide](guides/cli.md) for the
full command list.

### Can I build Laranet from source instead of using NuGet?

Yes. See [Build from source](getting-started/installation.md#build-from-source)
in the installation guide.

## Data and queries

### Does Laranet.Data work with providers other than EF Core?

Yes. `Laranet.Data` is provider-neutral: it composes standard LINQ expression
trees over any `IQueryable<T>` and does not depend on a database provider. The
[provider compatibility matrix](compatibility/providers.md) lists what has been
verified.

### Does Laranet change how my queries execute?

No. The query utilities preserve provider composition: nothing is materialized
early and nothing switches to in-memory execution without an explicit reason.
See [Data Access](guides/data-access.md).

## Validation

### Does Laranet ship its own validation engine?

No. MVC controllers keep using ASP.NET Core validation (`[ApiController]` and
DataAnnotations). For Minimal APIs, `Laranet.AspNetCore.Validation` adds an
optional FluentValidation endpoint filter. See [Validation](guides/validation.md).

## Admin prototype

### Why is my admin panel empty?

Resources must be registered in the service container, not in a local variable.
The panel pages resolve `IAdminResourceRegistry` from dependency injection:

```csharp
builder.Services.AddSingleton<IAdminResourceRegistry>(services =>
{
    var registry = new AdminResourceRegistry();
    registry.Add(ProductAdminResource.Definition);
    return registry;
});
```

See [Resource registration](guides/admin.md#resource-registration).

### Which services does the admin login need?

The Razor Pages login resolves `IAdminUserStore`, `IAdminPasswordHasher`, and
`IAdminLoginRateLimiter`. They are registered by
`AddLaranetAdminAuthentication(...)`, and users come from configuration via
`options.AddUser(...)`. The panel does not authenticate with ASP.NET Core
Identity's `UserManager`. The complete wiring is in the
[Laranet.Samples.Admin](../samples/Laranet.Samples.Admin) sample.

### Where can I find the admin routes?

The panel exposes `/admin` for the dashboard, `/admin/{resource}` for the
resource list, and conventional create, details, and edit routes, plus the
login, password, and MFA pages. See
[The panel](guides/admin.md#the-panel) for the full table.

## Versioning

### Which version should I install?

The current release line is `0.4.x`. The
[support policy](compatibility/support-policy.md) lists every published version
and the [versioning policy](compatibility/versioning.md) explains how releases
are numbered.

### What does "independent versioning" mean?

Packages have separate version histories and release cadences, so a change in
one package does not by itself require releasing the others. Within a release
line, patch releases keep the published packages synchronized. See
[Versioning](compatibility/versioning.md).

## Contributing

### How can I contribute?

Start with the [Contribution Guide](development/contribution-guide.md) and the
[Development Standards](development/standards.md). All participation is governed
by our [Code of Conduct](../CODE_OF_CONDUCT.md).

### Where can I report a bug or ask a question?

Open an issue in the
[issue tracker](https://github.com/TheJavaZ/Laranet/issues) using the available
templates for bug reports and questions.
