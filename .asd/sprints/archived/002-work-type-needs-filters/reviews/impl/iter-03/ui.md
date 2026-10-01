[REVIEW-impl-ui]: APPROVE

# Review — UI

- **Phase**: impl-review
- **Iteration**: 3

## Findings

| — | — | — | no findings | — |

## Verdict

APPROVE

## Next action

All acceptance criteria AC-27..AC-41 and AC-49 (needs-filter UI, rule summary, language strings) are correctly implemented:

- **AC-27**: Needs section positioned next to Allowed workers section (`DoRuleNeeds` called after `DoRuleAllowedWorkers` at line 529).
- **AC-28**: State control offers Off/On for default rule, Inherit/Off/On for work-type rules via `Fields.DoLabeledCheckbox` with boolean and nullable bool respectively.
- **AC-29**: Inherit tooltip uses `WorkTypeRuleUndefinedSettingTooltip` wording via `GetFilterNeedsTooltip(true)`.
- **AC-30**: Add button shown only when `HasAddableNeeds` returns true; menu of `NeedDef` sorted by `listPriority` descending then label, excludes `playerMechsOnly` and `Authority` needs.
- **AC-31**: Each menu option shows `def.GetLabel()` as label and `def.description` as tooltip.
- **AC-32**: New entries created at 50% threshold via `NeedLimit.DefaultThreshold = 0.5f`.
- **AC-33**: Percent slider with 0–100% range (0f–1f) and 1% step (0.01f).
- **AC-34**: Remove button on each entry via `IconButton(TexUI.DismissTex, ...)`.
- **AC-35**: Unresolved entries shown with `string.Format(NeedUnavailableLabel, defName)` and removable.
- **AC-36**: Empty-state text `NeedsEmptyLabel` shown when `limits.Count == 0`.
- **AC-37**: `NeedsTooltip` contains all four required statements (work type disabled while below threshold, overrides ensure/minimum, pawns without need unaffected, fast-changing needs may toggle).
- **AC-38**: Changes propagate via existing `WriteSettings` → `UpdateSettingsCache` → `ForceUpdateAssignments` flow.
- **AC-39**: `Description` property includes Needs section handling Inherit/Off/On states and empty case per AC-39.
- **AC-40**: `AvailablePawnsTooltip` states "The needs filter is not applied to this preview."
- **AC-41**: `EnsureWorkerAssignedOnTooltip` and `MinWorkerNumberTooltip` both state "Pawns blocked by the needs filter are never assigned."
- **AC-42**: New strings keyed `LordKuper.WorkManager.Settings.WorkTypes.*` in `Resources.Strings.Settings.WorkTypes` class.
- **AC-43**: All 8 new keys (NeedsLabel, NeedsTooltip, FilterNeedsLabel, FilterNeedsOnTooltip, FilterNeedsOffTooltip, NeedsEmptyLabel, NeedUnavailableLabel, NeedUnavailableTooltip) and 3 reworded keys present in 1.6 English, Russian, and ChineseSimplified; key sets identical across locales.
- **AC-44**: No keys added to 1.1–1.5 language folders.
- **AC-49**: Russian and ChineseSimplified values are real translations (not English placeholders).

Progression: Previous iter-01 UI APPROVE; iter-02 UI CONCERNS resolved. Impl-review iteration 3 severity floor is HIGH. No HIGH-severity findings detected.

## Escalations

None.
