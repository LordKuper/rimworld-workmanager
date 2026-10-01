---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-simplification]: APPROVE

# Review — simplification

- **Phase**: impl-review
- **Iteration**: 4
- **Severity floor**: high (over-engineering checklist hits are critical and undroppable)

## Scope

- `Source/WorkManager/Settings/NeedLimit.cs`
- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs` (needs filter fields, `Combine`, `CreateRule`, `IsNeedBlocked` x2, `ValidateNeedsFilter`, `Description` needs section, `ExposeData`)
- `Source/WorkManager/Settings/Settings_WorkTypes.cs` (`GetAddableNeeds`, `HasAddableNeeds`, `IsAddableNeed`, `DoRuleNeeds`)
- `Source/WorkManager/WorkPriorityUpdater.cs` (needs-filter call sites, dedicated-worker fallback pool)
- `Source/WorkManager.Tests/NeedsFilterTests.cs`, `Source/WorkManager.Tests/FakeDefProvider.cs`
- `Source/WorkManager.Tests/WorkManager.Tests.csproj` `CopyRimWorldTestDeps` target

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Checklist walk (evidence)

| Checklist item | Result |
|---|---|
| Interface with exactly one implementer | No new interface. `FakeDefProvider` implements Common's existing `IDefProvider` (production implementer lives in Common), CA-5 approved. |
| Generic with one concrete type parameter | None added. `NeedLimit : DefCache<NeedDef>` consumes Common's existing generic; it does not define one. |
| Factory for < 3 classes | None. `CreateRule` predates the sprint and only gained two field initialisers. |
| Plugin system with no plugin | None. |
| Abstraction with no second use case | `NeedLimit` (CA-1) is the persisted entry type. The static `IsNeedBlocked` seam (CA-3) has two callers: the instance overload and the tests, and the instance overload is reused by the dedicated-worker fallback pool (`WorkPriorityUpdater.cs:286`). No `NeedsFilter` wrapper class was introduced. |
| Premature config flag | `FilterNeeds` is user-facing and every state (inherit/off/on) is reachable from the UI (CA-2). No hidden flags. |
| Defensive code for impossible-by-contract case | Null handling on `NeedLimits` (`Combine`, `Description`, `IsNeedBlocked`, `DoRuleNeeds`) covers a documented real case: `Scribe_Collections` nulls the list on older settings, and the field is typed nullable. `pawn.needs?.` covers pawns without a needs tracker. None of this is dead. |
| Helper wrapping one stdlib call | `IsAddableNeed` holds the shared exclusion rule plus the duplicate scan used by both `GetAddableNeeds` and `HasAddableNeeds`, so it is not a trivial wrapper. `HasAddableNeeds` is the allocation-free per-frame check (CA-4). |
| Inheritance depth ≥ 3 without dispatch | `NeedLimit` is sealed, with depth 1 over `DefCache<T>`, which mirrors Common's `PawnCapacityLimit`. |
| Framework wrapping a framework | None. |
| Mock of a mock | None. `FakeDefProvider` is a single dictionary-backed fake, and level lookups are plain lambdas. |
| Comment that restates code | None found. The comments at `WorkPriorityUpdater.cs:284-285`, `NeedsFilterTests.cs:236` and the csproj target comment explain *why* the code is written that way. |
| Dead code "in case" | None. `NeedLimit(string)` has no production caller, but the tests use it to build unresolved-def entries (the removed-mod path), which cannot be built through the `NeedDef` overload. `FakeDefProvider.AllDefs` / `WorkTypeDefsInPriorityOrder` are required by the interface. |

Complexity-vs-value: the csproj `CopyRimWorldTestDeps` target is one `Copy` task. It is the minimal fix for the discovery-time load failure and is user-approved (ADR-0002 amendment). Every other addition sits inside approved CA-1..CA-5, and I found no unapproved abstraction, layer, dependency or flag.

## Verdict
APPROVE

## Next action
Simplification reviewer done for iter-04. No creator action required from this reviewer.

## Escalations (optional)
- none
