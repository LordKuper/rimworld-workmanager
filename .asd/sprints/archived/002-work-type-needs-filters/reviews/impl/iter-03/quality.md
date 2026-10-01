[REVIEW-impl-quality]: APPROVE

# Review — quality

- **Phase**: impl-review
- **Iteration**: 3 (severity floor: high; only high/critical counted)

## Scope reviewed

- `Source/WorkManager/Settings/Settings_WorkTypes.cs` — `GetAddableNeeds`, `HasAddableNeeds`, `IsAddableNeed`, `DoRuleNeeds` (fix b7077ee)
- `Source/WorkManager/Settings/NeedLimit.cs`
- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs` — `FilterNeeds`/`NeedLimits`, `ExposeData`, `Combine`, `CreateRule`, `IsNeedBlocked`, `ValidateNeedsFilter`, `Description`
- `Source/WorkManager/WorkPriorityUpdater.cs` — every `IsAllowedWorker` call site plus the day-level fail-safe (`AssignDedicatedWorkersForDay`, line 286) and the schedule-aware fallback (`TryAssignDedicatedWorkersBySchedule`, lines 824-828)
- `Source/WorkManager.Tests/NeedsFilterTests.cs`
- Contract context: ADR-0006..0008, PRD AC-15..AC-39

## Checks performed (no qualifying defects)

- **Add button visibility (b7077ee)**: `HasAddableNeeds` and `GetAddableNeeds` use the same `IsAddableNeed` predicate, so the button is shown only when the menu is non-empty. The predicate excludes `playerMechsOnly`, `Authority`, and already-listed needs (case-insensitive). The click handler recomputes the list and guards `Count == 0`. Removals are applied before the visibility check. Content height is cached only on `Layout`, the same pattern as the other sections. This matches AC-30 and D6.
- **Fail-safe (AC-24/25/26)**: the day-level fallback pool (line 286) excludes need-blocked pawns through `rule.IsNeedBlocked`. `rule` is the combined rule from `CombinedRules` (`WorkManagerGameComponent.cs:452`), so an Inherit state resolves correctly. The schedule fallback draws only from `allowedWorkers`, which is already need-filtered through `PawnCache.IsAllowedWorker` → `WorkTypeAssignmentRule.IsAllowedWorker`. All other assignment paths (assign-everyone, leftover, idle, learning rate, passion, skill) gate on `IsAllowedWorker`. When state is Off or there are no entries, `IsNeedBlocked` returns false, so the old behavior is kept exactly (AC-26).
- **Evaluation (AC-15..AC-21)**: comparison is strict `<`. Missing need or missing tracker yields `null`, which passes. Unresolved def is skipped. No cached state survives between updates, because `PawnCache.Update` clears `_allowedWorkTypes`.
- **Persistence**: `NeedLimit` and the rule re-declare `IExposable` with `new ExposeData`, so interface dispatch reaches the derived members. `ValidateNeedsFilter` runs on `LoadingVars` and before saving. It covers null, empty, duplicate (case-insensitive), NaN, and out-of-range values, and old saves where both fields are null.
- **Combine**: copies the source list (`[.. ]`), so later edits to the combined list do not change the settings list. Tests verify this.
- **Summary (AC-39)**: `limit.Label` falls back to the def name through `DefCache.Label` when the def is unresolved.
- **Security**: no trust boundary is involved. Input is local settings XML and is normalized on load.
- **Tests**: validation, defaults, Combine inheritance, IsNeedBlocked boundaries, and the addable-needs filter and ordering (including `HasAddableNeeds` parity cases) are covered.

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Verdict
APPROVE

## Next action
Quality reviewer done for iter-03. No fixes required from the creator.

## Escalations (optional)
- none
