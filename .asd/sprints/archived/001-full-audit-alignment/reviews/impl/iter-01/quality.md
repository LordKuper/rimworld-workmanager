---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-quality]: CONCERNS

# Review — quality

- **Phase**: impl-review
- **Iteration**: 1

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | `Source/WorkManager.Tests/PassionHelperTests.cs:6,11`; `DedicatedWorkerSettingsTests.cs:8`; `WorkTypeAssignmentRuleComparerTests.cs:8`; `GetTargetWorkersCountTests.cs:8`; `WorkShiftTests.cs:8` | Test doc comments reference ASD design artifacts — `(AC-2)`..`(AC-6)` and `manual-steps.md MS-?`. Violates the project rule "Self-contained code — no design-doc references" (custom-coding-rules §Self-contained; code-style §8). These references rot once the docs move and couple test code to the ASD layout. | Remove the `AC-N` / `manual-steps.md` citations; replace with a brief standalone rationale describing the behavior being verified (e.g. "covers Combine merge precedence" instead of "(AC-3)"). |
| 2 | low | `Source/WorkManager.Tests/GetTargetWorkersCountTests.cs:216-220` | `GetTargetWorkersCount_PawnCountMode_RequiresLiveGameContext` asserts `act.Should().Throw<Exception>()` — the broadest possible exception type. It would pass on any unrelated failure (e.g. a future signature change throwing something else), masking intent rather than verifying the null-Map boundary. | Narrow to the specific expected type (e.g. `Throw<NullReferenceException>()` or the actual thrown type), or assert the message, so the test fails if the failure mode changes. |
| 3 | low | `Source/WorkManager.Tests/StateIsolationTestBase.cs:74-78,83-89` | Reflection snapshot/restore silently no-ops when a target field/property setter is not found (`GetField`/`GetSetMethod` returns null → no-op). The fields (`_defaultRule`, `_defaultRulesByName`, `Instance` setter) exist today so isolation works now, but a future rename would silently disable state restoration, letting cross-test contamination pass undetected instead of failing loudly. | Throw (e.g. `InvalidOperationException`) when an expected field/setter is missing, so a rename surfaces as a test-infra failure rather than silent loss of isolation. |

## Verdict
CONCERNS: 3

## Next action
Route sprint back to `impl` (fix mode). The Test Engineer resolves findings #1–#3 (all in the test project). No escalation required — all are autofixable within scope. Sprint then re-enters impl-review.

## Notes (special-attention items — verified clean, no findings)

- **ADR-0001 null contract / prefix semantics**: All UI Harmony patches are `void` postfix/prefix methods (`WidgetsWorkPatch.DrawWorkBoxForPrefix/Postfix`, `WorkTabPatch.*`, `MainTabWindowWorkPatch.Postfix`, `PawnColumnWorkerWorkPriorityPatch.*`). A `void` prefix never skips the original method, so the `if (!IsInitialized) return;` early-out suppresses only WorkManager's *added* behavior and leaves vanilla/WorkTab UI fully intact — behavior-preserving. The "prefix returning `false` skips the original" risk does not apply (no patch returns `bool`). `GetMinHeaderHeightPostfix` (both copies) is intentionally unguarded: it only mutates `__result` (`+= 30`) with no `Instance` dereference, so it is safe.
- **UI entry-point guards**: `AutoWorkPriorities.DoCell` / `AutoWorkSchedule.DoCell` and every patch entry point early-out on `!IsInitialized` before any `Instance` access, per the ADR rule "guard at the entry point, not at every leaf".
- **Game-scoped consumers**: `WorkPriorityUpdater`, `ScheduleUpdater`, `PawnCache` keep direct `Instance` deref; they are reached only via `MapComponent` ticks / cache paths (Map ⇒ Game ⇒ Instance non-null), matching the documented contract. `WorkManagerMod.WriteSettings` (game-less path) correctly uses `?.`.
- **Localization**: `"WorkManager.Settings_Schedule_WorkShiftLabel".Translate(i + 1)` passes a single positional arg matching the `{0}` placeholder in all three locale files (English/Russian/ChineseSimplified). The `i + 1` is correct 1-based numbering — the loops start at `i = 1` (the first, non-deletable shift is skipped), so labels read #2, #3, … No off-by-one, no arg/placeholder mismatch.
