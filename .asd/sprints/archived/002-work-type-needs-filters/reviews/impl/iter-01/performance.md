---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-performance]: CONCERNS

# Review — performance

- **Phase**: impl-review
- **Iteration**: 1

Budget note: `.asd/project/custom-coding-rules.md` defines no latency, memory or throughput budgets, so this review checks no budgets. The findings below cover anti-patterns only (per-frame allocation on the UI thread). They are not budget violations.

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | Source/WorkManager/Settings/Settings_WorkTypes.cs:356-357 (calls `GetAddableNeeds`, :286-301) | `DoRuleNeeds` calls `GetAddableNeeds(DefProvider.Current.AllDefsListForReading<NeedDef>(), limits)` on every OnGUI event (Layout, Repaint and input events, so several times per frame) whenever the selected rule has the filter On. Each call allocates a `HashSet<string>`, a LINQ `Where` → `OrderByDescending` → `ThenBy` chain with its sort buffers, and a result `List`. The sort also calls `def.GetLabel()` and a `CurrentCulture` comparer for each comparison, over every NeedDef (dozens with DLCs and mods). The sorted list is only needed when the Add menu opens. Per frame, the result is used only for `addable.Count > 0`. This causes steady GC churn and sorting work on the UI thread for as long as the settings window stays open. | Move the `GetAddableNeeds(...)` call into the `Buttons.DoActionButton` click lambda, so the sorted list is built once per menu open. For the button's visibility, use a cheap non-allocating check instead: a plain `for` loop over `AllDefsListForReading<NeedDef>()` that returns at the first def that is not `playerMechsOnly`, is not `Authority` and has no matching `DefName` in `limits` (a nested loop over the few limits, case-insensitive). Alternatively, always show the button and let the menu handle the empty case. |
| 2 | low | Source/WorkManager/Settings/WorkTypeAssignmentRule.cs:399-421, consumed at Source/WorkManager/Settings/Settings_WorkTypes.cs:500-501 | The new Needs block in `Description` adds more string work to a summary that `DoWorkTypeRule` rebuilds eagerly on every OnGUI event for the header tooltip: a colorized label, plus an interpolated string and a `ToStringPercent()` for each limit. The per-event rebuild existed before this sprint, and the increment is small. Still, the diff adds allocations to a path that runs on every GUI event. | Build the tooltip only on hover: pass a lazily evaluated tooltip, such as a `TipSignal` built from a `Func<string>`, if the Common `Sections.DoSectionHeader` overload accepts one. Otherwise cache the `Description` string for the selected rule and invalidate it when the rule is edited or the selection changes. |

Reviewed and not flagged:
- `IsAllowedWorker` / `IsNeedBlocked` (WorkTypeAssignmentRule.cs:596-637): the check short-circuits after `SatisfiesFilter` and returns early unless the state is On. It does not allocate beyond the level-lookup delegate, which ADR-0006 explicitly accepts, and the result is memoised per update in `PawnCache.IsAllowedWorker` (PawnCache.cs:143-156). Cost is a few `TryGetNeed` scans of about 10 entries for each pawn × work type, once per update. This is negligible at a cadence of 64 ticks gated by the hourly update.
- Fail-safe loop (WorkPriorityUpdater.cs:280-291): the unmemoised `rule.IsNeedBlocked(pc.Pawn)` matches ADR-0007, which rejected memoisation on purpose because the path runs rarely. Its cost per pawn is the same cheap scan.
- `Combine` (WorkTypeAssignmentRule.cs:474-478): a shallow list copy, made only when combined rules are rebuilt after a settings change. It does not run on the tick path.

## Verdict
CONCERNS: 2

## Next action
Backend/Frontend dev (impl fix mode): for finding #1, build the addable-needs list only when the Add button is clicked, and replace the per-frame visibility check with a non-allocating scan. For finding #2, make the header tooltip lazy or cache it per selected rule. Neither fix needs escalation; both are local changes with no new abstraction.

## User decision (2026-09-29)
- **Finding #2 (low, per-frame `Description` rebuild)**: not fixed in this sprint. The per-event rebuild existed before this sprint, and the fix would add caching state. The user chose "Apply all (Recommended)" via AskUserQuestion, relayed by the orchestrator, and that option excluded this item. The finding is resolved by user decision and recorded as a follow-up.
