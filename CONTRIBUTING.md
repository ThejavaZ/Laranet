# Contributing to Laranet

First of all, thank you for considering contributing to Laranet!

Laranet is an open-source project focused on improving the .NET developer experience
through expressive APIs, reusable abstractions, and developer-friendly tools.

We welcome bug reports, feature suggestions, documentation improvements, and code
contributions.

## Table of Contents

- [Getting Started](#getting-started)
- [Ways to Contribute](#ways-to-contribute)
- [Development Workflow](#development-workflow)
- [Building and Testing](#building-and-testing)
- [Coding Standards](#coding-standards)
- [Commit Conventions](#commit-conventions)
- [Pull Requests](#pull-requests)
- [Documentation](#documentation)
- [Reporting Issues](#reporting-issues)
- [Security](#security)
- [Feedback](#feedback)

## Getting Started

Before contributing, make sure you have the following installed:

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)
- An IDE or code editor with C# support

Clone the repository:

```bash
git clone https://github.com/TheJavaZ/Laranet.git
cd Laranet
```

Restore dependencies and build the solution:

```bash
dotnet restore
dotnet build Laranet.slnx
```

> Laranet is currently in early development. Some workflows and public APIs may
> evolve before the first stable release.

Read [AGENTS.md](AGENTS.md) for the repository-specific architecture and design
constraints. The [documentation](docs/README.md) explains the packages and their
behavior.

## Ways to Contribute

There are several ways to contribute:

- Report bugs and unexpected behavior.
- Suggest new features or improvements.
- Improve documentation and examples.
- Submit bug fixes.
- Implement approved features.
- Improve tests and code quality.

For significant changes, open an issue first to discuss the proposed implementation.
Small fixes and documentation corrections can go directly to a pull request.

## Development Workflow

1. **Inspect.** Read the relevant project, public API, and existing tests.
2. **Analyze.** Identify the actual problem rather than a symptom.
3. **Propose.** Explain the intended change, affected files, and trade-offs.
4. **Implement.** Keep the change focused on the approved scope.
5. **Verify.** Build the solution and run the relevant tests.
6. **Report.** Summarize the change, the decisions, and any remaining concerns.

Guidelines:

- Do not make unrelated changes while implementing a feature.
- Do not introduce an abstraction, dependency, or configuration option without a
  concrete justification.
- Do not modify unrelated files or reformat entire files.
- Do not remove or weaken tests to make a build pass.
- Do not claim that a build or test passed unless it was executed.

## Building and Testing

Build the full solution:

```bash
dotnet build Laranet.slnx --configuration Release
```

Run the full test suite:

```bash
dotnet test Laranet.slnx --configuration Release --no-build
```

The CI workflow restores, builds, tests, and validates the production packages on
every push and pull request to `main`.

See [Testing](docs/development/testing.md) for test organization and conventions.

## Coding Standards

All contributions must follow the repository's `.editorconfig` configuration.

General guidelines:

- Use meaningful names for types, methods, properties, and variables.
- Follow standard C# naming conventions.
- Enable nullable reference types and handle nullability correctly.
- Prefer explicit and predictable behavior over clever code.
- Keep methods focused on a single responsibility.
- Validate public API arguments where appropriate.
- Document public APIs with XML documentation.
- Avoid comments that merely repeat the code.
- Avoid unnecessary dependencies.

Warnings are treated as errors. Do not suppress a warning without understanding why it
occurs and documenting the reason.

Public APIs are part of the product. Before adding one, consider whether it is
intuitive, predictable, composable with existing .NET APIs, and worth its long-term
compatibility cost.

## Commit Conventions

Use [Conventional Commits](https://www.conventionalcommits.org/):

```text
<type>(<scope>): <description>
```

Common types:

| Type | Purpose |
| --- | --- |
| `feat` | New functionality |
| `fix` | Bug fix |
| `refactor` | Restructuring without behavior changes |
| `docs` | Documentation changes |
| `test` | Adding or updating tests |
| `build` | Build system or dependency changes |
| `ci` | CI configuration changes |
| `chore` | Maintenance tasks |

Examples:

```text
feat(cli): add configurable project layouts
test(data): cover out-of-range pagination
docs(roadmap): split roadmap into versioned documents
```

Keep commits focused. Do not combine unrelated changes in one commit.

## Pull Requests

Use the [pull request template](.github/PULL_REQUEST_TEMPLATE.md). A good pull
request:

- Explains the problem and the chosen solution.
- Links the related issue when one exists.
- Lists the main changes.
- States how the change was tested.
- Calls out compatibility and documentation impact.
- Keeps the diff as small as the problem allows.

Before requesting review:

- [ ] The solution builds without warnings.
- [ ] Relevant tests pass.
- [ ] New behavior has tests.
- [ ] Public APIs are documented.
- [ ] Documentation and changelog are updated when appropriate.
- [ ] No unrelated files are modified.

## Documentation

Documentation is part of the product. When changing behavior:

- Update the package README when the public API or usage changes.
- Update the guides under `docs/guides/` when behavior or recommendations change.
- Update `CHANGELOG.md` for user-facing changes.
- Keep examples compilable. The samples under `samples/` are built by CI.

Do not document planned functionality as if it already exists. Distinguish verified
behavior from provider-dependent or expected behavior.

## Reporting Issues

Use the GitHub issue templates:

- [Bug report](.github/ISSUE_TEMPLATE/bug_report.yml)
- [Feature request](.github/ISSUE_TEMPLATE/feature_request.yml)
- [Documentation issue](.github/ISSUE_TEMPLATE/documentation.yml)

For bugs, include:

- Laranet package and version.
- .NET target framework and SDK version.
- Integration package versions, such as EF Core or ASP.NET Core.
- Operating system when relevant.
- Minimal reproduction steps and the relevant error output.

Feature requests should describe the problem before proposing a solution. Laranet
prefers additions that solve demonstrated problems.

## Security

Do not report security vulnerabilities through public issues. See
[SECURITY.md](SECURITY.md) for the private reporting process.

## Feedback

Questions, ideas, and general discussion belong in
[GitHub Discussions](https://github.com/TheJavaZ/Laranet/discussions). Feedback from
real applications directly shapes the [roadmap](docs/roadmap/README.md).

## License

By contributing, you agree that your contributions are licensed under the repository's
[MIT License](LICENSE).
