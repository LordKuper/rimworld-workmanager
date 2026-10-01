[REVIEW-impl-ui]: CONCERNS

# Review — UI

- **Phase**: impl-review
- **Iteration**: 4

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs:418` | Rule summary displays unresolved need entries without handling null Label. AC-39 requires using defName for unresolved entries. When a need's DLC/mod is removed, `limit.Label` becomes null (inherited from DefCache base class), resulting in display string like "null: 50%" instead of "NeedDefName: 50%". The DoRuleNeeds UI code (line 367) correctly uses `limit.Label ?? limit.DefName` pattern, but Description property omits the null coalesce. | Change line 418 from `$"{limit.Label}: {limit.Threshold.ToStringPercent()}"` to `$"{limit.Label ?? limit.DefName}: {limit.Threshold.ToStringPercent()}"` to match AC-39 requirement and DoRuleNeeds pattern. |

## Verdict
CONCERNS: 1

## Next action
Creator autofixes the null-coalesce issue in Description property (line 418). No escalation required. Re-enter impl-review iteration 5.

## Escalations (optional)
— none

---

## Summary of AC Compliance

**Passing (AC-27 through AC-41, AC-49):**
- **AC-27**: Needs section shown after Allowed workers section ✓
- **AC-28**: State control correctly offers Inherit/Off/On for work-type rules, Off/On for default rule ✓
- **AC-29**: Inherit tooltip uses existing undefined-setting wording via GetFilterNeedsTooltip(true) ✓
- **AC-30**: Add button, NeedDef menu, correct sorting by listPriority then label ✓
- **AC-31**: Add menu options show need label and description tooltip ✓
- **AC-32**: New entries default to 50% threshold (NeedLimit.DefaultThreshold = 0.5f) ✓
- **AC-33**: Percent slider with 1% step (0.01f) ✓
- **AC-34**: Remove button per entry via deferred-index pattern ✓
- **AC-35**: Unresolved entries shown with "(unavailable)" marker and removable ✓
- **AC-36**: Empty-state label when FilterNeeds==true with no entries ✓
- **AC-37**: Section tooltip contains all four required statements ✓
- **AC-38**: Changes take effect via existing WriteSettings flow ✓
- **AC-39**: Rule summary Needs part included (Inherit/Off/On handling correct; defName handling for unresolved entries needs fix per finding #1) ⚠
- **AC-40**: Available pawns preview unchanged; tooltip reworded to state needs filter not applied ✓
- **AC-41**: EnsureWorkerAssigned and MinWorkerNumber tooltips mention needs filter in all three 1.6 locales ✓
- **AC-42 / AC-43**: All 8 new keys + 3 reworded keys present in English, Russian, ChineseSimplified with identical key sets ✓
- **AC-49**: Russian and ChineseSimplified values are real translations (not English placeholders) ✓

**Localization verified:**
- English (1.6/Languages/English/Keyed/WorkManager_Keyed.xml): Lines 74, 77, 94–102 ✓
- Russian (1.6/Languages/Russian/Keyed/WorkManager_Keyed.xml): Lines 74, 77, 94–102 ✓
- ChineseSimplified (1.6/Languages/ChineseSimplified/Keyed/WorkManager_Keyed.xml): Lines 144, 150, 184–200 ✓

**Code patterns verified:**
- Tri-state inheritance (Combine method): Correctly copies entries when inheriting ✓
- Persistence validation: Duplicate removal, threshold clamping, default rule normalization ✓
- Unresolved entry handling in UI (DoRuleNeeds line 367): Correctly uses null-coalesce pattern ✓
- DefaultRules: No needs entries or On state (AC-6) ✓

## Orchestrator note

- **Below the severity floor**: finding #1 is `medium`. Iteration 4 applies the `high` floor (`iterations_medium: 1`, `iterations_high: 2` per `review-policy.md`), so the finding does not count toward the gate. The UI verdict is recorded in `state.json` as `APPROVE (below-floor finding dropped)`.
- **False positive**: `NeedLimit` derives from Common's `DefCache<NeedDef>`, where `Label => Def == null ? _defName : Def.GetLabel()`. The orchestrator checked this in both the Common clone source and the decompiled workshop DLL. An unresolved entry's `Label` is therefore its defName, never null, so the rule summary already shows the defName as AC-39 requires. No code change is needed.
