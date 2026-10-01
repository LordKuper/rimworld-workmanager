[REVIEW-impl-ui]: CONCERNS

## Findings

| # | AC | Severity | Finding | File:Line |
|---|----|---------:|---------|-----------|
| 1 | AC-30 | MEDIUM | Add button shown unconditionally; should be hidden when all needs are already added. When no addable needs exist, button click does nothing and returns silently, creating poor UX. ADR-0008 line 173 and Common's `PawnFilterWidget.DoPawnCapacitiesSection` (line 143) both show the button conditionally only when addable items exist. | Settings_WorkTypes.cs:356–370 |
| 2 | AC-42, AC-43, AC-49 | PASS | All 8 new keys present in Resources.cs (lines 470–507) and all three 1.6 locales (English lines 95–102, Russian lines 95–100, ChineseSimplified lines 186–200). Russian and ChineseSimplified values are real translations, not English placeholders. | Resources.cs:470–507; English:95–102; Russian:95–100; ChineseSimplified:186–200 |
| 3 | AC-40, AC-41 | PASS | Three existing tooltips reworded with needs-filter text in all three 1.6 locales: EnsureWorkerAssignedOnTooltip (English line 74, Russian line 74, Chinese line 144), MinWorkerNumberTooltip (English line 77, Russian line 77, Chinese line 150), AvailablePawnsTooltip (English line 94, Russian line 94, Chinese line 184). | English:74,77,94; Russian:74,77,94; Chinese:144,150,184 |
| 4 | AC-27, AC-28, AC-29, AC-33, AC-34, AC-35, AC-36, AC-37, AC-38 | PASS | Needs section layout, state control, entry rows (resolved/unresolved), empty-state text, tooltips, and removal button all implemented per ADR-0008 decision (§ line 173, lines 162–174). Slider at 0.01f step (1% as required). Thresholds persist through save/load. Changes take effect on next update without restarting. | Settings_WorkTypes.cs:310–374; WorkTypeAssignmentRule.cs:399–421 |
| 5 | AC-32 | PASS | New need added with 50% threshold via `new NeedLimit(def)` and common default. | Settings_WorkTypes.cs:365 |
| 6 | AC-31 | PASS | FloatMenuOption has tooltip set to `new TipSignal(def.description)`. | Settings_WorkTypes.cs:367 |
| 7 | AC-39 | PASS | Rule summary (Description property) includes Needs part with Inherit/Off/On branches, shows entries as "label: threshold%", uses defName for unresolved entries, and shows empty-state text when On with no entries. | WorkTypeAssignmentRule.cs:399–421 |

## Verdict

**CONCERNS**: Impl passes AC-42/43/49 (localization complete) and most AC-27..41 (UI layout and behavior). However, **AC-30 violation**: Add button always visible and clicking it does nothing when all needs are already added, instead of being hidden until an addable need exists (as specified in ADR-0008:173 and demonstrated by Common's capacity-limits precedent). This creates poor UX — user receives no feedback on why the button press has no effect. Fix: conditionally show button only when `GetAddableNeeds()` returns non-empty list.

## Next Action

Fix AC-30 violation: add guard before button creation (Settings_WorkTypes.cs line 356) to return early if no addable needs exist, matching ADR-0008 intent and Common precedent. Retest to ensure button hides/shows correctly as needs are added/removed.

## Escalations

None. Finding is implementation-level, not architectural.
