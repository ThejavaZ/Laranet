# Core API (`Laranet`)

## Status

The `Laranet` package is published and **intentionally empty**. It contains no public
types.

This is a deliberate architectural decision, not an unfinished package. See
[ADR-0005](../architecture/decisions/0005-intentionally-empty-core.md).

## Why the package exists

Keeping an empty core package available allows the ecosystem to reserve the
`Laranet` package ID and to provide a stable place for genuinely shared
abstractions later. The project installs it only when it needs something that belongs
to more than one module.

The project does not create a core type just to make the package look useful. A type
moves into `Laranet` only when:

- At least two independent modules need the same abstraction.
- The abstraction cannot live naturally in either module.
- Adding it does not introduce a dependency on EF Core, ASP.NET Core, or a database
  provider.
- The public contract is documented and justified by a real use case.

## What it must not contain

The core package must remain independent of:

- Entity Framework Core.
- ASP.NET Core.
- Specific database providers.
- The CLI.
- Infrastructure implementations from optional modules.

## Typical usage

There is nothing to call. Add the package only when a future shared abstraction is
introduced and documented here:

```bash
dotnet add package Laranet
```

Applications normally depend on `Laranet.Data`,
`Laranet.EntityFrameworkCore`, or `Laranet.AspNetCore` directly. Those packages do not
require `Laranet`.

## Related

- [ADR-0005: Keep the Core Package Intentionally Empty](../architecture/decisions/0005-intentionally-empty-core.md)
- [ADR-0001: Keep the Core Independent](../architecture/decisions/0001-independent-core.md)
- [Ecosystem architecture](../architecture/ecosystem.md)
- [Package overview](../packages/README.md)
