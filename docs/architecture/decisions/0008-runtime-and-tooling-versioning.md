# ADR-0008: Keep Runtime and Tooling Packages on One Version Train by Default

- **Status:** Accepted
- **Date:** 2026-11-09

## Context and Problem Statement

Laranet distributes two kinds of packages:

- **Runtime packages** are referenced by application code: `Laranet`, `Laranet.Data`,
  `Laranet.EntityFrameworkCore`, `Laranet.AspNetCore`, and the candidate
  `Laranet.Admin`, `Laranet.Admin.AspNetCore`, and `Laranet.AspNetCore.Validation`.
- **Tooling packages** are used at build time or from the command line:
  `Laranet.Cli` and the candidate `Laranet.Analyzers`.

Both kinds have different compatibility surfaces. A runtime package promises a stable
public API for application code. A tooling package promises stable commands, generated
output, Roslyn and MSBuild compatibility, and, in the case of analyzers, permanent
diagnostic IDs.

[ADR-0003](0003-independent-versioning.md) requires packages that belong to the same
Laranet release to share a consistent version, but explicitly allows a future
architectural decision to establish a justified exception. The
[versioning policy](../../compatibility/versioning.md) also states that packages which
are not part of a particular release do not need to be published.

Version 5 proposes a `1.0.0` release and introduces analyzers. Forcing every package to
reach `1.0.0` at the same time would freeze the CLI interface, the analyzer rule set,
and the diagnostic IDs as part of the same commitment as the runtime API.

The question is not whether independent versioning is possible. ADR-0003 already
permits it. The question is **when it is justified**, and what must be true before the
project pays its operational cost.

## Decision Drivers

- Predictable package upgrades for consumers.
- A clear compatibility promise for runtime packages.
- Room for tooling to evolve at its own pace when its contract requires it.
- Permanent diagnostic IDs and stable CLI output.
- A credible, evidence-based `1.0.0`.
- Avoid a premature split that multiplies documentation, support, and publishing work.

## Considered Options

### Option 1 — One synchronized train forever

Every package always shares the same version and release date.

- Advantage: the simplest story for consumers.
- Risk: tooling cannot make a breaking change without a major release of the runtime,
  and the runtime cannot ship a patch without dragging tooling along.

### Option 2 — Split runtime and tooling immediately

Runtime packages move to `1.0.0`; tooling packages stay in `0.x` with their own
calendar.

- Advantage: each surface evolves according to its own contract.
- Risk: two support matrices, two documentation tracks, and consumer confusion before
  any real need exists.

### Option 3 — One train by default, with documented split triggers

Packages remain synchronized while no trigger is met. A split requires a new ADR and a
concrete justification.

- Advantage: preserves the simple consumer story and avoids speculative complexity.
- Risk: the triggers must be concrete enough to avoid drifting into an accidental
  permanent coupling.

## Decision Outcome

Chosen option: **Option 3 — one synchronized train by default, with documented split
triggers.**

### Definitions

- **Release train:** the set of packages published together with the same version.
- **Runtime lane:** packages referenced by application code.
- **Tooling lane:** packages used at build time or from the command line.

### Rules

1. Packages published in the same release share the same version.
2. A package that did not change may be omitted from a release. Omission does not
   authorize a breaking change and does not create an independent support promise.
   This is the existing behavior of the
   [versioning policy](../../compatibility/versioning.md#5-package-version-consistency).
3. Tooling packages are not required to reach `1.0.0` at the same time as runtime
   packages. Their inclusion in the `1.0.0` train is a separate decision.
4. Runtime and tooling packages may be split into independent lanes only when at least
   one trigger below is met and a new ADR records the decision.
5. No package may adopt an independent version outside that process.

### Split triggers

A split is justified when at least one of the following is true:

- **Breaking tooling change while the runtime must stay compatible.** For example, a
  diagnostic ID must be retired, or a CLI command must change incompatibly, while
  runtime packages must remain on the same major version.
- **Platform-driven cadence.** Roslyn, MSBuild, or an IDE requires a tooling release
  that cannot be aligned with the runtime release schedule.
- **Blocked release.** A runtime patch or minor release would be delayed solely because
  a tooling package is not ready, or the reverse.
- **Unavoidable documentation or support cost.** Keeping the train synchronized forces
  a compatibility matrix or deprecation story that is materially harder to explain than
  two independent lanes.

The following are **not** sufficient triggers on their own:

- A tooling package simply releases more often.
- A tooling package is small.
- Independent versioning is technically possible.
- A future package might need it.

### What a split ADR must define

Before any lane becomes independent, the approving ADR must specify:

- Which packages move to the independent lane and why.
- The compatibility matrix between tooling versions and runtime major versions.
- The support window for each lane.
- How consumers discover compatible combinations.
- How release notes, changelogs, and package metadata communicate the split.
- How `verify-packages.sh` validates the matrix instead of a single shared version.
- How the split can be reversed if it proves unnecessary.

## Relationship with 1.0.0

`1.0.0` is a statement about the **runtime surface**, not about completing a roadmap
milestone.

Runtime packages reach `1.0.0` only when all of the following are true:

- The documented public API is frozen.
- A public API compatibility check runs in CI and fails on unapproved changes.
- A migration guide exists for every breaking change since the previous minor.
- At least one real application has upgraded across a minor release using only
  documented changes.
- The provider matrix is green for the supported providers.

Tooling packages may reach `1.0.0` later, or remain in `0.x` while the runtime is
stable. That outcome is acceptable only after the split process above has been
followed; it must not happen implicitly through an omitted release.

## Consequences

### Positive Consequences

- Consumers keep a single version to reason about while the ecosystem is small.
- The runtime can make a credible `1.0.0` promise without freezing tooling UX.
- A future split has explicit, reviewable triggers instead of an improvised decision.
- Diagnostic IDs and CLI contracts receive the stability they need when the split
  happens.

### Negative Consequences

- The project must review the triggers at each minor release.
- Until a trigger is met, tooling changes are constrained by the train.
- A split will require a second support matrix and additional release automation.
- A deferred split may still arrive at the least convenient moment if the triggers are
  not reviewed regularly.

## Confirmation

This decision will be validated through:

- `verify-packages.sh` enforcing one shared version per release train.
- A public API compatibility gate in CI.
- A release review that records whether any split trigger was met.
- The versioning policy and roadmap links updated when the decision is revised or
  superseded.

## Out of Scope

- The diagnostic catalog, rule IDs, and the contract shared by analyzers and
  `laranet doctor`. That requires its own ADR.
- Supply-chain hardening: package signing, SBOM, build provenance, and vulnerability
  scanning.
- The release date or sprint plan for `1.0.0`.

## Related Decisions

- [ADR-0003: Maintain Independent Project Versioning](0003-independent-versioning.md)
- [Versioning Policy](../../compatibility/versioning.md)
- [Support Policy](../../compatibility/support-policy.md)
- [Roadmap](../../roadmap/README.md)
