---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-documentation]: CONCERNS

# Review — documentation

- **Phase**: impl-review
- **Iteration**: 2 (severity floor: medium)

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | `design/architecture/adr/adr-0008-needs-ui-summary-and-strings.html` Decision item 1, bullet "Add button" (line 173) vs `Source/WorkManager/Settings/Settings_WorkTypes.cs:356-370` | The persistent ADR is out of date with the code. ADR-0008 says the Add button is "shown only while an addable need exists". The code now draws the Add button every time the needs state is On, and it only calls `GetAddableNeeds(DefProvider.Current.AllDefsListForReading<NeedDef>(), limits)` when the button is clicked. If the list is empty, the click does nothing (`if (addable.Count == 0) return;`) and no menu opens. So a reader of the ADR would expect a hidden button in the "all needs already added" case, but the player actually sees a button that does nothing. The ADR's "Addable needs" bullet is also written as if the list is computed eagerly to decide whether to show the button, which is no longer true. `design/product/requirements.html` NF-AC-30 does not say whether the button is visible, so it still matches the code and needs no change. | The Architect should update ADR-0008 Decision item 1, "Add button" bullet, to say: "shown whenever the needs state is On; on click it builds the addable list (`GetAddableNeeds`) and opens a vanilla `FloatMenu`; if nothing is addable the click is a no-op". Leave the `GetAddableNeeds` filter and sort rules as they are, since they match the code and `NeedsFilterTests`. Bump the `updated` meta/chip only if the date changes. |

Checked and found consistent, with no finding at or above the floor:
- ADR-0006 matches `NeedLimit.cs` and `WorkTypeAssignmentRule.cs`: fields, `ExposeData` plus the `LoadingVars` validation, `ValidateNeedsFilter`, `CreateRule` and `DefaultRules` defaults, the `Combine` shallow copy, `IsAllowedWorker`, and both `IsNeedBlocked` overloads with the strict `<` comparison. The test approach matches `NeedsFilterTests` and `FakeDefProvider`.
- ADR-0007 matches `WorkPriorityUpdater.cs:284-287`: the fail-safe excludes need-blocked pawns only, and the comment is present.
- The ADR-0002 amendment matches the `CopyRimWorldTestDeps` target in `WorkManager.Tests.csproj`: it copies `Assembly-CSharp.dll` only and keeps `Private=False`. The fact has a single home in the NUnit-4.6.1 tech-reference, Known issues section. RimWorld-1.6.md links to that section instead of copying it.
- ADR-0008 strings match the code. Each 1.6 locale (en/ru/zh-Hans) has 55 `Settings.WorkTypes.*` keys. The 8 new keys are present and translated into ru/zh-Hans, and the 3 reworded tooltips are in place. The rule-summary Needs part matches `Description`, and `DefCache.Label` falls back to the defName for unresolved entries, as the Common tech-reference states.
- The tech-reference facts used by the code (`FloatMenuOption.tooltip` of type `TipSignal?`, `TexUI.DismissTex`, `ToStringPercent`, the Common public UI API, and the ADR-0006 exception to the eligibility convention) are present and accurate.
- The requirements.html NF-AC ↔ ADR-0006/0007/0008 ↔ code/test traceability is complete for the sprint ACs.

## Verdict
CONCERNS: 1

## Next action
Route back to impl (fix mode). The Architect updates the "Add button" bullet in ADR-0008 so it describes the always-visible button with the list built on click. No code change is needed.

## Escalations (optional)
- none
