[REVIEW-impl-implementation]: CONCERNS

# Implementation Review — Sprint 001-full-audit-alignment, Iteration 3

## Summary

Review of acceptance criteria AC-1 through AC-18 against code/test implementation. Scope: pure-logic unit tests, Instance null-handling contract, localization, Harmony package removal, documentation alignment, and build quality gates. User-verified manual steps: MS-2 (passion score) PASS, MS-3 (hour-to-assignment mapping) PASS. MS-1 (game-less UI no-NRE) BLOCKED pending.

**Test suite**: 64 tests across 8 files. Three subtasks incomplete in plan (line 69–72, 80); verdict deferred to iteration 4 pending test completion and manual AC-12.

---

## Findings — HIGH+ Severity Only (Iteration 3 Floor)

| Severity | AC | File:Line | Description | Fix |
|---|---|---|---|---|
| **CRITICAL** | AC-1 | `Source/WorkManager.Tests/PassionHelperTests.cs:42` | `Assert.Pass()` call violates AC-1 requirement: "no `Assert.*` calls remain in the test project." The test must use FluentAssertions `.Should()` or be replaced with a meaningful unit test. Current form is a placeholder that masks missing AC-5 coverage. | Replace `Assert.Pass(...)` with either: (a) a real assertion using `.Should()` if unit testing is feasible without game context, or (b) remove test and rely solely on manual-steps.md MS-2 for AC-5 coverage. Plan line 68 says "test normalization across all handled passion levels, asserting the expected normalized score for each" — this placeholder does neither. |
| **HIGH** | AC-6 | `Source/WorkManager.Tests/GetTargetWorkersCountTests.cs` (present) | Test file exists with comprehensive coverage of non-PawnCount modes (Constant, WorkTypeCount, CapablePawnRatio), but plan.md line 69 remains unchecked (`- [ ]`), blocking confirmation that AC-6 task is complete. File contains 20+ parameterized tests asserting correct calculation per mode. No code gap; planning/tracking gap only. | Mark plan line 69 checked; verify line-coverage floor via `dotnet test` or coverage tool and check line 71. |
| **HIGH** | AC-18, AC-2, AC-5, AC-6 | Plan lines 71–72 | Plan.md subtasks remain unchecked: line 71 "Verify the covered units meet the 80% line-coverage floor" and line 72 "Run `dotnet test Source/WorkManager.slnx` and confirm green." Neither test execution output nor coverage report provided. AC-18 DoD requires "no skipped/ignored tests mask missing coverage"; **cannot verify without test run**. PassionHelperTests placeholder masks AC-5; GetTargetWorkersCountTests exists but untested. | Execute: `dotnet test Source/WorkManager.slnx` and report output; if all 64 tests pass green, proceed to coverage analysis (`dotnet test --collect:"XPlat Code Coverage"` or JetBrains coverage tool) and confirm AC-2…AC-6 units meet ≥80% floor. Mark lines 71–72 checked. |
| **HIGH** | AC-12 | Plan line 80 | Subtask marked `[ ] — BLOCKED: MS-1`. MS-1 status is "pending" per manual-steps.md line 25. AC-12 cannot be verified until MS-1 is performed (in-game test: "no NRE on game-less UI screens"). Code-level guard audit complete (all UI entry points checked, see below); **runtime verification required**. | After build succeeds and code review passes: user performs MS-1 steps (launch RimWorld, open settings/tabs at main menu, check logs for NRE), then mark line 80 checked. AC-12 is code-dependent (guards are in place) but runtime-dependent (verification is manual). |

---

## AC-to-Code Trace (Complete Cases)

### AC-1: Test Infrastructure
- **Status**: IMPLEMENTED with CRITICAL violation (see HIGH findings, PassionHelperTests)
- **Trace**:
  - Global `[SetUpFixture]`: `Source/WorkManager.Tests/RimWorldAssemblyResolverFixture.cs` ✓ (lines 11–50)
    - `[OneTimeSetUp]` registers `AppDomain.CurrentDomain.AssemblyResolve` handler ✓
    - Resolves `Assembly-CSharp` and Unity modules from `$(RimWorldManagedDir)` ✓
  - State isolation base: `Source/WorkManager.Tests/StateIsolationTestBase.cs` ✓ (lines 10–115)
    - `[SetUp]` snapshots `WorkManagerGameComponent.Instance`, `WorkTypeAssignmentRule._defaultRule`, `_defaultRulesByName` ✓
    - `[TearDown]` restores state ✓
    - Enumerates snapshots in XML doc ✓
  - FluentAssertions 7.x: `WorkManager.Tests.csproj` line 19 ✓; global `<Using>FluentAssertions>` line 24 ✓
  - `Assert.*` calls: **ONE VIOLATION** in `PassionHelperTests.cs:42` (`Assert.Pass(...)`)
  - Build clean: Not yet verified (plan line 62 unchecked)

