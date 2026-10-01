[REVIEW-impl-testing]: APPROVE

# Review — Testing

- **Phase**: impl-review
- **Iteration**: 3

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Verdict

APPROVE

All AC-48 unit test requirements are met. The five new `HasAddableNeeds_*` tests (iter-02 → iter-03) join comprehensive coverage of AC-2, AC-3, AC-4, AC-6, AC-7, AC-8, AC-10, AC-13–AC-19, and the `GetAddableNeeds` logic for AC-30. Tests are deterministic, cover edge cases (empty, single, many, boundary, invalid, special cases), and assert observable behavior. `FakeDefProvider` and `WorkManager.Tests.csproj` infrastructure are sound. Manual verification MS-1–MS-5 all passed (2026-09-29) and confirm in-game evaluation, persistence, fail-safe blocking, UI, and localization work as specified.

### Test coverage summary

**AC-48 requirements (all covered):**
- AC-2 (threshold clamping): 3 tests validating 0–1 range, NaN reset, in-range pass-through
- AC-3 (duplicate removal): 2 tests including case-insensitive deduplication
- AC-4 (default rule normalization): 3 tests covering null→Off and explicit state preservation
- AC-6 (no shipped entries/On): 3 tests confirming all default rules have empty lists and FilterNeeds≠true
- AC-7 (inheritance): 4 tests verifying inherit takes fallback state and entries (with copy semantics)
- AC-8 (On/Off independence): 3 tests showing On/Off ignore fallback, On doesn't inherit
- AC-10 (old settings load): 3 tests ensuring null/missing fields normalize safely
- AC-13–AC-19 evaluation: 12 `IsNeedBlocked` tests covering strict-below-threshold, boundary conditions (0%, 50%, 100%), missing needs, missing pawns, unresolved entries, all-pass conditions
- AC-30 (Add menu): 13 `GetAddableNeeds` tests plus 5 new `HasAddableNeeds` tests validating filtering (playerMechsOnly, Authority, already-added, showOnNeedList=false), sorting (listPriority desc then label), and per-frame efficiency

**Edge cases covered:**
- Empty: no needs, no defs, no eligible (only excluded) remain
- Single / many entries: parametrized and multi-entry tests
- Boundary values: 0%, just-below (0.49/0.5), exactly-at (0.5), just-above (0.51), full (1.0/0.99)
- Invalid: unresolved defs (kept, skipped at eval), missing pawn needs (pass), null lists (handled safely)
- Special: Authority and playerMechsOnly excluded; showOnNeedList=false included; case-insensitive deduplication

**Determinism & quality:**
- No sleep-based timing, network calls, or randomness
- All needs mocked via `FakeDefProvider` (isolated, repeatable)
- FluentAssertions for readable assertions
- Parametrized tests used effectively
- Proper Setup/TearDown for def provider lifecycle
- No test-for-test-sake: each test asserts a requirement or edge case

**Infrastructure:**
- `WorkManager.Tests.csproj` target `CopyRimWorldTestDeps` correctly copies `Assembly-CSharp.dll` so `FakeDefProvider` (with `Def` generic constraint) loads at test discovery
- `FakeDefProvider` is minimal, clean, implements `IDefProvider` contract precisely
- No TODO(sprint-002-*) markers left in code

**Manual verification (all passed 2026-09-29):**
- **MS-1** (Persistence): Settings survive save/load unchanged; defs resolved on first update after load (not on load path); unresolved entries shown unavailable, removable; no log errors
- **MS-2** (Live evaluation): Needs filter re-checks every update; pawns recover when need recovers; pawns lacking listed need unaffected (pass)
- **MS-3** (Fail-safe): EnsureWorkerAssigned + MinWorkerNumber never assign need-blocked pawns (≥ 0 assigned if all blocked); pre-sprint behavior (needs filter Off) identical
- **MS-4** (UI): Needs section present, state controls correct (2-state default / 3-state work-type), Add button shown only when addable, menu sorted and filtered, entry rows editable, empty state shown, rule summary includes Needs part, reworded tooltips mention needs block, changes apply without restart
- **MS-5** (Localization): Russian and ChineseSimplified UI strings display correctly (not raw keys), all three 1.6 locales have 55 identical Settings.WorkTypes keys (8 new + 3 reworded), no 1.1–1.5 changes

**Test results:**
- 124 passed, 0 failed, 0 skipped ✓

## Next action

Phase complete. Testing reviewer approves.

## Escalations (optional)

None.

## Manual verification (optional, Testing reviewer only)

| # | Requirement (AC-ID) | Steps for user | Result reported by user |
|---|---|---|---|
| 1 | AC-9, AC-10, AC-11, AC-12 (MS-1) | Settings: add Outdoors/Beauty needs to Mining, close, save game, quit, restart, verify Needs section shows Outdoors 60% / Beauty 35% unchanged; hand-edit fake need, restart, verify shown unavailable; delete it, save, restart, verify gone | pass (verified 2026-09-29: persistence and load-before-defs working; unresolved entries handled) |
| 2 | AC-13, AC-16, AC-17, AC-21, AC-22, AC-23 (MS-2) | Outdoors threshold 60% on Mining; force pawn Outdoors < 60%, trigger update, verify Mining priority 0; restore Outdoors ≥ 60%, trigger update, verify Mining restored; test with Undergrounder (no Outdoors need), verify unaffected | pass (verified 2026-09-29: live evaluation and recovery working; missing needs unaffected) |
| 3 | AC-24, AC-25, AC-26 (MS-3) | Mining: EnsureWorkerAssigned On + MinWorkerNumber 1 + Outdoors 60% + 3 miners all blocked; verify none assigned; restore one, verify one assigned; set needs Off, block all again, verify at least one assigned (pre-sprint behavior) | pass (verified 2026-09-29: fail-safe blocks need-blocked pawns absolutely; pre-sprint behavior preserved) |
| 4 | AC-27…AC-41 (MS-4) | Needs section present, state control correct, Add button/menu work, entry rows editable, empty state, rule summary includes Needs, tooltips reworded, changes apply without restart | pass (verified 2026-09-29: settings UI complete; summary correct; tooltips reworded; no restart needed) |
| 5 | AC-42, AC-43, AC-49 (MS-5) | Russian and ChineseSimplified: verify Needs-section strings, tooltips, buttons all localized (not English keys); count Settings.WorkTypes keys in all 1.6 locales (should be 55); spot-check new/reworded keys | pass (verified 2026-09-29: Russian and ChineseSimplified translated; no raw keys; 55 keys in all locales) |
