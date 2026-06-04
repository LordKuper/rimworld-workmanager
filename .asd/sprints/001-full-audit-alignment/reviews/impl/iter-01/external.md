[REVIEW-impl-external]: CONCERNS

# External Review Report

- **Phase**: impl-review
- **Iteration**: 1
- **Severity floor (this iter)**: low (all severities count)
- **External engine**: Codex CLI 0.136.0 (available; review completed)
- **Diff payload**: `git diff master...HEAD` (base master @ 3317357) — 44 files, +1978 / -485
- **Build/test gate (verified by reviewer)**: Release build 0 warnings / 0 errors (AC-17 PASS); test suite 49 passed / 0 failed / 0 skipped (AC-18 PASS)

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | Source/WorkManager/WorkManagerGameComponent.cs:125 (`Instance`/`IsInitialized`) | `Instance` is set in the constructor but never cleared on game unload. After loading a save and returning to the main menu, `IsInitialized` still returns `true`, so game-less UI paths dereference a stale, defunct component. The "play, then quit to menu" path is reachable and not protected by the new guard — AC-12 ("no NRE / graceful degradation on game-less screens") is only partially satisfied. (Reviewer note: a fresh `Game` reconstructs the component and reassigns `Instance`, so the *cold main-menu* path is fine; the gap is specifically the post-game return-to-menu state.) | Clear `Instance` on the appropriate game-unload lifecycle hook, or have `IsInitialized` validate against the current active `Game`/`Current.Game` rather than a stale static. Confirm against the ADR-0001 contract semantics ("null genuinely means no game"). |
| 2 | medium | Source/WorkManager.Tests/StateIsolationTestBase.cs:38 (`GetWorkManagerGameComponentType`) | Static-state isolation never actually snapshots/restores `WorkManagerGameComponent.Instance`. It resolves the type via `Type.GetType("LordKuper.WorkManager.WorkManagerGameComponent")`, which only searches mscorlib + the calling (test) assembly and returns `null` for a type in the referenced production assembly — so snapshot and restore both silently no-op. The same file's `GetWorkTypeAssignmentRuleType()` documents this exact pitfall and correctly uses `typeof(...)`. AC-1's isolation contract ("snapshots relevant static state … restores it") is not honored for `Instance`; latent cross-test bleed risk (currently masked because tests pass). | Use `typeof(WorkManagerGameComponent)` like the rule-type helper, then snapshot/restore the `Instance` property directly. |
| 3 | low | Source/WorkManager.Tests/PassionHelperTests.cs:18 | AC-5 unmet: `PassionHelperTests` is an empty placeholder with zero `[Test]` methods (deferred to manual-steps.md). AC-5 (priority `must`) requires `PassionHelper.GetPassionScore` normalization unit-tested across passion levels. | Add real FluentAssertions tests for each handled passion level / normalized score, or test the pure normalization boundary without a live RimWorld session; if genuinely untestable without game context, escalate the AC-5 scope to the user rather than leaving a silent gap. |
| 4 | low | Source/WorkManager.Tests/DedicatedWorkerSettingsTests.cs:13 | AC-4 partially covered: 11 tests exercise `Combine`, but none call `DedicatedWorkerSettings.Validate` or assert clamping on out-of-range inputs. AC-4 explicitly names both `Combine` AND `Validate` + the clamping behavior — the clamping half is uncovered. | Add `Validate` tests setting each clamped field below/above its bounds and asserting the clamped result. |
| 5 | low | Source/WorkManager.Tests/WorkShiftTests.cs:73 | AC-2 partially covered: constructor validation and out-of-range `GetTimeAssignment` throws are tested, but the valid hour→assignment *mapping* (the start/end hour-to-shift mapping AC-2 names first) is not — the test comment admits it "requires RimWorld defs to be loaded." | Add tests for representative valid hours asserting the configured assignment is returned (with the AssemblyResolve fixture this should be feasible against constructed inputs). |
| 6 | low | Source/WorkManager.Tests/WorkTypeAssignmentRuleComparerTests.cs:179 | AC-3 comparer coverage incomplete: `Compare_OrdersBySkillCountDescending` does not actually exercise skill-count ordering — its own comment (line 185) states both rules fall back to defName ordering because no defs are loaded. The skill-count and `naturalPriority` ordering branches are untested; only the defName tie-break is. | Construct/inject testable `WorkTypeDef` instances (or set `relevantSkills`/`naturalPriority`) so tests assert skill-count-descending, natural-priority, and the final defName tie-break in order. |
| 7 | low | 1.6/Languages/ChineseSimplified/Keyed/WorkManager_Keyed.xml:249 (`Settings_Schedule_NightOwlWorkShiftsTooltip`) | Out-of-scope visible-text change: while adding the new key, the existing Night Owl tooltip's opening quote glyph was altered from U+201C (`"`) to U+201D (`"`), leaving mismatched curly quotes (`"夜猫子"`). Violates the AC-9/AC-10 non-goal "no other visible text or layout changes." (Reviewer-confirmed via byte inspection.) | Restore the original tooltip text byte-for-byte; keep only the new `WorkManager.Settings_Schedule_WorkShiftLabel` addition. |

## Dropped findings (below severity floor)

None — severity floor is `low` on iteration 1; all findings qualify.

## Dropped findings (nitpick)

None — Codex returned no findings in the nitpick drop-list categories (no pure wording polish, opinion-only style, or reformatting-churn flags). The jb-cleanup reformat across ~32 files was correctly treated as in-scope and not flagged.

## Verdict

CONCERNS: 7

All findings are creator-autofixable within sprint scope and do not require Complication Approval: none alters the approved concept, PRD requirements, or an API contract, and none introduces a new abstraction/layer/dependency. They are test-coverage gaps (3–6), a test-infrastructure defect (2), a reliability-contract completeness gap (1), and a scope-violating stray edit (7). Note on AC-5 (finding 3): if the unit genuinely cannot be tested without a live RimWorld session, that is a scope question for the user, not a silent placeholder.

## Next action

Per `review-policy.md` (impl-review autofix), route the sprint back to `impl` (fix mode): the responsible dev resolves findings 1–7, then the sprint re-enters impl-review for iteration 2. On iteration 2 the severity floor rises to `medium`, so findings 1 and 2 remain in scope while the four `low` test-coverage findings (3–6) and the `low` locale fix (7) should be addressed in this round to avoid being dropped below floor next iteration.
