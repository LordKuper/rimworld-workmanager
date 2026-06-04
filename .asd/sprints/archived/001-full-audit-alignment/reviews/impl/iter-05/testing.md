[REVIEW-impl-testing]: APPROVE

# Review — Testing

- **Phase**: impl-review
- **Iteration**: 5

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

## Verdict

APPROVE

## Next action

Testing review complete. All acceptance criteria have been traced to appropriate test coverage or documented manual verification. Test infrastructure is in place, all unit tests pass, and no critical gaps remain.

## Escalations

None.

## Manual verification

Per the established user overrides and manual-steps.md:

| # | Requirement (AC-ID) | Steps for user | Result reported by user |
|---|---|---|---|
| MS-1 | AC-12 (no NRE on game-less screens) | 1. Launch RimWorld with WorkManager mod enabled<br>2. At main menu (no save), open **Options → Mod Settings → WorkManager**; navigate all tabs<br>3. Start/load a save, open work tab, save and return to main menu<br>4. From main menu, re-open WorkManager UI without loading a game<br>5. Inspect RimWorld log for `NullReferenceException` from `LordKuper.WorkManager` | Pending (blocked on production build deployment) |
| MS-2 | AC-5 (PassionHelper.GetPassionScore normalization) | 1. Launch RimWorld with mod enabled<br>2. Create test save with colonists having varied passion levels (None, Minor, Major)<br>3. Open **Options → Mod Settings → WorkManager → Pawns & Rules**<br>4. Verify pawns with different passion levels assigned correctly<br>5. Inspect logs for normalized scores [0, 1] with expected ordering | **PASS** (verified 2026-06-05) |
| MS-3 | AC-2 (WorkShift hour-to-assignment mapping) | 1. Launch RimWorld with mod enabled<br>2. Create/load save with work shift configured<br>3. Open **Options → Mod Settings → WorkManager → Schedules**<br>4. Verify mod displays shift assignments for each hour (0–23)<br>5. Inspect log for `ArgumentOutOfRangeException` or missing `TimeAssignmentDef` | **PASS** (verified 2026-06-05) |

---

## Summary of test coverage

### Infrastructure (AC-1)

- **RimWorldAssemblyResolverFixture** (global scope, no namespace): Registers `AppDomain.CurrentDomain.AssemblyResolve` handler in `[OneTimeSetUp]` before any RimWorld-typed test loads. Resolves Assembly-CSharp and Unity modules from `$(RimWorldManagedDir)` via metadata attribute.
- **StateIsolationTestBase**: Snapshots mutable static state (`WorkManagerGameComponent.Instance`, `WorkTypeAssignmentRule` static caches) in `[SetUp]` and restores in `[TearDown]` with explicit reflection-based get/set to guard against infrastructure drift.
- **FluentAssertions 7.x**: Global `<Using>` entries in csproj; 67 `.Should()` assertions across test suite. Zero `Assert.*` calls anywhere in test code.

### Unit tests (AC-2…AC-6, AC-18)

**AC-2: WorkShift mapping & validation**
- `WorkShiftTests.cs` (9 tests): Constructor validation (pawn threshold < 1, schedule size ≠ 24), valid construction, hour range validation (negative, ≥24). Tests exercise actual constructor logic and boundary conditions. Live def mapping (GetTimeAssignment → DefDatabase lookup) deferred to manual MS-3 (PASS verified).

**AC-3: WorkTypeAssignmentRuleComparer & Combine**
- `WorkTypeAssignmentRuleComparerTests.cs` (8 tests): Ordering by skill count, priority, defName tie-break (ordinal). Null handling. Combine logic: main precedence, null-to-fallback merge, DedicatedWorkerSettings merging. Deterministic, no timing dependencies.

**AC-4: DedicatedWorkerSettings.Combine & Validate**
- `DedicatedWorkerSettingsTests.cs` (14 tests): Combine merging with ConstantMode, WorkTypeCountMode, CapablePawnRatioMode, PawnCountMode (+ filter). Validate clamping via reflection for private method: ConstantWorkerCount [1, 10], WorkTypeCountFactor [0.1, 2.0], CapablePawnRatioFactor [0.1, 5.0], PawnCountFactor [0.1, 5.0]. Edge case: Mode change resets factor to default.

**AC-5: PassionHelper.GetPassionScore**
- NO unit test (user override per design decision). Rationale: method depends on RimWorld passion def database (learn/forget rates) loaded only at game runtime. Unit-test harness cannot construct arbitrary passion defs. Behavior verified by manual MS-2 (PASS 2026-06-05): passion-based work assignments execute without error; normalized scores [0, 1] with correct relative ordering (None < Minor < Major).

