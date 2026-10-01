---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-performance]: APPROVE

# Review — performance

- **Phase**: impl-review
- **Iteration**: 3

Note: `.asd/project/custom-coding-rules.md` defines no performance budgets (sections: Nullability, Zero warnings, Build / lint flow, Analyzer suppressions, Self-contained code, Logging, Testing). There are no budgets to enforce. The review is limited to anti-pattern and complexity heuristics. The iteration severity floor is HIGH, so only high and critical findings are reported.

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Assessment notes (below floor, informational only)

- `Source/WorkManager/Settings/Settings_WorkTypes.cs:303-322,377`: `HasAddableNeeds` is an index-based loop over `AllDefsListForReading<NeedDef>()` with an inner linear scan over existing limits. It does not allocate (no LINQ, no closures, no boxing). At about 10–40 NeedDefs and a few entries, that is a few hundred ordinal string comparisons per OnGUI event, which costs microseconds. It exits early on the first addable need, which is the common case. It runs only while the settings window is open and the needs filter is on. This is acceptable. The sorted list (`GetAddableNeeds`, LINQ + `OrderBy`) is built only inside the click callback (`:381-393`), which is correct.
- `Settings_WorkTypes.cs:370-374`: each OnGUI event allocates one `IconButton[]` and one closure per need row. This matches the existing per-row UI pattern, the row count is small and it only happens while the settings window is open. Not a hot path.
- `WorkTypeAssignmentRule.cs:290-424` (Description): the sprint appends a loop over `NeedLimits` (a few interpolated strings). This does not make the accepted, pre-existing per-frame rebuild materially worse. Not re-raised.
- `WorkTypeAssignmentRule.cs:609-613`: `IsNeedBlocked(Pawn)` allocates one closure per call, and `TryGetNeed` scans the pawn's needs list linearly. The result is memoised through `PawnCache.IsAllowedWorker` (`Cache/PawnCache.cs:143-154`, cleared per update at `:250`), so the cost is bounded to pawns × work types once per update cycle.
- `WorkPriorityUpdater.cs:282-288`: the dedicated-worker fallback pool calls `rule.IsNeedBlocked` without the cache, once per capable pawn. This only runs when the target count is not yet met. It is linear in pawns and each call is cheap. Negligible.
- `WorkTypeAssignmentRule.cs:478`: `Combine` copies `NeedLimits`. It is only called from `WorkManagerGameComponent.UpdateCombinedRules` (settings change / init), not per tick.
- `WorkPriorityUpdater.cs:852-879`: the fail-safe `try/catch` around `Update()` adds no cost on the normal path.

## Verdict
APPROVE

## Next action
None from performance. Proceed with the other reviewers' verdicts.
