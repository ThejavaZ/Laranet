# ADR-0016: Synthesize Per-Operation Policies On Demand

- **Status:** Accepted — prototype implemented, pending real-application validation
- **Date:** 2026-11-16
- **Related:** ADR-0014 (reduce registration ceremony), ADR-0015 (field
  inference), ADR-0013 (admin architecture and security boundary).

## Context

P0 of ADR-0014 removed the loop that registered `Resource.Operation` policies
(`Admin.Products.Create`, …) because the authorization bridge already falls
back to the resource's base policy. That removed boilerplate, but it also left
per-operation granularity available only to consumers that know about the
bridge's fallback: a Razor Page or Minimal API endpoint annotated with
`[Authorize(Policy = "Admin.Products.Delete")]` receives nothing — the policy
name does not exist unless the application registers it by hand.

Applications that need one stricter operation (delete requires a role, bulk
actions require a permission claim) currently face the choice between
registering all six operations again or writing a custom
`IAuthorizationPolicyProvider`.

.NET already has the standard extension point for this: a custom
`IAuthorizationPolicyProvider` (the documented "fallback policy provider"
pattern). The provider resolves registered policies first and only synthesizes
what is missing — the same "explicit wins" rule that ADR-0014 and ADR-0015
established for registration and field inference.

## Decision

1. **`AdminAuthorizationPolicyProvider`** (registered by `AddLaranetAdmin`)
   implements `IAuthorizationPolicyProvider`:
   - registered policies from `AuthorizationOptions` are returned first — an
     explicit application registration always wins;
   - a name of the form `{Base}.{AdminOperation}` is synthesized only when
     `{Base}` matches the `PermissionPolicy` of a registered resource and the
     suffix parses as a defined `AdminOperation` (letter-initial, exact case);
   - the synthesized policy clones the requirements of the registered base
     policy, so behavior is identical to the P0 fallback — but the operation
     name itself now resolves for every consumer, not only the bridge;
   - unknown names return null, exactly like the default provider.
2. **Registration order**: the provider is registered with `TryAdd` before
   `AddAuthorization()`, so it occupies the slot the default provider would
   take. An application that registered its own provider first still wins.
3. The default, fallback, and dynamic policy lookups delegate to
   `AuthorizationOptions` unchanged — `[Authorize]` without a policy name keeps
   its standard behavior.

### Alternatives considered

- **Bridge-only fallback (status quo)** — rejected: only the admin bridge
  benefits; endpoint-level `[Authorize(Policy = ...)]` and other consumers
  receive nothing.
- **Synthesize from the name pattern alone** (`X.Y.Z` → `X.Y`) — rejected: far
  too broad; any three-segment policy name would be fabricated.
- **Eager registration of all six operations per resource** — rejected: it is
  the boilerplate P0 removed, and it cannot express "only Delete is special".

## Consequences

Positive:

- Per-operation policies work everywhere with zero registration; specializing
  one operation is a single explicit `AddPolicy` call.
- One global resolution rule matches the explicit-wins philosophy of ADR-0014
  and ADR-0015.
- No changes to the authorization bridge or to application code.

Negative:

- The provider occupies a global extension point; it must delegate faithfully
  for every non-admin policy name (covered by tests).
- Synthesized policies share requirement instances with the base policy —
  correct because requirements are stateless, but worth documenting.
- `TryAdd` ordering means an application provider registered *after*
  `AddLaranetAdmin` will be skipped; this is documented in the guide.
