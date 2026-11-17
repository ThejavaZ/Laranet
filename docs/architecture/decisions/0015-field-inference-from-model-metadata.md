# ADR-0015: Infer Admin Fields from Model Metadata

- **Status:** Accepted — prototype implemented, pending real-application validation
- **Date:** 2026-11-16
- **Related:** ADR-0014 (reduce registration ceremony), ADR-0012
  (provider-neutral core), ADR-0006 (toolkit with framework-like tooling).

## Context

ADR-0014 removed the registration ceremony; the remaining repetition is field
declaration. Every model property is declared twice: once in the model and again
in `Configure()` — `resource.Field(p => p.Name)`, labels, types, and required
flags restate what the type system and DataAnnotations already know. For a
resource with a handful of properties this is a dozen lines of restatement, and
it is the largest remaining block of admin code.

Django reaches its simplicity by introspecting model metadata: `ModelForm` and
the admin derive fields, labels, validation, and widgets from the model
declaration. .NET's native equivalent of that metadata is DataAnnotations
(`[Key]`, `[Required]`, `[Display]`, `[Range]`, `[Editable]`,
`[ScaffoldColumn]`) plus the CLR type itself.

Two existing decisions constrain the shape of this one:

- ADR-0012: the core stays provider-neutral — inference must not depend on
  EF Core's model.
- ADR-0006: tooling may feel framework-like, but the runtime stays explicit —
  inference must resolve once at startup and remain overridable.

## Decision

1. **`AdminResource<TModel>` seeds the builder with inferred fields** before
   `Configure` runs:
   - every readable public instance property, in declaration order (indexers and
     static properties are excluded);
   - properties marked `[ScaffoldColumn(false)]` are excluded;
   - mappings: `[Key]` → key (the first key only — validation requires exactly
     one; otherwise the `Id` name convention applies), `[Required]` → required,
     `[StringLength]`/`[MaxLength]` → max length, `[MinLength]` → min length,
     `[Range]` → min/max, `[Display(Name)]` → label,
     `[Display(Description)]` → help text, `[Editable(false)]` or a getter-only
     property → read-only;
   - the input control keeps using the existing CLR type inference.
2. **Explicit always wins through merge semantics.** `Field(selector, label)`
   now updates the existing entry instead of replacing it: arguments you pass
   override the inferred value and everything else is preserved. This also fixes
   a latent bug where updating a non-last field returned the builder for the
   last field, so chained configuration landed on the wrong property.
3. **Model-first registration.** `AddResourceFromModel<TModel>(permissionPolicy,
   configure?)` builds a complete resource from the model alone — no resource
   class required for standard CRUD. The permission policy is mandatory because
   the authorization bridge denies operations without one.
4. With no configuration at all, a model still produces a complete field
   surface. Actions, filters, and ordering remain explicit metadata.
5. A model whose key cannot be resolved (no `[Key]` and no `Id`) fails
   registration through the existing "exactly one key field" validation.

### Alternatives considered

- **Opt-in `InferFields()`** — rejected: it breaks the "no configuration"
  promise from ADR-0014 and leaves every existing resource behind.
- **EF Core `IModel` introspection** — rejected: provider metadata would leak
  into the provider-neutral core (ADR-0012).
- **Source generators** — rejected: build-time complexity ahead of a
  demonstrated need; reflection over a cached metadata set, resolved once per
  resource at startup, is sufficient.

## Consequences

Positive:

- `Configure()` shrinks to overrides; a model-first resource is a single line.
- DataAnnotations become the single source of truth, shared with the rest of
  the .NET stack instead of a parallel fluent vocabulary.
- Field labels and validation stop diverging from the model declaration.

Negative:

- Typed resources gain fields for properties they never declared — a behavior
  change. The admin surface is experimental (ADR-0012), and the sample, guide,
  and tests were updated in the same change.
- `Field()` redeclaration semantics change from "reset" to "merge" — required
  for explicit-wins and documented here.
- Reflection runs once per resource at startup, never per request.
