# ADR-0004: Generate Ordinary, Editable C#

- **Status:** Accepted
- **Date:** 2026-11-08

## Context and Problem Statement

Laranet includes a CLI that creates projects and generates models, services,
controllers, middleware, resources, and optional repository scaffolds.

Code generators can make generated output depend on the tool at runtime. Common
approaches include base classes, attributes that drive behavior, partial classes
completed by the generator, or configuration files that must stay synchronized with
the code.

Those approaches make the generated application harder to read, harder to debug, and
harder to abandon. They also create a long-term compatibility obligation between the
tool and every generated project.

How should the CLI generate code without making applications depend on the generator
at runtime?

## Decision Drivers

- Generated code must be understandable and editable.
- Applications must remain normal .NET projects.
- The CLI must not become a runtime dependency.
- Users must be able to stop using the generator at any time.
- The generator must remain useful for common scaffolding tasks.
- Generation must not conflict with native tooling.

## Considered Options

- Generate code with mandatory runtime metadata, attributes, or base classes.
- Generate code through a custom runtime framework that resolves generated parts.
- Generate ordinary C# files that users own and can edit.

## Decision Outcome

Chosen option: **Generate ordinary C# files that users own and can edit.**

The CLI:

- Writes standard C# source files into the project.
- Does not add attributes, base classes, or interfaces that exist only for the tool.
- Does not require a generator-specific runtime package.
- Does not modify existing application code.
- Applies conventions for names, folders, and namespaces, which can be overridden
  with CLI options or user templates.

The generated project uses normal `dotnet` commands and can be edited, refactored, or
moved without losing functionality.

### Template overrides

Built-in code templates can be replaced through a `.laranet/templates` directory.
Templates support `{{Name}}` and `{{Namespace}}` placeholders. This provides
customization without introducing runtime metadata.

### Overwrite protection

Existing files are never overwritten unless `--force` is specified. When a command
generates several files, conflicts are reported before anything is written, so a
failed command does not leave a partial result.

## Consequences

### Positive Consequences

- Generated applications remain idiomatic .NET.
- Users can remove the CLI and keep working.
- Generated code is easy to review and debug.
- The CLI does not create a runtime compatibility surface.
- Template overrides cover common customization needs.
- No hidden synchronization between generator and application.

### Negative Consequences

- Regeneration does not update previously generated files automatically.
- Users are responsible for keeping generated code aligned with their changes.
- Conventions must be documented clearly because they are not enforced at runtime.
- Project templates are built in and are not yet customizable through the template
  directory.

## Confirmation

This decision is validated by:

- Generated projects that build and run with only the .NET SDK.
- Generated files containing no Laranet-specific runtime metadata.
- CLI tests that verify overwrite protection and template overrides.
- Samples that use the generated structure without generator-only dependencies.

## Related Decisions

- [ADR-0002: Use Optional Framework Integrations](0002-optional-integrations.md)
- [ADR-0003: Maintain Independent Project Versioning](0003-independent-versioning.md)
- [CLI Guide](../../guides/cli.md)