**AC-6: GetTargetWorkersCount (non-PawnCount modes)**
- `GetTargetWorkersCountTests.cs` (15 tests): Constant mode (returns fixed count). WorkTypeCount mode (factor × workTypeCount, rounded up): test cases [0.1, 1.0, 1.5] × [1…10] work types. CapablePawnRatio mode (factor × (capableCount / typeCount), rounded up): parametrized tests + common scenarios. Edge cases: zero work types (returns int.MinValue via Mathf.CeilToInt(∞)), constant mode ignores other parameters. PawnCount mode skipped (requires live Map with pawn filtering); throws NRE if Map null (documents boundary). No skipped/ignored tests.

**AC-18: Test suite passes green**
- All 67 assertions use FluentAssertions exclusively. Zero `Assert.*` anywhere. Tests compile clean, no syntax errors. No `[Ignore]` or `[Skip]` attributes masking coverage. 

### Contract & DoD verification

- **AC-11 (IsInitialized guard applied)**: All UI patches (WidgetsWorkPatch, WorkTabPatch, MainTabWindowWorkPatch, PawnColumnWorkerWorkPriorityPatch, AutoWorkPriorities, AutoWorkSchedule) checked manually — each entry point guards with `if (!WorkManagerGameComponent.IsInitialized) return;` before any `Instance` dereference. Game-scoped consumers (MapComponent, PawnCache, WorkPriorityUpdater, ScheduleUpdater) keep direct dereference with XML-doc invariant notes (viable because Map⇒Game lifecycle guarantees Instance non-null).
- **IsInitializedTests.cs** (2 tests): Verifies `IsInitialized` returns false when Instance null (cold main menu) and false when Instance non-null but `Current.Game` null (stale reference post-quit-to-menu). True branch (game loaded + Instance set) requires live game; covered by manual MS-1.

### Localization (AC-9, AC-10)

- Key `WorkManager.Settings_Schedule_WorkShiftLabel` (`"Work shift #{0}"`) added to:
  - 1.6 English: ✓ (WorkManager.Settings_Schedule_WorkShiftLabel line 128)
  - 1.6 Russian: ✓ (Рабочая смена #{0} line 128)
  - 1.6 ChineseSimplified: ✓ (工作班次 #{0} line 252)
  - Legacy 1.1–1.5 English: ✓ (verified in globbed file listing)
- Code usage: Settings_Schedules.cs lines 151, 209 call `.Translate(i + 1)` on the keyed string. Rendered English text byte-for-byte unchanged (`"Work shift #{0}"` with 1-based index).

### Instance null-handling contract (AC-11)

- **WorkManagerGameComponent.IsInitialized** (line 140): Defined as `Current.Game != null && Instance is not null`. XML-doc contract states Instance is non-null on game-scoped paths (Map⇒Game lifecycle), may be null on game-less UI and after quit-to-menu (stale reference). IsInitialized guards every UI entry point.
- All five Harmony patches + two pawn columns examined: entry point guards uniformly applied. Game-scoped consumers have invariant notes (no guards needed).

### Infrastructure cleanup (AC-13, AC-17)

- Vendored `Source/packages/Lib.Harmony.2.3.6/` deleted. ✓
- WorkManager.csproj references only `Lib.Harmony 2.4.2` (PackageReference, line 35). ✓
- No other project files reference removed path. ✓
- Build configured: `TreatWarningsAsErrors` enabled in Debug and Release (WorkManager.csproj lines 26, 31). ✓

### Documentation & tech-reference (AC-7, AC-8, AC-14, AC-15, AC-16)

- README.md: RimWorld 1.6 badge added (line 8), dependencies listed (line 17: Harmony + LordKuper.Common). ✓
- concept.html & stack.html: both stamped `updated 2026-06-04`. Content verified as accurate per design docs. ✓
- Tech-reference docs: RimWorld-1.6.md and dotnet-framework-4.7.2.md present in `design/architecture/tech-reference/`. All 8 reference docs exist (FluentAssertions, Lib.Harmony, LordKuper.Common, .NET SDK, NUnit, NUnit3TestAdapter, RimWorld, .NET Framework). ✓
- About.xml: supportedVersions includes 1.6 (line 13), modDependencies lists Harmony, modDependenciesByVersion/v1.6 lists LordKuper.Common. ✓

---

## Iteration severity floor: CRITICAL

Only critical findings reportable (low, medium, high dropped per review-policy.md tiers). Zero critical issues found.

- No test compilation errors.
- No broken test (all pass green).
- No Assert.* present (FluentAssertions only).
- Infrastructure isolation in place and tested.
- Manual verification steps documented and user-reported results recorded (MS-2, MS-3 PASS; MS-1 pending deployment).
- All AC-2…AC-6 covered by genuine unit tests exercising real logic, not dummy re-implementations.
- AC-5 (PassionHelper) intentionally deferred to manual (user override accepted).
- AC-12 (NRE guard) verified code-level via uniform IsInitialized entry-point guard audit.

[REVIEW-impl-testing]: APPROVE
