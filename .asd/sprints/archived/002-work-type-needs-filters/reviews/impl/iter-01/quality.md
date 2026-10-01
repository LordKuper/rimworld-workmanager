---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-quality]: CONCERNS

# Review — quality

- **Phase**: impl-review
- **Iteration**: 1 (severity floor: low, so all findings count)

Scope: production, test and 1.6 localization changes for the needs filter (`NeedLimit.cs`, `WorkTypeAssignmentRule.cs`, `Settings_WorkTypes.cs`, `Resources.cs`, `WorkPriorityUpdater.cs`, `NeedsFilterTests.cs`, `FakeDefProvider.cs`, `WorkManager.Tests.csproj`, the three 1.6 `WorkManager_Keyed.xml` files). Checked against ADR-0006/0007/0008, custom-coding-rules.md and code-style.md.

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | low | Source/WorkManager/Settings/WorkTypeAssignmentRule.cs:649-652 | Input validation gap for the settings file. `ValidateNeedsFilter` sanitizes thresholds with `Mathf.Clamp01`. Unity's `Clamp01` returns `NaN` unchanged, because both `< 0` and `> 1` are false for NaN. RimWorld's float parsing accepts the literal `NaN`, so a hand-edited or corrupted settings file can load `Threshold = NaN`. The entry then breaks the [0, 1] invariant: `level < NaN` is always false, so the limit silently never blocks, and the slider and summary show a NaN percentage. | Replace NaN before clamping, e.g. `limit.Threshold = float.IsNaN(limit.Threshold) ? NeedLimit.DefaultThreshold : Mathf.Clamp01(limit.Threshold);`. Add a `ValidateNeedsFilter_ThresholdNaN_Reset` test case next to the existing clamp tests. |
| 2 | low | Source/WorkManager/Settings/Settings_WorkTypes.cs:360 | The Add button row is sized with `Labels.SectionHeaderHeight`, which is a label metric (`LineHeightOf(Medium) + 4`). Common's own Add buttons use the dedicated `Buttons.ActionButtonHeight` constant (e.g. `PawnFilterWidget.cs:144`, `:568`, `:710`). The Needs Add button therefore does not match the Add buttons of the Allowed workers section directly above it. | Use `Layout.GetTopRowRect(needsRect, Buttons.ActionButtonHeight, out needsRect)` for the Add row. |

Checked with no finding:
- DefCache latch: the load path (`ExposeData` → `ValidateNeedsFilter`) reads only `DefName` and `Threshold`. `.Def` and `.Label` are read only in-game or in the UI.
- Persistence: `NeedLimit` re-lists `IExposable`, so Scribe dispatches to the hiding `ExposeData` that persists `Threshold`. A null `NeedLimits` from older settings is normalized on `LoadingVars`.
- Deferred removal: the `IconButton` action runs synchronously inside the slider call (Common `Buttons.cs:91`), so `removeIndex` is set before the post-loop `RemoveAt`, and the bounds guard is present.
- Fail-safe: `AssignDedicatedWorkersForDay` excludes need-blocked pawns using the combined rule. The schedule-aware path gets the block through `PawnCache.IsAllowedWorker` → `rule.IsAllowedWorker`. Every `SetWorkPriority` path goes through `IsAllowedWorker` or the new exclusion.
- `Combine`: the list is copied into a new instance, and `NeedLimit` entries are shared by design (per ADR-0006).
- Tests: FluentAssertions only, `DefProvider.Current` is saved and restored per test, fixtures are `[NonParallelizable]` and derive from `StateIsolationTestBase`, and nothing depends on execution order. Source and tests contain no ASD references.
- Localization: the 8 new keys and 3 reworded keys are present in all three 1.6 locales. `{0}` is preserved in `NeedUnavailableLabel`.
- Security: no secrets, no command or path construction, no reflection into Common.

## Verdict
CONCERNS: 2

## Next action
Route to `impl` (fix mode). backend-dev fixes #1 (NaN guard and test). frontend-dev fixes #2 (Add row height). Then run impl-review again.

## Escalations (optional)
- none. Both fixes are local, with no contract, abstraction or scope change.
