---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-design-simplification]: APPROVE

# Review — simplification

- **Phase**: design-review
- **Iteration**: 1

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings at or above floor (low) | — |

## Verdict
APPROVE

## Next action
Reviewer done. No simplification findings; the design adds only the complexity each audit fix requires.

## Assessment notes (informational — not findings)

Severity floor: low (iteration 1). Full over-engineering checklist (review-policy.md §"Over-engineering checklist") applied. No item trips.

- **ADR-0001 (Instance null contract).** Minimal option chosen and defended. `Instance` stays non-null; one static `IsInitialized` (`Instance is not null`) plus entry-point guards. Three heavier alternatives (`TryGetInstance`/`Current` new API, nullable retype spreading `?.` into hot loops, lazy-init) are explicitly rejected on Simplicity-Default grounds. `IsInitialized` was weighed against "helper that wraps one stdlib call without added value" (checklist) — it survives: it is not a stdlib wrapper but a named contract that unifies the scattered `?.`/`==null` idiom across ~25 sites and documents the lifecycle invariant. No new field, type, interface, or dependency. The two-rule split (guard at UI entry points; trust lifecycle in game-scoped paths) is justified by avoiding provably-dead guards in tick/cache hot paths, not speculative.
- **ADR-0002 (test infra).** Two NUnit primitives only: a global `[SetUpFixture]` (required to register the RimWorld `AssemblyResolve` before any RimWorld-typed test loads) and an abstract `StateIsolationTestBase` (snapshot/restore static state). Checked against "abstraction with no second use case" and "inheritance depth ≥ 3 without polymorphic dispatch": the base class has five concrete consumers (AC-2…AC-6), inheritance depth is 1, and pure-logic tests may skip it — proportionate, not premature. No mocking framework is added, so "mock of a mock" cannot arise; the ADR defers mocking until a sprint actually tests game-coupled paths. No "framework wrapping a framework" — `[SetUpFixture]` is host-mandated infrastructure, not a wrapper. No premature config flag.
- **ADR-0003 (remove vendored Harmony 2.3.6).** Pure deletion of dead, unreferenced binaries. Directly clears a "dead code left in case we need it" smell rather than introducing one. No new structure.
- **ADR-0004 (tech-reference coverage).** Two docs under the existing `<tech>-<version>.md` one-file-per-tech convention; the combined "platform" doc was explicitly rejected for conflating independently-versioned technologies. No new abstraction.
- **ADR-0005 (localize work-shift labels).** One parameterized keyed entry reusing the existing `WorkManager.Settings_Schedule_*` convention; byte-identical English output. Minimal.

Cross-reviewer guard: no reviewer-proposed fix observed in this iteration that would itself add an abstraction, layer, or dependency requiring Complication Approval.

## Escalations (optional)
- none
