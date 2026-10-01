---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-performance]: APPROVE

# Review — performance

- **Phase**: impl-review
- **Iteration**: 4

Note: `.asd/project/custom-coding-rules.md` has no performance budgets section (sections are Nullability, Zero warnings, Build / lint flow, Analyzer / linter suppressions, Self-contained code, Logging, Testing), so there are no budgets to enforce. The review checked only for anti-patterns and algorithmic complexity. The iteration 4 severity floor is HIGH.

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

Scope checked (nothing reached the HIGH floor):

- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs:596-637`: `IsAllowedWorker` / `IsNeedBlocked` do a linear scan over a small user-curated `NeedLimits` list. Each call allocates one lambda that captures `pawn`. `PawnCache.IsAllowedWorker` memoises the result per pawn and work type for each update (`Source/WorkManager/Cache/PawnCache.cs:146-154`, cleared at `:250`), and updates are throttled to `WorkPrioritiesUpdateFrequency` days in `MapComponentTick`. The cost is negligible.
- `Source/WorkManager/WorkPriorityUpdater.cs:280-291`: the fail-safe pool calls `rule.IsNeedBlocked(pc.Pawn)` without memoisation, together with the existing `goodWorkers.Contains` (O(P^2) on the colonist count). This runs only when the target is not reached, and P is small. The `Contains` pattern predates the sprint, and the sprint adds only an O(N_limits) term. This is below the floor.
- `Source/WorkManager/WorkPriorityUpdater.cs:852-879`: the try/catch around `Update()` does not change the cost of the hot path.
- `Source/WorkManager/Settings/Settings_WorkTypes.cs:303-322,377`: `HasAddableNeeds` is an index-based loop with no allocation. It is O(N_needDefs x N_limits) per frame, with a small N (about 20 need defs in vanilla). The sorted LINQ list (`GetAddableNeeds`, `:286-294`) is built only inside the click callback (`:381-392`). This is correct.
- `Source/WorkManager/Settings/Settings_WorkTypes.cs:361-375`: each frame allocates one `IconButton[]` and one closure per listed limit. This happens only while the settings window is open and the list is small, so it is below the floor.
- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs:399-421`: the per-frame `Description` rebuild is a follow-up the user already accepted. The sprint adds one short section with O(N_limits) lines, which is not materially worse, so it is not re-raised.

## Verdict
APPROVE

## Next action
No performance action is required. The sprint may proceed.
