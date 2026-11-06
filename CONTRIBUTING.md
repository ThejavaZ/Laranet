# Contributing to Laranet

First of all, thank you for considering contributing to Laranet!

Laranet is an open-source project focused on improving the .NET developer experience through expressive APIs, reusable abstractions, and developer-friendly tools.

We welcome bug reports, feature suggestions, documentation improvements, and code contributions.

## Table of Contents

- [Contributing to Laranet](#contributing-to-laranet)
  - [Table of Contents](#table-of-contents)
  - [Getting Started](#getting-started)
  - [Ways to Contribute](#ways-to-contribute)
  - [Development Guidelines](#development-guidelines)
  - [Coding Standards](#coding-standards)

## Getting Started

Before contributing, make sure you have the following installed:

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Git](https://git-scm.com/)
- An IDE or code editor with C# support.

Clone the repository:

```bash
git clone https://github.com/TheJavaZ/Laranet.git
cd Laranet
```

Restore dependencies and build the solution:

```bash
dotnet restore
dotnet build
```

> Note: Laranet is currently in early development. Some projects and development workflows may not be available yet.

## Ways to Contribute

There are several ways to contribute:

- Report bugs and unexpected behavior.
- Suggest new features or improvements.
- Improve documentation and examples.
- Submit bug fixes.
- Implement approved features.
- Improve tests and code quality.

For significant changes, please open an issue first to discuss the proposed implementation.

## Development Guidelines

Laranet follows a few fundamental principles:

- **Simplicity:** Prefer straightforward solutions over unnecessary complexity.
- **Expressiveness:** Public APIs should be intuitive and readable.
- **Extensibility:** Design abstractions that developers can customize.
- **Compatibility:** Avoid breaking existing public APIs without a clear reason.
- **Native integration:** Respect existing .NET conventions and ecosystem standards.
- **Minimal dependencies:** Avoid introducing dependencies without a clear technical justification.

Contributions should solve real problems rather than introduce abstractions without a practical purpose.

## Coding Standards

All contributions must follow the repository's `.editorconfig` configuration.

General guidelines:

- Use meaningful names for types, methods, properties, and variables.
- Follow standard C# naming conventions.
- Enable nullable reference types.
- Prefer explicit and predictable behavior.
- Keep methods focused on
