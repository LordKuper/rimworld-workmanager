[REVIEW-impl-quality]: APPROVE

# Review — quality

- **Phase**: impl-review
- **Iteration**: 4 (severity floor: high)

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Scope checked

- `Source/WorkManager/Settings/NeedLimit.cs`: persists only the def name and `Threshold` (default 0.5). `new ExposeData` plus the re-declared `IExposable` sends Scribe calls to the derived method.
- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs`:
  - `FilterNeeds` and `NeedLimits` are scribed. `ValidateNeedsFilter` runs on `LoadingVars` and inside `Validate` on save. It null-initialises the list, drops null, empty and case-insensitive duplicate entries, clamps thresholds, maps NaN to the default and sets the default rule to off.
  - `Combine` takes state and entries together from one source and copies the list, as ADR-0006 requires.
  - `IsNeedBlocked` blocks only on `true`, skips unresolved defs and needs the pawn lacks, and compares with strict `<`.
  - `IsAllowedWorker` ANDs the needs block, so every allowed-worker consumer in `WorkPriorityUpdater`, including the schedule-aware fail-safe that draws from `allowedWorkers`, respects it.
- `Source/WorkManager/Settings/Settings_WorkTypes.cs:331-397` (`DoRuleNeeds`):
  - The default rule gets a two-state checkbox and work-type rules get a tri-state one.
  - Removal is deferred until after the loop, so the list is not changed during iteration.
  - The Add button is shown only when `HasAddableNeeds` is true. After 7fa925a, the click handler relies on that render-time gate. Nothing can change the list between the render and the click, so removing the guard is safe. Even if a stale menu added a duplicate, `ValidateNeedsFilter` would remove it on save.
- `Source/WorkManager/WorkPriorityUpdater.cs:280-291`: the day-level fail-safe excludes need-blocked pawns through `rule.IsNeedBlocked(pc.Pawn)` on the combined rule and keeps the existing AllowedWorkers bypass. This matches ADR-0007 (G-3 (a)) and has the required comment. `Update()` is wrapped in try/catch with logging.
- `Source/WorkManager.Tests/NeedsFilterTests.cs` covers these core paths: validation, defaults, `CreateRule`, `Combine` precedence and copy semantics, `IsNeedBlocked` boundaries (0, the threshold, 1, unresolved entries, missing needs, any-of) and the addable-needs filtering and ordering.

## Verdict
APPROVE

## Next action
Reviewer done. No action required from the creator.
