# API Reference

This section is the reference for the public API of the Laranet packages that ship
code. It complements the XML documentation in the source code and the package
READMEs.

| Page | Package | Status |
| --- | --- | --- |
| [Core](core.md) | `Laranet` | Published, intentionally empty |
| [Data](data.md) | `Laranet.Data` | Published |
| [Integrations](integrations.md) | `Laranet.EntityFrameworkCore`, `Laranet.AspNetCore` | Published |

## How to read this reference

- The namespace of each type is stated explicitly. Laranet does not use global
  namespaces.
- Behavior that depends on a database provider is documented as such.
- Exceptions are part of the contract. They are listed with the conditions that
  trigger them.
- Examples are ordinary C# and can be adapted to a Minimal API, MVC, or console
  application.

## Source of truth

The XML documentation generated from the source is the definitive reference for
parameter names, nullability, and exception conditions. This section explains the
intent and shows the common combinations.

## Not covered yet

Modules that are planned or under evaluation, such as validation and administrative
resources, are not part of the published API. The [roadmap](../roadmap/README.md)
tracks their status, and the
[ecosystem architecture](../architecture/ecosystem.md) defines when a module is
admitted.

## Related

- [Package overview](../packages/README.md)
- [Data access guide](../guides/data-access.md)
- [Pagination guide](../guides/pagination.md)
