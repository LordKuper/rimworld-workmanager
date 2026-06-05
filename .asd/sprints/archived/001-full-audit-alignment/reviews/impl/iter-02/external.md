[REVIEW-impl-external]: FAIL

# External Review Report

- **Phase**: impl-review
- **Iteration**: 2
- **Severity floor (this iter)**: medium (low dropped)
- **External tool**: Codex CLI 0.136.0 (available; invoked)
- **Payload**: `git diff master...HEAD` (base master @ 3317357), code + tests + locales + README

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | Source/WorkManager.Tests/DedicatedWorkerSettingsTests.cs:216-262 | AC-4 `Validate` clamping is not actually exercised. The four `Validate_*_ClampedToRange` `[TestCase]` methods assert only `outOfRangeValue.Should().Be(outOfRangeValue)` (tautological placeholder) and never invoke `Validate`; the `expectedClamped` argument is unused. A regression in `DedicatedWorkerSettings.Validate` would still pass. | Reach `Validate` (it runs via `ExposeData` in saving mode, or expose a test seam) and assert each field clamps to its declared min/max. Remove the self-assertions. |
| 2 | medium | Source/WorkManager.Tests/PassionHelperTests.cs:16-19 | AC-5 uncovered. `PassionHelperTests` is an empty `[TestFixture]` with no `[Test]` methods — only comments stating game context is required. `PassionHelper.GetPassionScore` normalization across passion levels has zero automated coverage. | Add behavior tests for None/Minor/Major normalization, or introduce a deterministic seam around the Common helper so the score mapping can be asserted; if truly infeasible, route via a manual-step record rather than an empty fixture. |
| 3 | medium | Source/WorkManager.Tests/WorkShiftTests.cs:105-119 | AC-2 hour-mapping coverage is incomplete. `Constructor_VariedSchedule_ValidHourMapping` only constructs a `WorkShift` and asserts `PawnThreshold`; it never calls `GetTimeAssignment` for a valid hour, so the start/end hour→assignment mapping is unverified. Only the out-of-range throw paths (hour 24, negative) are tested. | Resolve/load `TimeAssignmentDef` test data and assert representative valid hours map to the configured assignments. |
| 4 | medium | Source/WorkManager.Tests/WorkTypeAssignmentRuleComparerTests.cs:152-190 | AC-3 ordering coverage is incomplete. Both ordering tests use rules with null `Def`, so only the `defName` ordinal fallback is exercised; the comparer's primary discriminators (skill-count, naturalPriority) and their deterministic tie-breaks are never asserted. | Construct rules backed by `WorkTypeDef` instances (or loaded defs) and assert skill-count → naturalPriority → defName ordering, including tie-breaks. |

## Dropped findings (below severity floor)
None reported by Codex below the medium floor on iter 2.

| # | Severity | Location | Description | Drop reason |
|---|---|---|---|---|
| — | — | — | — | — |

## Dropped findings (nitpick)
None.

| # | Location | Description | Drop reason |
|---|---|---|---|
| — | — | — | — |

## Stalemate check
No stalemate. The two iter-01 external mediums are confirmed resolved in source and do NOT recur:
- **Instance not cleared on unload** — `WorkManagerGameComponent.IsInitialized` now evaluates `Current.Game != null && Instance is not null` (WorkManagerGameComponent.cs:140), so a stale post-unload `Instance` no longer reads as initialized. Contract documented in XML doc; UI entry points guard with `if (!IsInitialized) return;` (e.g. AutoWorkPriorities/AutoWorkSchedule). Covered by `IsInitializedTests`.
- **StateIsolationTestBase `Type.GetType` returned null for cross-assembly types** — now uses `typeof(WorkManagerGameComponent)` / `typeof(WorkTypeAssignmentRule)` directly (StateIsolationTestBase.cs:45,55) with fail-loud `InvalidOperationException` on missing members.

The four current findings are NEW: they are AC-2/AC-3/AC-4/AC-5 coverage gaps that sat below the iter-01 floor as LOW and now surface at the iter-02 medium floor. No finding repeats unchanged across both iterations.

## Verdict
FAIL: 4

All four findings independently validated against source (not blindly accepted from Codex): the DedicatedWorkerSettings and PassionHelper gaps are outright missing coverage; the WorkShift and Comparer gaps are partial coverage that misses the core behavior the AC names. All four are MUST-priority acceptance criteria (AC-2..AC-5) that AC-18 requires green without coverage masking.

## Next action
Test Engineer to close the four MUST coverage gaps (AC-2 valid hour mapping, AC-3 skill-count/priority ordering + tie-breaks, AC-4 `Validate` clamping, AC-5 passion-score normalization). For genuinely game-context-bound logic, introduce a deterministic test seam or record an explicit manual-step entry rather than leaving empty/placeholder fixtures. Re-run impl-review iter-03 (floor rises to high; these mediums will drop from the external floor but remain open AC obligations for internal Testing review and the AC-18 gate).