### AC-2: WorkShift Mapping & Validation
- **Status**: TEST IMPLEMENTED; game-context validation via MS-3 (PASS)
- **Code**: `Source/WorkManager/WorkShift.cs` (constructor + `GetTimeAssignment` method)
- **Tests**: `Source/WorkManager.Tests/WorkShiftTests.cs` (lines 10–149)
  - Constructor validation: hour range (24-hour requirement) ✓ lines 25–44
  - Constructor validation: threshold >= 1 ✓ lines 17–22
  - GetTimeAssignment boundary checks (hour < 0, hour >= 24) ✓ lines 72–99
  - Valid schedule mapping (mixed hour assignments) ✓ lines 105–148
  - FluentAssertions: all tests use `.Should()` ✓
- **Manual verification**: MS-3 status = PASS (user verified 2026-06-05)

### AC-3: WorkTypeAssignmentRuleComparer & Combine
- **Status**: IMPLEMENTED
- **Code**: `Source/WorkManager/Helpers/WorkTypeAssignmentRuleComparer.cs` + `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs`
- **Tests**: `Source/WorkManager.Tests/WorkTypeAssignmentRuleComparerTests.cs` (lines 10–258)
  - Ordering logic: null handling ✓ lines 165–173
  - Ordering: skill count descending, fallback to defName ✓ lines 180–190
  - Tie-breaks: defName ordinal ✓ lines 196–208
  - Combine: null handling (main/fallback) ✓ lines 17–33
  - Combine: precedence (main over fallback) ✓ lines 39–67
  - Combine: DedicatedWorkerSettings merge ✓ lines 73–95
  - Combine: defName preservation ✓ lines 101–115
  - Deterministic tie-break: reference equality ✓ lines 251–257
  - FluentAssertions exclusive ✓

### AC-4: DedicatedWorkerSettings Combine & Validate
- **Status**: IMPLEMENTED
- **Code**: `Source/WorkManager/Settings/DedicatedWorkerSettings.cs`
- **Tests**: `Source/WorkManager.Tests/DedicatedWorkerSettingsTests.cs` (lines 10–312)
  - Combine: null handling ✓ lines 97–113
  - Combine: AllowDedicated fallback ✓ lines 17–30
  - Combine: precedence (main over fallback) ✓ lines 119–137
  - Combine: all modes (Constant, WorkTypeCount, CapablePawnRatio, PawnCount) ✓ lines 77–191
  - Validate: clamping (ConstantWorkerCount) ✓ lines 216–232
  - Validate: clamping (WorkTypeCountFactor) ✓ lines 239–255
  - Validate: clamping (CapablePawnRatioFactor) ✓ lines 262–278
  - Validate: clamping (PawnCountFactor) ✓ lines 285–302
  - FluentAssertions exclusive ✓

### AC-5: PassionHelper.GetPassionScore
- **Status**: TEST SKELETON ONLY; real coverage via manual MS-2
- **Code**: `Source/WorkManager/Helpers/PassionHelper.cs`
- **Tests**: `Source/WorkManager.Tests/PassionHelperTests.cs` (lines 19–44)
  - Current test: documents infeasibility, calls `Assert.Pass()` ✗ (CRITICAL violation, AC-1)
  - Coverage intent: manual in-game verification via MS-2 ✓ (PASS per manual-steps.md line 47)
- **Gap**: Plan AC-5 states "test normalization across all handled passion levels, asserting the expected normalized score for each." Placeholder test does not assert; relies entirely on manual. AC-1 violation blocks PR merge.

### AC-6: GetTargetWorkersCount Non-PawnCount Modes
- **Status**: TESTS IMPLEMENTED; line-coverage floor not yet verified
- **Code**: `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs` method `GetTargetWorkersCount`
- **Tests**: `Source/WorkManager.Tests/GetTargetWorkersCountTests.cs` (lines 12–269)
  - Constant mode: returns ConstantWorkerCount ✓ lines 17–34 (3 parameterized cases)
  - WorkTypeCount mode: factor × workTypeCount, rounded up ✓ lines 39–56 (4 cases)
  - CapablePawnRatio mode: factor × (capablePawnCount / workTypeCount), rounded up ✓ lines 62–110 (multiple scenarios)
  - PawnCount mode: documented as requires live game, throws NullReferenceException when map=null ✓ lines 202–219
  - Edge cases (zero workTypeCount, null Mode) ✓ lines 118–194
  - FluentAssertions exclusive ✓
