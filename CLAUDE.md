# CLAUDE.md

## Claude Code Instructions

@AGENTS.md

## Additional Guidelines

When working on Laranet:

- Follow the repository's architectural decisions and established conventions.
- Inspect existing implementations before proposing new abstractions.
- Prefer small, incremental changes.
- Do not redesign public APIs without discussing the compatibility implications.
- Do not introduce Laravel-specific concepts into the internal implementation merely to imitate Laravel.
- Preserve native .NET developer experience.

## Before Implementing Features

1. Understand the intended developer experience.
2. Review existing APIs that solve related problems.
3. Identify the appropriate module.
4. Define the expected public contract.
5. Consider provider compatibility and dependency impact.
6. Implement only what is necessary.

## During Code Review

Prioritize:

- Correctness.
- API consistency.
- Maintainability.
- Compatibility.
- Performance where relevant.
- Test quality.
- Architectural boundaries.

Avoid recommending abstractions or patterns without a concrete justification.

## Communication

When proposing changes:

- Explain the problem first.
- Present the proposed solution.
- Identify relevant trade-offs.
- Mention affected files.
- Report tests and build results accurately.

Do not silently make broad architectural changes.
