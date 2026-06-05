[REVIEW-impl-simplification]: CONCERNS

# Review — simplification

- **Phase**: impl-review
- **Iteration**: 4

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | critical | `Source/WorkManager.Tests/StateIsolationTestBase.cs:43-46, 53-56` | `GetWorkManagerGameComponentType()` and `GetWorkTypeAssignmentRuleType()` are private helpers whose entire body is `return typeof(X);`. Each wraps a single language operator with no added value (over-engineering checklist: "Helper that wraps one stdlib call without added value"). The XML-doc on each also restates what the code does and argues against an alternative nobody proposed ("Uses the direct typeof operator rather than string-based lookup…") — over-engineering checklist: "Comment that restates code". Category: **simplify**. | Inline `typeof(WorkManagerGameComponent)` / `typeof(WorkTypeAssignmentRule)` at the two call sites each (in `Snapshot/RestoreState` and the Instance get/set helpers); delete the two wrapper methods and their doc comments. No behavior change. |

<!-- Reviewed and explicitly cleared (no finding): -->
<!-- IsInitialized: one-line property `Current.Game != null && Instance is not null`. Minimal; no abstraction. KEEP-AS-IS. -->
<!-- UI guards: single `if (!IsInitialized) return;` at each entry point (Patches/**, AutoWorkPriorities/AutoWorkSchedule DoCell), then direct Instance deref. No double-guarding, no leaf guards. Matches ADR-0001 entry-point idiom. KEEP-AS-IS. -->
<!-- Game-scoped consumers keep direct deref with XML-doc invariant note — correctly NOT defensively guarded (no defensive code for impossible-by-contract case). KEEP-AS-IS. -->
<!-- Test infra (RimWorldAssemblyResolverFixture, StateIsolationTestBase): proportionate. Resolver is the minimal AssemblyResolve handler ADR-0002 requires; snapshot/restore reflection is justified by internal static state crossing the assembly boundary. No mocking framework added (no mock-of-a-mock). KEEP-AS-IS. -->
<!-- Test bodies (WorkShift, GetTargetWorkersCount, DedicatedWorkerSettings, WorkTypeAssignmentRuleComparer, IsInitialized): real FluentAssertions, no speculative scaffolding, no dead "in case" tests, no interface/factory/generic/plugin abstractions. KEEP-AS-IS. -->
<!-- No new config flag, layer, interface, or dependency introduced by the diff. No speculative future-proofing. -->

## Verdict
CONCERNS: 1

## Next action
Route back to `impl` (fix mode): the Test Engineer inlines the two `typeof` wrappers in `StateIsolationTestBase` and removes the restating doc comments, then sprint re-enters impl-review. No user escalation required — the simpler alternative is direct inlining and adds no abstraction, layer, or dependency.

## Escalations (optional)
- none