- **Gap**: Plan line 69 says AC-6 checkbox; line 71 requires "Verify the covered units meet the 80% line-coverage floor." **Not yet executed**: `dotnet test` run + coverage report needed.

### AC-7 & AC-8: Tech-Reference Coverage
- **Status**: IMPLEMENTED (verify-only per plan)
- **Files**:
  - `design/architecture/tech-reference/RimWorld-1.6.md` ✓ exists, last verified 2026-06-04, grounded in observed usage
  - `design/architecture/tech-reference/dotnet-framework-4.7.2.md` ✓ exists, last verified 2026-06-04
- **Stack verification**: `design/architecture/stack.html` re-verified (updated 2026-06-04) ✓

### AC-9 & AC-10: Work-Shift Label Localization
- **Status**: IMPLEMENTED
- **Code change**: `Source/WorkManager/Settings/Settings_Schedules.cs` lines 151, 209
  - Old: hardcoded `"Work shift #{i + 1}"` (lines 148, 201 per AC-9)
  - New: `"WorkManager.Settings_Schedule_WorkShiftLabel".Translate(i + 1)` ✓
  - Key value: `"Work shift #{0}"` ✓ (byte-for-byte English text match)
- **Localization files**:
  - 1.6 English: `1.6/Languages/English/Keyed/WorkManager_Keyed.xml` line 128 ✓
  - 1.6 Russian: `1.6/Languages/Russian/Keyed/WorkManager_Keyed.xml` line 128 ✓
  - 1.6 ChineseSimplified: `1.6/Languages/ChineseSimplified/Keyed/WorkManager_Keyed.xml` line 252 ✓
  - Legacy 1.1–1.5 English: all six files contain key (e.g., `1.1/Languages/English/Keyed/WorkManager_Keyed.xml` line 29) ✓

### AC-11: Instance Null-Handling Contract
- **Status**: IMPLEMENTED uniformly across all UI entry points
- **Contract definition**: `Source/WorkManager/WorkManagerGameComponent.cs` lines 116–140
  - `IsInitialized` property: `Current.Game != null && Instance is not null` ✓ line 140
  - XML doc invariant: contract fully documented ✓ lines 116–139
- **UI guards**:
  - `WidgetsWorkPatch.cs`: lines 29, 59 (`if (!WorkManagerGameComponent.IsInitialized) return;`) ✓
  - `WorkTabPatch.cs`: lines 58, 86, 120, 159 ✓
  - `MainTabWindowWorkPatch.cs`: line 24 ✓
  - `PawnColumnWorkerWorkPriorityPatch.cs`: line 29 ✓
  - `AutoWorkPriorities.DoCell`: line 24 ✓
  - `AutoWorkSchedule.DoCell`: line 24 ✓
- **Game-scoped consumers** (kept direct dereference, no guards):
  - `WorkPriorityUpdater` (MapComponent) — invariant note expected but not verified in brief scan
  - `ScheduleUpdater` (MapComponent) — invariant note expected but not verified in brief scan
  - `PawnCache` — invariant note expected but not verified in brief scan
- **Verdict**: All UI entry points guarded uniformly. Game-scoped consumers assumed OK per plan (not fully audited).

### AC-12: No NRE on Game-less UI Paths
- **Status**: CODE GUARDS IN PLACE; runtime verification pending (MS-1)
- **Evidence**:
  - Every UI Harmony patch and pawn column method early-outs on `!IsInitialized` before any `Instance` dereference ✓
  - Test coverage: `IsInitializedTests.cs` (lines 14–72)
    - `IsInitialized_WhenInstanceIsNull_ReturnsFalse()` ✓
    - `IsInitialized_WhenInstanceIsNotNull_ButNoActiveGame_ReturnsFalse()` ✓ (stale Instance scenario)
  - **Runtime test**: MS-1 (launch RimWorld at main menu, open mod settings, check logs for NRE)
    - Status: PENDING (manual-steps.md line 25)
- **Assessment**: Code-level fix is complete; AC-12 is blocked on manual MS-1 execution.

### AC-13: Lib.Harmony 2.3.6 Removal
- **Status**: IMPLEMENTED
- **Verification**:
  - Directory `Source/packages/Lib.Harmony.2.3.6/` does not exist ✓
  - No references to 2.3.6 in `WorkManager.csproj` (only 2.4.2 PackageReference line 35) ✓
  - No references in `WorkManager.Tests.csproj` ✓
  - No references in `Directory.Build.props` ✓
  - No references in `WorkManager.slnx` ✓

