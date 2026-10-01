---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-simplification]: CONCERNS

# Review — simplification

- **Phase**: impl-review
- **Iteration**: 3
- **Severity floor**: high (over-engineering checklist hits are critical and never dropped)

## Findings

| # | Severity | Category | Location | Description | Suggested fix |
|---|---|---|---|---|---|
| 1 | critical | simplify | `Source/WorkManager/Settings/Settings_WorkTypes.cs:385` (`if (addable.Count == 0) return;` inside the Add-button lambda in `DoRuleNeeds`) | Checklist: **Defensive code for impossible-by-contract case**. The Add button is drawn only when `HasAddableNeeds(DefProvider.Current.AllDefsListForReading<NeedDef>(), limits)` returns `true` (line 377). `LordKuper.Common.UI.Buttons.DoActionButton` invokes the action synchronously inside the same `OnGUI` call (`if (Widgets.ButtonText(...)) action.Invoke();`), with the same `limits` list (the removal at line 376 has already run) and the same immutable def list. `GetAddableNeeds` applies the same `IsAddableNeed` predicate, so an empty result is impossible at that point. The guard is unreachable and implies a race that cannot happen. | Delete line 385. The lambda then reads `var addable = GetAddableNeeds(...); Find.WindowStack.Add(new FloatMenu([...]));`. No behaviour change. |

## Assessed and kept as is (not findings)

- `HasAddableNeeds` + private `IsAddableNeed` (new since iter 2): `IsAddableNeed` has two real callers (`GetAddableNeeds`, `HasAddableNeeds`), so the shared predicate removes duplication rather than adding an abstraction. `HasAddableNeeds` runs every frame to decide button visibility. `GetAddableNeeds` allocates and sorts, so the loop-based check is proportionate and small. It is tested directly. No checklist item applies.
- `NeedLimit : DefCache<NeedDef>, IExposable` (CA-1), `FilterNeeds`/`NeedLimits` fields (CA-2), `IsNeedBlocked` static seam + instance wrapper (CA-3), `ValidateNeedsFilter`/`GetAddableNeeds` internals (CA-4), `FakeDefProvider` (CA-5): all user-approved in ADR-0006, and each is used as approved. `FakeDefProvider` is a single in-memory fake. It is not a mock of a mock. `WorkTypeDefsInPriorityOrder` returns `[]` only because the interface requires it.
- Null/NaN handling in `ValidateNeedsFilter`, `Combine` (`?? []`) and `IsNeedBlocked` (`limits == null`): each covers a real input source (`Scribe_Collections` can yield null lists and entries, and older or hand-edited settings can yield out-of-range values). These cases are not impossible by contract.
- `WorkPriorityUpdater.cs:284-286`: the comment explains *why* the fallback pool still applies the needs filter. It does not restate the code.
- `WorkManager.Tests.csproj` `CopyRimWorldTestDeps` target: user-approved through the ADR-0002 amendment (2026-09-29). It copies a single file and follows the Common precedent. No further simplification is in scope.
- `NeedsFilterTests.cs`: plain constructed inputs and lambdas for need levels. There are no mocking frameworks, no mock-of-mock, and no dead helpers (`CreateCombinablePair` and `RegisterNeed` are both used repeatedly).

## Verdict
CONCERNS: 1

## Next action
Route back to `impl` (fix mode): the responsible dev deletes the unreachable guard at `Settings_WorkTypes.cs:385`. This fix removes code, adds no complexity and needs no escalation.

## Escalations (optional)
- none
