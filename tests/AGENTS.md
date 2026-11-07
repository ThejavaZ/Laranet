# Tests — Agent Instructions

## Testing Strategy

Laranet uses automated tests to protect public contracts and architectural behavior.

The test suite currently uses:

- xUnit.
- Microsoft.NET.Test.Sdk.
- Entity Framework Core SQLite for integration testing.

The use of EF Core SQLite in the test project does not authorize adding EF Core dependencies to provider-neutral production modules.

## Test Organization

Tests are organized by type and module.

```text
tests/
└── Laranet.Tests/
    ├── Unit/
    │   └── Data/
    ├── Integration/
    │   └── Data/
    └── Laranet.Tests.csproj
```

Follow the existing organization when adding tests.

## Unit Tests

Unit tests should validate:

- Public API behavior.
- Argument validation.
- Default values.
- Boundary conditions.
- Query composition.
- Expected exceptions.
- No-op behavior where applicable.

Use descriptive test names following the existing naming convention:

`MethodName_ShouldExpectedBehavior_WhenCondition`

Examples:

- `Search_ShouldReturnMatchingItems_WhenTermIsProvided`
- `WhereIf_ShouldReturnOriginalQuery_WhenConditionIsFalse`
- `Paginate_ShouldLimitPageSize_WhenRequestedSizeExceedsMaximum`

## Integration Tests

Integration tests should verify behavior that cannot be established reliably through isolated unit tests.

For IQueryable extensions, use SQLite to verify:

- Expression translation.
- SQL generation.
- Query execution.
- Correct results.
- Composition between extensions.

Use the existing shared test entities and DbContext where appropriate.

Keep database connections alive for the lifetime of the in-memory SQLite database.

Dispose DbContext and connection resources correctly.

## Test Quality

Tests must:

- Be deterministic.
- Be independent.
- Avoid depending on execution order.
- Use meaningful test data.
- Assert observable behavior.
- Avoid unnecessary duplication.
- Avoid testing private implementation details.

Prefer explicit assertions over overly broad assertions.

Do not weaken or remove a test simply because it exposes a defect.

If a test fails:

1. Read the complete failure message.
2. Identify whether the issue is in the implementation, test setup, or expected behavior.
3. Reproduce the failure.
4. Fix the underlying cause.
5. Run the affected test.
6. Run the full suite when appropriate.

## Commands

Run all tests:

```bash
dotnet test
```

Run tests in Release:

```bash
dotnet test -c Release
```

Run a specific test:

```bash
dotnet test --filter "FullyQualifiedName~TestName"
```

Build the solution:

```bash
dotnet build -c Release
```

Never report successful tests without actually executing them.
