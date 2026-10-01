---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-ui]: APPROVE

# Review — UI

- **Phase**: impl-review
- **Iteration**: 1
- **Reviewer baseline**: PRD AC-27..AC-41, AC-49; ADR-0008 needs section design; LordKuper.Common IMGUI patterns; accessibility (N/A — IMGUI settings UI, no web UI accessibility baseline)

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Verdict

APPROVE

## Next action

None. UI implementation meets all acceptance criteria. User verification (MS-4 in-game UI check, MS-5 locale check) passed on 2026-09-29. Ready for impl-review DoD assessment.

## Rationale

**AC-27 (Needs section shown)**: Settings_WorkTypes.DoRuleNeeds (lines 310–376) renders the section in the rule editor, called immediately after DoRuleAllowedWorkers per the ADR requirement.

**AC-28 (State control: Inherit/Off/On)**: Lines 316–329 render the appropriate checkbox variant: `Fields.DoLabeledCheckbox(ref bool)` for the default rule (Off/On), and `Fields.DoLabeledCheckbox(ref rule.FilterNeeds)` for work-type rules (Inherit/Off/On via tri-state). GetFilterNeedsTooltip(triState) supplies the state-aware tooltip.

**AC-29 (Inherit tooltip)**: Resources.Strings.Settings.WorkTypes.GetFilterNeedsTooltip(triState=true) follows the existing AppendUndefinedSettingTooltip pattern, concatenating the On/Off tooltip with the undefined-setting wording. No new key required.

**AC-30 (Add button & menu)**: GetAddableNeeds method (Settings_WorkTypes.cs:286–301) filters by playerMechsOnly, Authority, and already-listed defs; sorts by listPriority descending then by label; returns defNames in menu order. Button opens FloatMenu at lines 362–371 using Common.Resources.Strings.Actions.Add (generic shared label, per ADR and user decision).

**AC-31 (Option tooltips)**: FloatMenuOption instances at line 365–369 include `tooltip = new TipSignal(def.description)`, as verified per ADR-0008 decision point 2 (verified field).

**AC-32 (50% default)**: NeedLimit constructor at lines 366–367 creates `new NeedLimit(def)`, which defaults Threshold to 0.5f per NeedLimit.cs:17.

**AC-33 (Percent slider, 1% step)**: Fields.DoLabeledPercentSlider call at line 349–353 with step 0.01f and range [0f, 1f].

**AC-34 (Remove button)**: IconButton with TexUI.DismissTex at lines 350–352; deletion via deferred-index pattern (removeIndex >= 0 check at line 355).

**AC-35 (Unresolved entries)**: Lines 344–348 show string.Format(Strings.NeedUnavailableLabel, limit.DefName) for unresolved defs; same row layout as resolved, removable via delete button.

**AC-36 (Empty state)**: Lines 333–337 show NeedsEmptyLabel when limits.Count == 0 and FilterNeeds == true.

**AC-37 (Section tooltip)**: Resources.Strings.Settings.WorkTypes.NeedsTooltip (English XML line 96) states all four required points: work type disabled while need below threshold, overrides ensure/minimum, pawns without the need unaffected, fast-changing needs may toggle on/off.

**AC-38 (Changes effective next update)**: DoRuleNeeds modifies rule state in-place; WriteSettings → UpdateSettingsCache → ForceUpdateAssignments flow applies changes on next priority update without restart (existing architecture).

**AC-39 (Rule summary includes needs)**: WorkTypeAssignmentRule.Description property (lines 399–421) includes a Needs section with coloured header, showing Inherit / Off / On states and entry details per ADR specification.

**AC-40 (Available pawns preview)**: Line 94 (English XML) AvailablePawnsTooltip states "The needs filter is not applied to this preview." List shown unchanged; no AC changes.

**AC-41 (EnsureWorkerAssigned & MinWorkerNumber tooltips updated)**: 
- EnsureWorkerAssignedOnTooltip (XML line 74): "Pawns blocked by the needs filter are never assigned."
- MinWorkerNumberTooltip (XML line 77): "Pawns blocked by the needs filter are never assigned."
- Verified in English, Russian (lines 74, 77), and ChineseSimplified (lines 144, 150).

**AC-42 (New strings keyed correctly)**: All new members in Resources.Strings.Settings.WorkTypes (NeedsLabel, NeedsTooltip, FilterNeedsLabel, FilterNeedsOnTooltip, FilterNeedsOffTooltip, NeedsEmptyLabel, NeedUnavailableLabel, NeedUnavailableTooltip) follow the pattern `LordKuper.WorkManager.Settings.WorkTypes.<Name>`.

**AC-43 (Keys in 1.6 locales)**: English, Russian, and ChineseSimplified WorkManager_Keyed.xml files all contain the new keys at identical offsets within the Settings.WorkTypes block.

**AC-44 (No keys in legacy 1.1–1.5)**: Only 1.6/Languages/ subdirectories were modified; legacy folders remain unchanged.

**AC-49 (Real translations, not English placeholders)**:
- Russian (line 96): "Потребности" (Needs, not English)
- ChineseSimplified (line 188): "需求" (Needs, not English)
- All reworded keys (EnsureWorkerAssigned, MinWorkerNumber, AvailablePawns) translated to Russian and Chinese per ADR-0008 D5.

**Component fidelity & conventions**:
- Slider and delete button follow LordKuper.Common's PawnFilterWidget.DoPawnCapacitiesSection pattern.
- Tri-state checkbox (Inherit/Off/On) matches existing WorkTypeRule fields (EnsureWorkerAssigned, AssignEveryone, DedicatedWorkerSettings.AllowDedicated).
- Section layout (DoLabeledSectionBox, GetTopRowRect, Layout.DoVerticalGap) consistent with Allowed workers and Assignment sections.
- No hardcoded strings; all text via Resources.Strings.Settings.WorkTypes.* with .Translate().
- No raw keys in UI; no English fallback text in locale files.

**Height caching**: _needsSectionContentHeight cached on Layout event (line 374) per IMGUI conventions.

**User verification**: MS-4 (in-game UI check) and MS-5 (locale check) passed on 2026-09-29. All labels, sliders, tooltips, and translations render correctly in-game.
