# Architecture Decision Records

This directory contains the Architecture Decision Records (ADRs) for Laranet.

ADRs document significant architectural decisions, including their context, considered alternatives, rationale, and consequences.

Their purpose is to preserve architectural knowledge and provide transparency into why Laranet is designed the way it is.

## Decision Records

| ADR                                    | Decision                                | Status   |
| -------------------------------------- | --------------------------------------- | -------- |
| [0001](0001-independent-core.md)       | Keep the Core Independent               | Accepted |
| [0002](0002-optional-integrations.md)  | Use Optional Framework Integrations     | Accepted |
| [0003](0003-independent-versioning.md) | Maintain Independent Project Versioning | Accepted |
| [0004](0004-editable-generated-code.md) | Generate Ordinary, Editable C#         | Accepted |
| [0005](0005-intentionally-empty-core.md) | Keep the Core Package Intentionally Empty | Accepted |
| [0006](0006-toolkit-with-framework-tooling.md) | Position Laranet as a Modular Toolkit with Framework-like Tooling | Accepted |
| [0007](0007-simple-generation-profile.md) | Default to the Simplest Generation Profile | Accepted |
| [0008](0008-runtime-and-tooling-versioning.md) | Keep Runtime and Tooling Packages on One Version Train by Default | Accepted |

## ADR Status

The following statuses are used:

- **Proposed:** The decision is under consideration.
- **Accepted:** The decision has been approved.
- **Rejected:** The proposed decision was not adopted.
- **Deprecated:** The decision is no longer recommended.
- **Superseded:** A newer ADR replaces the decision.

## Guidelines

- Create an ADR for decisions that significantly affect architecture, dependencies, public APIs, or project direction.
- Use sequential four-digit numbering.
- Keep each ADR focused on a single decision.
- Document the context and alternatives considered.
- Include both positive and negative consequences.
- Keep accepted ADRs as historical records.

Accepted ADRs should not be silently rewritten when architectural decisions change.

If a decision needs to change, create a new ADR referencing the previous one and mark the previous record as superseded.

## Format

Laranet uses a lightweight Markdown-based ADR format inspired by [MADR (Markdown Architectural Decision Records)](https://adr.github.io/madr/).

## Scope

ADRs document architectural decisions, not implementation details or general usage instructions.

For broader architectural information, see the [Architecture Overview](../overview.md).
