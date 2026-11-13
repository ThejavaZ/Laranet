# Development Standards

This page describes the conventions used in the Laranet repository and in the code
that Laranet generates. It complements [`AGENTS.md`](../../AGENTS.md); when the two
differ, the architectural constraints in `AGENTS.md` take precedence.

## Language and formatting

- Target framework: `net10.0`.
- C# with nullable reference types and implicit usings enabled.
- Warnings are errors. Do not suppress a warning without understanding why it occurs.
- Use file-scoped namespaces.
- Use four-space indentation and braces on their own lines.
- Prefer explicit and readable code over clever code.
- Use `var` when the type is obvious from the right-hand side; use an explicit type
  when it improves readability.
- Keep one primary type per file unless a small private helper belongs with it.
- Public APIs require XML documentation with `summary`, `param`, `returns`, and
  `exception` elements where applicable.

### Naming

| Element | Convention | Example |
| --- | --- | --- |
| Types, methods, properties, events | PascalCase | `PagedResult<T>`, `PaginateAsync` |
| Parameters and local variables | camelCase | `pageSize`, `cancellationToken` |
| Private fields | `_camelCase` | `_next`, `_commands` |
| Constants | PascalCase | `DefaultPageSize` |
| Interfaces | `I` prefix | `IGenerator` |
| Asynchronous methods | `Async` suffix | `ToListAsync`, `PaginateAsync` |

Names should describe intent, not implementation. Avoid abbreviations that are not
widely understood.

## Project structure and dependency direction

| Project | Responsibility |
| --- | --- |
| `Laranet` | Shared abstractions. Intentionally empty until two modules need the same type. |
| `Laranet.Data` | Provider-neutral query composition, pagination, search, and sorting. |
| `Laranet.EntityFrameworkCore` | EF Core-specific asynchronous execution. |
| `Laranet.AspNetCore` | ASP.NET Core integration. |
| `Laranet.Cli` | Project creation and code generation. |

Dependency rules:

- `Laranet` does not depend on ASP.NET Core or EF Core.
- `Laranet.Data` does not depend on EF Core or ASP.NET Core.
- Integrations may depend on `Laranet` and on the library they integrate with.
- Do not introduce circular project references.
- Do not add a project without a concrete responsibility and an architecture decision.

Namespaces must match folder structure. A type in `Laranet.Data.Extensions` belongs in
`src/Laranet.Data/Extensions`.

## Async and cancellation

- Asynchronous methods return `Task` or `ValueTask` and use the `Async` suffix.
- Accept a `CancellationToken` as the last parameter and pass it through to every
  asynchronous call that supports one.
- Never block with `.Result`, `.Wait()`, or `GetAwaiter().GetResult()`.
- Do not switch to in-memory execution to avoid writing asynchronous code.
- For `IQueryable`, preserve composition and defer materialization. Provider-specific
  asynchronous execution belongs in the corresponding integration module.

## Validation and errors

- Validate public arguments at the boundary with `ArgumentNullException.ThrowIfNull`,
  `ArgumentException`, or `ArgumentOutOfRangeException`.
- Use DataAnnotations for request validation in ASP.NET Core applications.
- Return `ProblemDetails` from HTTP endpoints instead of inventing an error envelope.
- Use exceptions for exceptional conditions. Do not introduce a transversal
  `Result<T>` or `Error` type without an accepted architecture decision.
- Do not create a custom exception hierarchy. Add a domain-specific exception only
  when the caller needs to distinguish it from a general failure.

## DTOs and API contracts

- Do not expose EF Core entities directly as API responses.
- Prefer immutable `record` types for response DTOs. Use classes when model binding
  requires settable properties.
- Keep mapping explicit unless repetition justifies a helper.
- Do not add a resource-serialization engine or a mandatory mapping layer.
- Validate request DTOs, not persistence entities.

## Interfaces, repositories, and abstractions

- Introduce an interface when it represents a real boundary, a substitution point, or
  a test seam. Do not create an interface for every class.
- `DbContext` is the default persistence abstraction. Do not add a repository or unit
  of work unless a concrete use case justifies it.
- Do not add CQRS, mediator, generic repository, or `Result<T>` patterns as defaults.
- A new public abstraction requires a demonstrated problem and, when it expands the
  public model, an ADR.
- Prefer composition over inheritance.

## Generated code

Generated code is part of the user's project and must be ordinary, editable C#.

- Use file-scoped namespaces, nullable reference types, and the conventions above.
- Generated projects must build without warnings.
- Do not add runtime metadata, attributes, or hidden configuration.
- The `simple` profile is the default. Do not generate repositories, service
  interfaces, DTO mapping layers, or unit-of-work types unless the selected profile
  requires them.
- Every profile and template must be covered by tests before it ships.
- Do not claim that a template works until it has been built in CI.

### Generator contract

The CLI generators are independent from the command-line layer:

| Type | Responsibility |
| --- | --- |
| `IGenerator` | Builds a `GenerationPlan` from a `GenerationContext`. |
| `GenerationContext` | Carries the normalized name, namespace, output directory, flat mode, and template catalog. |
| `GenerationOptions` | Carries `--force` and `--dry-run` behavior. |
| `GeneratedFile` | Represents one destination path and its content. |
| `GenerationPlan` | Groups the files produced by one generator. |
| `GeneratorRegistry` | Resolves a `GeneratorKind` to its generator. |
| `GenerationWriter` | Detects conflicts and writes or previews a plan. |

Generators only plan files; they do not write to disk. The writer reports every
conflict before writing anything, so a failed command never leaves a partial result.

### Template overrides

Templates live in `.laranet/templates`. The CLI walks up from the output directory and
uses the nearest directory it finds. Each generator has its own file:

```text
.laranet/
└── templates/
    ├── model.tpl
    ├── service.tpl
    ├── controller.tpl
    ├── middleware.tpl
    └── repository.tpl
```

Only the matching file replaces built-in content; folders, file names, and namespaces
remain convention-based. Templates support `{{Name}}` and `{{Namespace}}`; other
placeholders are left untouched. Empty templates are rejected. See the
[CLI template documentation](../../src/Laranet.Cli/README.md#customizing-templates)
for the user-facing behavior.

## Testing

- Every new behavior needs a test that asserts observable behavior.
- Use unit tests for contracts, validation, normalization, planning, and conflict
  detection.
- Use integration tests when EF Core translation, ASP.NET Core behavior, the file
  system, or a database provider is involved.
- Do not mock `IQueryable` to test translation.
- Keep tests deterministic and independent. Do not depend on execution order.
- Verify provider-dependent behavior against a real provider. See the
  [provider compatibility matrix](../compatibility/providers.md) and the
  [testing guidelines](testing-guidelines.md).

## Documentation

- Document what exists, not what is planned.
- Update `CHANGELOG.md` when behavior changes.
- Update installation, migration, and compatibility pages when a release changes them.
- Every relative link under `docs/` must resolve.
- Mark provider-dependent behavior explicitly.

## Related documents

- [`AGENTS.md`](../../AGENTS.md)
- [Contribution guide](contribution-guide.md)
- [Development workflow](development-workflow.md)
- [Testing guidelines](testing-guidelines.md)
- [Provider compatibility](../compatibility/providers.md)
- [`Laranet.Cli` README](../../src/Laranet.Cli/README.md)
