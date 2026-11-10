# Testing Guidelines

This page describes how tests are written and organized. For the commands used to run
them, see [Testing](testing.md) and the
[development workflow](development-workflow.md).

## Test project

All tests live in `tests/Laranet.Tests`:

```text
tests/Laranet.Tests/
├── Unit/
│   ├── AspNetCore/
│   ├── Cli/
│   ├── Data/
│   └── EntityFrameworkCore/
├── Integration/
│   ├── AspNetCore/
│   ├── Cli/
│   ├── Data/
│   └── EntityFrameworkCore/
└── TestSupport/
```

| Folder | Purpose |
| --- | --- |
| `Unit/` | Contracts, argument validation, name normalization, and isolated behavior. |
| `Integration/` | Provider translation, HTTP responses, and CLI generation against real file systems and SQLite. |
| `TestSupport/` | Shared helpers, fixtures, and temporary directories. |

## Choosing the test type

Use a unit test when:

- The behavior is independent of a provider or framework.
- You are verifying argument validation, normalization, or a pure calculation.
- The test can run without I/O.

Use an integration test when:

- The behavior depends on `IQueryable` translation.
- The result depends on EF Core, ASP.NET Core, or the file system.
- A provider could translate an expression differently.

Do not mock `IQueryable` to test translation. If translation matters, run it against a
real provider.

## Naming

Use descriptive names that state the scenario and the expected outcome:

```csharp
[Fact]
public void Paginate_WithPageBeyondResults_ReturnsEmptyItemsAndPreservesCount()

[Fact]
public void Search_WithMultipleSelectors_MatchesAnyProperty()

[Fact]
public void OrderBy_WithDisallowedProperty_ThrowsArgumentException()
```

Avoid names that describe the implementation (`CallsInternalMethod`) or names that do
not state an expectation (`PaginationTest1`).

## Structure

Follow arrange–act–assert:

```csharp
[Fact]
public void Paginate_WithPageSizeAboveMaximum_UsesMaximum()
{
    // Arrange
    var source = Enumerable.Range(1, 200).AsQueryable();

    // Act
    var result = source.Paginate(page: 1, pageSize: 500, maxPageSize: 50);

    // Assert
    Assert.Equal(50, result.PageSize);
    Assert.Equal(50, result.Items.Count);
}
```

Keep each test focused on one behavior. A test that asserts several unrelated outcomes
is harder to diagnose.

## Determinism

Tests must be deterministic and independent:

- Do not rely on execution order.
- Do not rely on the current culture.
- Do not rely on the machine clock.
- Do not share mutable state between tests.
- Create and dispose temporary databases and directories per test.

Use the shared helpers in `TestSupport` instead of duplicating setup code.

## SQLite integration tests

Provider behavior is verified with SQLite through EF Core. SQLite is a test-only
dependency; production packages must not reference it.

A data integration test typically:

1. Creates a `DbContext` with a unique SQLite database.
2. Seeds the required rows.
3. Executes the Laranet operation.
4. Asserts the materialized result and, when relevant, the executed commands.

`CommandCaptureInterceptor` records the commands so a test can verify that a query was
translated and executed as expected, without asserting the exact SQL text.

## CLI tests

CLI tests generate projects and files in temporary directories. They should:

- Use a fresh directory per test.
- Assert the generated file paths and content.
- Build the generated project when the test verifies compilation.
- Clean up the directory, even when the test fails.

Do not assert exact console formatting unless formatting is the behavior under test.

## What to test

For a new behavior, cover:

- The expected result.
- Boundary values.
- Invalid arguments and the exception they produce.
- Empty input and out-of-range pages.
- Composition with existing operations.
- Provider translation when the behavior depends on `IQueryable`.

For a bug fix, add a regression test that fails before the fix and passes after it.

## What not to test

- Private implementation details.
- Exact SQL text, unless the SQL shape is the contract.
- Internal helper methods that are already covered through public behavior.
- Framework behavior that Laranet does not own.

## Updating tests

- Do not modify a test to make an incorrect implementation pass.
- If a contract changes intentionally, update the test and document the reason in the
  pull request and changelog.
- Keep test names and assertions aligned with the public behavior.

## Related

- [Testing](testing.md)
- [Development workflow](development-workflow.md)
- [Contribution guide](contribution-guide.md)
