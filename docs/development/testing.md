# Testing

## Commands

Build the full solution:

```bash
dotnet build Laranet.slnx --configuration Release
```

Run every test:

```bash
dotnet test Laranet.slnx --configuration Release --no-build
```

Run a single test class:

```bash
dotnet test Laranet.slnx --configuration Release --no-build \
  --filter "FullyQualifiedName~PaginationIntegrationTests"
```

The CI workflow runs restore, build, tests, and package validation on every push and
pull request to `main`.

## Test projects

| Project | Purpose |
| --- | --- |
| `tests/Laranet.Tests` | Unit, integration, and CLI tests. |

Test categories live under `tests/Laranet.Tests`:

- `Unit/` verifies contracts, validation, name normalization, and isolated behavior.
- `Integration/` verifies provider translation, ASP.NET Core responses, and CLI
  generation against real file systems.
- `TestSupport/` contains shared helpers such as temporary directories and the SQLite
  database fixture.

## SQLite integration tests

Provider behavior is verified with SQLite through EF Core. The SQLite dependency is
kept in the test project only; production packages must not reference it.

Integration tests should:

- Use a real `DbContext` and a real provider.
- Verify observable results and generated commands.
- Avoid asserting implementation details that a provider may change.
- Clean up temporary databases and directories.

## What to test

For a new behavior, cover:

- The expected result.
- Boundary values and invalid arguments.
- Empty and out-of-range inputs.
- Composition with existing operations when relevant.
- Provider translation when the behavior depends on `IQueryable`.

Tests must be deterministic and independent. Do not rely on execution order, the
current culture, or the machine's clock.

## Rules

- Do not modify tests to make an incorrect implementation pass.
- Do not claim a test passed without running it.
- Do not add a dependency to the test project without a concrete need.
- Prefer testing public behavior over private implementation details.

## Documentation checks

Documentation examples should compile. When an example changes, update the matching
sample under [`samples/`](../../samples) and run the solution build so the compiler
verifies it.

## Related

- [Contributing](../../CONTRIBUTING.md)
- [Release process](release.md)
