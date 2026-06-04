---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-simplification]: APPROVE

# Review — simplification

- **Phase**: impl-review
- **Iteration**: 1

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | low | `Source/WorkManager.Tests/StateIsolationTestBase.cs:29-43` vs `:50-53` | Inconsistent reflection strategy for type lookup: `WorkManagerGameComponent` is resolved via string-based `Type.GetType("LordKuper.WorkManager.WorkManagerGameComponent")`, while `WorkTypeAssignmentRule` uses `typeof(...)`. The XML doc on `GetWorkTypeAssignmentRuleType` explicitly warns that `Type.GetType` "only searches mscorlib and the calling assembly and would fail for types in other assemblies" — the same reasoning applies to the component lookup, which silently returns `null` (and the setter then no-ops) if resolution ever fails. Not an over-engineering checklist trip; both types live in the directly-referenced production assembly, so the string path works today. Flagged only as a consistency/robustness nit. | Use `typeof(WorkManagerGameComponent)` directly in `GetWorkManagerGameComponentType`, mirroring the rule-type helper, and drop the null-handling branches that exist only to tolerate the unreliable string lookup. |

<!-- All over-engineering checklist items pass; see Notes below. -->

## Notes (checklist items verified, no findings)

- **`WorkManagerGameComponent.IsInitialized` (ADR-0001)** — `internal static bool IsInitialized => Instance is not null;`. Minimal one-liner that names the lifecycle invariant and is the single guard expression reused across UI entry points. Does NOT trip "helper that wraps one stdlib call without added value": it adds a named, documented contract (`Instance` valid only while a `Game` is loaded) and is the cited guard idiom. Acceptable.
- **Null-guard placement** — every `IsInitialized` guard is at a UI entry point: `Patches/{WorkTabPatch, MainTabWindowWorkPatch, WidgetsWorkPatch, PawnColumnWorkerWorkPriorityPatch}.cs` and `PawnColumnWorkers/{AutoWorkPriorities, AutoWorkSchedule}.cs`. The game-scoped hot paths (`WorkPriorityUpdater`, `ScheduleUpdater`, `PawnCache`) contain NO `Instance` null guard — verified by grep. `PawnCache.cs:15-22` documents the no-guard contract explicitly. No "defensive code for impossible-by-contract case" trip.
- **`StateIsolationTestBase`** — snapshots exactly three genuinely-mutable statics (`WorkManagerGameComponent.Instance`, `WorkTypeAssignmentRule._defaultRule`, `WorkTypeAssignmentRule._defaultRulesByName`), all confirmed to be real mutable static state in production. No snapshotting of fields that do not need it. No mock-of-mock (no mocking framework; tests use real objects / `FormatterServices.GetUninitializedObject`). Proportionate to the static state under isolation.
- **`RimWorldAssemblyResolverFixture`** — single-purpose `AssemblyResolve` handler. RimWorld/Unity assemblies are not NuGet packages, so this is the only mechanism, not a wrapper-over-framework. No abstraction layering. Acceptable.
- **No speculative future-proofing** introduced this sprint: no new interface with one implementer, no generic-with-one-type, no factory <3 classes, no premature config flag (the existing `PriorityManagementEnabled`/`ScheduleManagementEnabled` flags have live UI callers), no plugin system, no dead "in case we need it" code.
- **Cross-reviewer guard**: no proposed fix in scope would add an abstraction, layer, or dependency. The only finding (#1) is a simplification (removes branches), not an addition.

## Verdict
APPROVE

## Next action
Reviewer done. Finding #1 is `low` (at the iteration-1 floor) and a pure `simplify` — the responsible dev MAY address it on the next impl pass, but it does not block DoD and requires no escalation.

## Escalations (optional)
- None.
