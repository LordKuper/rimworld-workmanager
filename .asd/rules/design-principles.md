# Design Principles

Applied during design-shaping phases (design, design-review, design-promote) and verified against during impl-review.

## 1. Evidence over Speculation

Do not build systems before the user-facing need is validated. Prefer a vertical slice over horizontal scaffolding. A new system, layer, or module proposed in ADR requires concrete usage evidence: a PRD scenario, measured pain, a named consumer. "We might need it later" does not qualify.

## 2. KISS — Simple Core

Simplest design that solves the validated problem. Three similar lines beat a premature abstraction. Any complication needs user approval — see `core.md` Simplicity Default + Complication Approval.

## 3. Separation of Concerns

Identify the natural layers (presentation / domain / data / integration). Each gets its own module with explicit contracts captured in the ADR. Cross-layer calls only through those contracts. No domain logic in UI, no persistence concerns in domain code, no business rules in transport adapters.

## 4. Loose Coupling / High Cohesion

A module groups things that change together (cohesion); modules depend through narrow contracts (coupling). Test: replacing one module must not require editing unrelated ones. One change touching many unrelated places signals broken cohesion or leaked coupling.

## 5. Single Source of Truth

Each fact has exactly one home file or module. Other places link or import, never copy. Applies to code as to docs (`artifact-layout.md` SSoT iron rule): one config source, one schema source, one constant source.

## 6. Fail Fast on Load

Validate content, data, and configuration at load time, not in runtime hot paths. Crash beats corrupt save. Crash beats silent fallback masking broken state.

## 7. Observability by Design

Every long-lived component ships from day one: structured logs (with context), metrics counters, traces, a health/readiness endpoint, inspectable runtime state. Add at design time; retrofitting is harder and less complete.

## 8. Backward Compatibility

Persisted formats (DB schema, on-disk state, public API, wire protocol) are versioned. A breaking change requires an ADR, a migration plan, and a version bump. Additive changes preferred. A release must not invalidate existing user data or integrations without a migration path. Gated by `config.backward_compat` (`strict` | `migration` | `none`).

## 9. Evolutionary Architecture

Defer commitment where the cost of reversal is high. Every ADR documents reversibility (easy / moderate / hard) and the trigger condition for re-evaluation. Architecture is grown, not poured.

## 10. Over-Engineering Smells

Apply the over-engineering checklist from `review-policy.md` PROACTIVELY during design, not only at review — flag during ADR drafting. Each item is `critical` and undroppable.

## See also

- `core.md` — Simplicity Default, Complication Approval, QODDA
- `artifact-layout.md` — SSoT iron rule, document responsibility
- `review-policy.md` — over-engineering checklist, severity floor
- `external-review.md` — Codex prompts encode several of these as checks
