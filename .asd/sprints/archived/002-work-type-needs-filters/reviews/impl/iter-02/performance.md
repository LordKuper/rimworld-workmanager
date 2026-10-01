---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-performance]: APPROVE

# Review — performance

- **Phase**: impl-review
- **Iteration**: 2

Note: `.asd/project/custom-coding-rules.md` defines no performance budgets (latency, memory, throughput, regression tolerance), so there are no budgets to enforce. The anti-pattern and complexity scan below was run at the iteration-2 severity floor (MEDIUM and above only).

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

Scope checked (nothing reached the MEDIUM floor):

- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs:596-637`: `IsAllowedWorker` / `IsNeedBlocked`. The need check is O(limits x pawn needs), and both are small (a few entries, about 20 needs at most). `NeedLimit.Def` resolves through `DefCache`. The result is memoised per update in `PawnCache.IsAllowedWorker` (`Source/WorkManager/Cache/PawnCache.cs:143-156`). The capturing lambda allocates one delegate per uncached call, which is negligible at update frequency.
- `Source/WorkManager/WorkPriorityUpdater.cs:280-291`: the fail-safe loop calls `rule.IsNeedBlocked(pc.Pawn)` without the PawnCache memo, once per capable pawn per under-filled dedicated work type, and only when the target was not met. The cost is linear and cheap. The existing `goodWorkers.Contains` (List, O(n)) makes the loop O(n^2) in colony size. That predates this sprint, and at realistic colony sizes it is below the floor.
- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs:469-490`: `Combine` copies `NeedLimits` into a new list. This runs when combined rules are rebuilt, not per pawn, so it is not a hot path.
- `Source/WorkManager/Settings/Settings_WorkTypes.cs:310-374`: `DoRuleNeeds` runs per OnGUI event. Each limit row allocates one `IconButton[]` and one closure, which is bounded by the few configured limits. `GetAddableNeeds` (LINQ filter + sort over all NeedDefs) now runs only inside the Add button click handler, so no per-frame def scan remains.
- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs:399-421`: `Description` gains a needs section that appends one line per limit. This is a small constant addition to the known, user-accepted per-event rebuild follow-up, and it does not make that follow-up materially worse. Not re-raised.

## Verdict
APPROVE

## Next action
None from the performance side. Optional, outside this sprint: if performance budgets are wanted for later reviews, add a budgets section to `.asd/project/custom-coding-rules.md`.
