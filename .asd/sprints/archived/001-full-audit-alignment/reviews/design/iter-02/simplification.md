---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-design-simplification]: APPROVE

# Review — simplification

- **Phase**: design-review
- **Iteration**: 2

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings at or above MEDIUM floor; no over-engineering checklist item trips | — |

## Verdict
APPROVE

## Next action
Reviewer done. No simplification concerns block the design gate. Remaining DoD reviewers (Documentation, UI, External Review if enabled) must also APPROVE in the same iteration for design-review to advance.

## Detail (rationale, non-blocking)

Each of the five ADRs adds only the complexity its fix needs, and each explicitly enumerates and rejects the heavier alternatives — exactly the proactive over-engineering screen `design-principles.md §10` asks for. Over-engineering checklist swept; nothing trips at any severity.

- **ADR-0001 (Instance null contract).** Adds exactly one one-line static member, `IsInitialized` (`Instance is not null`). Considered against "helper that wraps one expression without added value": it earns its weight — it names the documented invariant AC-11 requires and collapses the existing scattered `?.` / `== null` idiom into one uniform form, rather than adding a parallel access path. Rejected alternatives (nullable-typed `Instance` forcing `?.` into hot tick/cache loops; a `TryGetInstance`/`Current` second accessor; lazy-init; blanket guarding of all ~25 sites) are each the *more* complex path; the entry-point-guard rule with a game-scoped exemption deliberately avoids provably-dead defensive guards in hot paths. No new field, type, interface, or dependency. Keep as is.
- **ADR-0002 (test infra).** Two NUnit-native primitives (`[SetUpFixture]` + an abstract isolation base class), no new test dependency. The base class is not an abstraction-without-a-second-use-case: AC-2…AC-6 supply multiple deriving consumers, and snapshot/restore is the minimum deterministic fix for real static-state bleed. The `[SetUpFixture]` resolver follows the established `LordKuper.Common` convention, not a new invention. A mocking framework is explicitly rejected (Simplicity Default); no mock-of-a-mock. Keep as is.
- **ADR-0003 (delete vendored Harmony 2.3.6).** Pure deletion of unreferenced dead weight; removes complexity, adds none. No "dead code kept in case we need it" — it is being removed. Keep as is.
- **ADR-0004 (tech-reference coverage).** Two flat Markdown docs; the one-combined-"platform"-doc option is rejected to preserve the existing one-file-per-tech-version convention. No abstraction or layer introduced. Keep as is.
- **ADR-0005 (localize work-shift labels).** One parameterized keyed entry replaces two hardcoded strings; per-string keys are implicitly avoided in favor of a single positional-argument key. English output preserved byte-for-byte. No premature config flag, no new surface. Keep as is.

Cross-reviewer guard: none of the proposed solutions, if implemented as written, introduces a new abstraction, layer, interface, dependency, or config flag that would require a Complication Approval. No escalation triggered.

## Escalations (optional)
- none
