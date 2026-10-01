---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-simplification]: APPROVE

# Review — simplification

- **Phase**: impl-review
- **Iteration**: 2
- **Severity floor**: medium (over-engineering checklist hits are critical and cannot be dropped)

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Checklist walk (for the record: all keep-as-is)

| Checklist item | Where checked | Result |
|---|---|---|
| Interface with exactly one implementer | `FakeDefProvider : IDefProvider` | keep-as-is. `IDefProvider` is Common's upstream interface with a production implementer. The fake is its second implementer, a test seam approved as CA-5. |
| Generic with one concrete type parameter | `NeedLimit : DefCache<NeedDef>` | keep-as-is. `DefCache<T>` is Common's generic, already used with `WorkTypeDef`, `PawnCapacityDef` and others. No new generic was added. |
| Factory for fewer than three classes | — | none added |
| Plugin system with no plugin | — | none |
| Abstraction with no second use case | `IsNeedBlocked(Pawn)` and static `IsNeedBlocked(bool?, IReadOnlyList<NeedLimit>?, Func<NeedDef,float?>)` | keep-as-is. The instance method has two callers (`IsAllowedWorker` and the fail-safe in `WorkPriorityUpdater.cs:286`). The static seam serves production and the unit tests (CA-3). |
| Abstraction with no second use case | `Settings.GetAddableNeeds`, `ValidateNeedsFilter` | keep-as-is. Both have a production caller plus tests. `ValidateNeedsFilter` has two production callers (the load path in `ExposeData` and `Validate()`), approved as CA-4. |
| Premature config flag | `FilterNeeds` (`bool?`) | keep-as-is. This is the user-facing tri-state setting in the approved scope (CA-2), and the UI sets every state. |
| Defensive code for impossible-by-contract case | null checks on `NeedLimits` in `Combine`, `Description`, `DoRuleNeeds`, `IsNeedBlocked`; `limit.DefName != null` in `GetAddableNeeds`; `pawn.needs?` | keep-as-is. `NeedLimits` is `List<NeedLimit>?` because Scribe sets it to null for older settings. `DefCache.DefName` is `string?`. `pawn.needs` can be null for some pawns. None of these cases is impossible by contract. |
| Helper wrapping one stdlib call | — | none |
| Inheritance depth ≥ 3 without dispatch | `NeedLimit` → `DefCache<NeedDef>` → object | depth 2; `sealed` |
| Framework wrapping a framework | — | none |
| Mock of a mock | `NeedsFilterTests` | none. It uses one fake (`FakeDefProvider`) and plain lambdas for level lookup. |
| Comment that restates code | `WorkPriorityUpdater.cs:284-285`; `WorkManager.Tests.csproj:45-49`; XML docs on new members | keep-as-is. The comments explain *why* (the fail-safe skips the allowed-workers filter but never the needs block; why Assembly-CSharp must be copied for discovery). |
| Dead code "in case" | `NeedLimit(string defName)` constructor | keep-as-is. Tests use it to build unresolved or removed-mod entries, and it is listed in the approved ADR-0006 decision. `FakeDefProvider.WorkTypeDefsInPriorityOrder` returns `[]` because the interface requires it. |
| Build target | `CopyRimWorldTestDeps` in `WorkManager.Tests.csproj` | keep-as-is. The user approved it (ADR-0002 amendment 2026-09-29). It is one `Copy` task with no extra layer. |

Complexity-vs-value: the feature adds one sealed entry class, two fields, one validation method, one evaluation seam and one menu-building function, all at the minimum the approved scope needs. It adds no new dependency, config flag or layer beyond CA-1..CA-5.

## Verdict
APPROVE

## Next action
Simplification reviewer is done for this iteration. No creator action required.

## Escalations (optional)
- none