### AC-14, AC-15, AC-16: Documentation Alignment
- **Status**: IMPLEMENTED
- **AC-14 (README badges)**:
  - `README.md` lines 3–8: full 1.1–1.6 badge set ✓
  - Matches `About/About.xml` supportedVersions ✓
- **AC-15 (README description)**:
  - Line 14: "automates assigning work priorities to your pawns and, optionally, their daily work schedules" ✓
  - Dependencies: Harmony + LordKuper.Common mentioned (lines 17) ✓
- **AC-16 (Re-verification of concept/stack)**:
  - `design/product/concept.html`: updated 2026-06-04 ✓
  - `design/architecture/stack.html`: updated 2026-06-04 ✓
  - `About/About.xml` `<description>`: "Automatic work priority management mod" (no mention of scheduling added, but not required per AC-16 low-priority note) ✓

### AC-17 & AC-18: Build Quality & Test Suite
- **Status**: NOT YET VERIFIED (plan lines 102–105 unchecked)
- **AC-17** (zero warnings, TreatWarningsAsErrors enabled):
  - Setting: must verify via `dotnet build Source/WorkManager.slnx -c Release`
  - Plan line 102 unchecked
- **AC-18** (test suite green, no skipped tests):
  - Test files exist and contain FluentAssertions assertions ✓
  - Test count: 64 tests across 8 files ✓
  - One placeholder test (PassionHelperTests) with `Assert.Pass()` masks coverage ✗
  - Plan line 103 unchecked ("Run the full test suite... and confirm green")
- **Blocker**: Cannot confirm AC-17/AC-18 without running build and test suite.

---

## Verdict

**[REVIEW-impl-implementation]: CONCERNS**

### High-Priority Gaps

1. **AC-1 Violation (Critical)**: `PassionHelperTests.cs:42` contains `Assert.Pass()`, an explicit `Assert.*` call. This violates AC-1 requirement: "no `Assert.*` calls remain in the test project." Must replace before merge.

2. **AC-5 Coverage Gap**: PassionHelper normalization test is a placeholder with `Assert.Pass()`. Real coverage is manual (MS-2 PASS), but automated unit test promised by AC-5 ("test normalization across all handled passion levels, asserting the expected normalized score for each") is not delivered. Fix: either implement real unit test or remove placeholder and rely on manual.

3. **AC-6, AC-18 Unverified**: GetTargetWorkersCountTests file exists and is comprehensive, but plan subtasks remain unchecked (lines 69, 71, 72). **No test execution output provided.** Cannot confirm 80% line-coverage floor or that all 64 tests pass green. Iteration 4 must execute `dotnet test` and coverage analysis.

4. **AC-12 Pending**: Manual step MS-1 (no-NRE on game-less UI paths) is pending. Code-level guards are in place; runtime verification required. Cannot close AC-12 until MS-1 is performed and marked done.

5. **AC-17 Unverified**: Build not run against `dotnet build Source/WorkManager.slnx -c Release` with `TreatWarningsAsErrors` check. Plan line 102 unchecked.

### What's Complete & Good

- Instance null-handling contract (AC-11): uniformly applied across all UI entry points, well-documented.
- Localization (AC-9, AC-10): keys present in all required locale files; English text byte-identical.
- Lib.Harmony 2.3.6 removal (AC-13): complete, no orphan references.
- Tech-reference docs (AC-7, AC-8): both exist, stack/concept re-verified.
- Test infrastructure (AC-1, partial): resolver + state isolation in place; FluentAssertions used throughout except for one `Assert.Pass()`.
- WorkShift, WorkTypeAssignmentRuleComparer, DedicatedWorkerSettings tests (AC-2, AC-3, AC-4): comprehensive, FluentAssertions exclusive, both manual verification (MS-2, MS-3) complete.
- GetTargetWorkersCountTests (AC-6): extensive test coverage exists; just needs execution verification.

### Next Iteration (Iter 4)

1. **Fix AC-1**: Remove or replace `Assert.Pass()` in PassionHelperTests.
2. **Execute & verify AC-18**: Run `dotnet test Source/WorkManager.slnx`; report green/fail.
3. **Verify AC-6 coverage**: Confirm 80% line-coverage floor on AC-2…AC-6 units via coverage report.
4. **Verify AC-17**: Run `dotnet build Source/WorkManager.slnx -c Release` with zero-warning check.
5. **Execute MS-1**: User launches RimWorld, tests game-less UI paths, confirms no NRE in logs.
6. **Mark plan complete**: Check lines 69–72 and 80 once all above done.

---

[REVIEW-impl-implementation]: CONCERNS
