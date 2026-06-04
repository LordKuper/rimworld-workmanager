---
responsibility:
  owns: task breakdown, dod, task status (checkboxes)
  excludes: requirements, design decisions, code, review findings
  delegates_to: design/ docs (requirements/design), reviews/ (findings)
---

# Plan

<!--
Format rules (parser-critical):
- Overview, Context, Definition of Done — prose only, NO checkboxes
- Checkboxes (- [ ]/- [x]) appear ONLY inside `### Task N:` sections
- Checkboxes in any non-task section break orchestrator task parsing
- A subtask deferred for a manual action stays `- [ ]` and is suffixed ` — BLOCKED: MS-N` (see manual-steps.md)
-->

## Overview

This plan implements every finding of the full audit of the WorkManager mod (sprint `001-full-audit-alignment`), bringing the codebase and its documentation into compliance with the four reference standards: the `.asd` rules, the documented technology stack, the `LordKuper.Common` upstream contract, and the code conventions. The work is behavior-preserving — no new mod features, no UI/UX changes — and is decomposed into seven tasks tracing to the 18 acceptance criteria (AC-1…AC-18) of the sprint PRD.

The implementation approach follows the five approved ADRs: a two-part test-isolation infrastructure (ADR-0002) and real unit coverage of the audit-named pure-logic units (AC-1…AC-6, AC-18); a single documented `WorkManagerGameComponent.Instance` null-handling contract with an `IsInitialized` guard at UI entry points (ADR-0001, AC-11…AC-12); keyed localization of the two hardcoded work-shift labels across all shipped locales (ADR-0005, AC-9…AC-10); removal of the stale vendored Lib.Harmony 2.3.6 package tree (ADR-0003, AC-13); documentation alignment of README and About.xml plus re-verification of concept/stack docs (AC-14…AC-16); and a final build + quality gate (AC-17…AC-18). The two missing tech-reference docs called out by AC-7/AC-8 (`RimWorld-1.6.md`, `dotnet-framework-4.7.2.md`) were already authored and promoted during the design / design-promote phases (ADR-0004) — they are verify-only here, folded into the docs-alignment task rather than re-created.

## Context

- Sprint scope: [sprint.md](./sprint.md)
- Audit findings (G1–G4 + cleanup): [audit.md](./audit.md)
- Requirements (US-1…US-7, AC-1…AC-18): [prd.html](./design/prd.html)
- ADR-0001 — Instance null-handling contract: [adr-0001-instance-null-handling-contract.html](../../../design/architecture/adr/adr-0001-instance-null-handling-contract.html)
- ADR-0002 — Test isolation infrastructure: [adr-0002-test-isolation-infrastructure.html](../../../design/architecture/adr/adr-0002-test-isolation-infrastructure.html)
- ADR-0003 — Remove vendored Harmony 2.3.6: [adr-0003-remove-vendored-harmony-2.3.6.html](../../../design/architecture/adr/adr-0003-remove-vendored-harmony-2.3.6.html)
- ADR-0004 — Tech-reference coverage: [adr-0004-tech-reference-coverage.html](../../../design/architecture/adr/adr-0004-tech-reference-coverage.html)
- ADR-0005 — Localize work-shift labels: [adr-0005-localize-work-shift-labels.html](../../../design/architecture/adr/adr-0005-localize-work-shift-labels.html)
- Build/test/lint commands (SSoT): [commands.yaml](../../project/commands.yaml)
  - build: `dotnet build Source/WorkManager.slnx -c Release`
  - test: `dotnet test Source/WorkManager.slnx`
  - lint (verify): `dotnet format Source/WorkManager.slnx --verify-no-changes`
  - jb-cleanup: `jb cleanupcode Source\WorkManager.slnx` (before build)
  - jb-inspect: `jb inspectcode Source\WorkManager.slnx -o=".\TestResults\jb-inspect.sarif" --build` (after lint; sarif must be error/warning-clean)

## Definition of Done

The sprint is done when all of the following hold:

- All 18 acceptance criteria (AC-1…AC-18) are satisfied and traced to a completed task below: AC-1 (Task 1); AC-2…AC-6, AC-18 (Task 2); AC-11, AC-12 (Task 3); AC-9, AC-10 (Task 4); AC-13 (Task 5); AC-7, AC-8, AC-14, AC-15, AC-16 (Task 6); AC-17, AC-18 (Task 7).
- Test infrastructure (ADR-0002) is in place: a global `[SetUpFixture]` registering the RimWorld `AssemblyResolve` handler, and an abstract `StateIsolationTestBase` snapshot/restore base class. The FluentAssertions 7.x package is in use and no `Assert.*` calls remain (the `Assert.Pass()` placeholder is deleted).
- Real unit tests cover the audit-named pure-logic units (`WorkShift`, `WorkTypeAssignmentRuleComparer` + `WorkTypeAssignmentRule.Combine`, `DedicatedWorkerSettings.Combine`/`Validate`, `PassionHelper.GetPassionScore`, `GetTargetWorkersCount` non-`PawnCount` modes), use FluentAssertions exclusively, and meet the project's 80% line-coverage floor on those covered units. No skipped/ignored tests mask missing AC-2…AC-6 coverage.
- The `WorkManagerGameComponent.Instance` null-handling contract (ADR-0001) is applied uniformly: `IsInitialized` guards every UI-scoped entry point (Harmony patches + pawn columns); game-scoped consumers (`MapComponent` ticks, `PawnCache`) keep direct dereference with an XML-doc invariant note. No NRE from `Instance` on game-less screens.
- The two hardcoded `"Work shift #{i + 1}"` strings are replaced with the keyed lookup `WorkManager.Settings_Schedule_WorkShiftLabel` (value `"Work shift #{0}"`); the key is present in all 1.6 locales (English, Russian, ChineseSimplified) and English in the legacy 1.1–1.5 folders; visible English text is byte-for-byte unchanged.
- The `Source/packages/Lib.Harmony.2.3.6/` tree is deleted and unreferenced; the only Harmony reference is the `Lib.Harmony` 2.4.2 `PackageReference`.
- Documentation is aligned: README badges include RimWorld 1.6 and the description mentions scheduling + Harmony + LordKuper.Common deps; `concept.html` and `stack.html` re-verified against current `Source/` (confirmed accurate or updated in lockstep with `updated` bumped); `About.xml` `<description>` updated only if other description text is touched; the two tech-reference docs (`RimWorld-1.6.md`, `dotnet-framework-4.7.2.md`) verified present and version-accurate, with no reference pointing to an unused version.
- The production build (`Source/WorkManager.slnx`) and test build compile clean — zero errors, zero warnings, with `TreatWarningsAsErrors` enabled in Debug and Release — and the full test suite passes green.
- Test scope is explicit: automated coverage is unit-level over the pure-logic units (AC-2…AC-6). The game-less-screen no-NRE check (AC-12) is verified at the code level (every UI entry point early-outs via `IsInitialized` before any `Instance` dereference); if a live-game integration test is not feasible without a running RimWorld instance, AC-12 is recorded as a manual in-game verification note rather than an automated test, and the code-level guard audit stands as the primary evidence.
- All impl-review reviewers return green (APPROVE) in a single iteration, with `dotnet format --verify-no-changes` clean and the `jb-inspect` sarif free of errors/warnings.

### Task 1: Test infrastructure (ADR-0002) — assembly resolver + state isolation base
- [x] Add a global `[SetUpFixture]` (assembly scope, no namespace) in `Source/WorkManager.Tests/` whose `[OneTimeSetUp]` registers an `AppDomain.CurrentDomain.AssemblyResolve` handler resolving `Assembly-CSharp` and the Unity modules from `$(RimWorldManagedDir)`, before any RimWorld-typed test type loads.
- [x] Surface the `RimWorldManagedDir` path to the test assembly via an MSBuild-emitted constant/config so the resolver knows where to load from (edit `WorkManager.Tests.csproj` as needed).
- [x] Add an abstract `StateIsolationTestBase` that snapshots the relevant mutable static state in `[SetUp]` and restores it in `[TearDown]` (including `WorkManagerGameComponent.Instance`); enumerate every snapshotted static field.
- [x] Confirm the FluentAssertions 7.x package and the global `<Using>` for `FluentAssertions` are present and used; do not add a mocking framework.
- [x] Delete the `Assert.Pass()` placeholder in `WorkManager.Tests/WorkShiftTests.cs` (the real `WorkShift` tests in Task 2 replace it); ensure no `Assert.*` calls remain in the test project.
- [x] Build the test project clean and confirm the resolver loads a RimWorld-typed test type without `FileNotFoundException`.

### Task 2: Unit tests for pure-logic units (ADR-0002) — AC-2…AC-6, AC-18
- [ ] `WorkShift`: test start/end hour-to-shift mapping and validation/normalization of out-of-range or invalid hour values. (AC-2)
- [ ] `WorkTypeAssignmentRuleComparer`: test ordering; `WorkTypeAssignmentRule.Combine`: test the merge result including deterministic tie-breaks. (AC-3)
- [ ] `DedicatedWorkerSettings.Combine` and `DedicatedWorkerSettings.Validate`: test value-clamping on out-of-range inputs. (AC-4)
- [ ] `PassionHelper.GetPassionScore`: test normalization across all handled passion levels, asserting the expected normalized score for each. (AC-5)
- [ ] `GetTargetWorkersCount`: test the non-`PawnCount` modes, asserting the computed target per mode. (AC-6)
- [ ] Use FluentAssertions `.Should()` exclusively; derive from `StateIsolationTestBase` for any test touching static state or RimWorld defs; pure-input tests may skip the base class.
- [ ] Verify the covered units meet the 80% line-coverage floor (AC-6); ensure no test is skipped/ignored in a way that masks AC-2…AC-6 coverage (AC-18).
- [ ] Run `dotnet test Source/WorkManager.slnx` and confirm green.

### Task 3: Instance null-handling contract (ADR-0001) — AC-11, AC-12
- [x] Add a static `WorkManagerGameComponent.IsInitialized` helper returning `Instance is not null`; document the contract in XML doc ("Instance is valid only while a Game is loaded; non-null in game-scoped paths, may be null on game-less UI paths").
- [x] Guard every UI-scoped entry point with an early-out on `!IsInitialized` before any `Instance` dereference: the Harmony patches in `Source/WorkManager/Patches/**` (`WidgetsWorkPatch`, `WorkTabPatch`, `MainTabWindowWorkPatch`, `PawnColumnWorkerWorkPriorityPatch`) and the pawn columns `AutoWorkPriorities.DoCell` / `AutoWorkSchedule.DoCell` (and any header method).
- [x] Normalize the existing mixed guards (`?.`, `== null`) to the single `IsInitialized` entry-point idiom so all UI sites read uniformly (guard at entry point, not at every leaf).
- [x] Add an XML-doc invariant note to the game-scoped consumers that keep direct dereference — `WorkPriorityUpdater` and `ScheduleUpdater` (`MapComponent`) and `PawnCache` — stating that Instance is non-null by the Map⇒Game lifecycle; do not add guards to these hot paths.
- [x] Verify no UI call site dereferences `Instance` divergently from the contract (AC-11).
- [ ] Confirm no `NullReferenceException` from `Instance` on game-less UI paths (AC-12) via the code-level guard audit; if a live-game integration test is infeasible, record AC-12 as a manual in-game verification note (see DoD). — BLOCKED: MS-1

### Task 4: Localize work-shift labels (ADR-0005) — AC-9, AC-10
- [x] Replace the two hardcoded `"Work shift #{i + 1}"` strings in `Source/WorkManager/Settings/Settings_Schedules.cs` (lines 148 and 201) with a keyed lookup using `WorkManager.Settings_Schedule_WorkShiftLabel` via `.Translate()` and the 1-based shift index argument. (AC-9)
- [x] Confirm the rendered English text is byte-for-byte identical to the current output (key value `"Work shift #{0}"`). (AC-9)
- [x] Add the `WorkManager.Settings_Schedule_WorkShiftLabel` key to the 1.6 keyed files for English, Russian, and ChineseSimplified (`1.6/Languages/**/Keyed/WorkManager_Keyed.xml`). (AC-10)
- [x] Add the same key (English) to the legacy 1.1–1.5 English keyed files (`1.*/Languages/English/Keyed/WorkManager_Keyed.xml`). (AC-10)
- [x] Confirm no other visible text or layout change.

### Task 5: Remove vendored Lib.Harmony 2.3.6 (ADR-0003) — AC-13
- [x] Delete the `Source/packages/Lib.Harmony.2.3.6/` directory tree (DLLs, nupkg, pdbs, README).
- [x] Confirm no project or build file (`WorkManager.csproj`, `WorkManager.Tests.csproj`, `Directory.Build.props`, `WorkManager.slnx`) references the removed path; the only remaining Harmony reference is the `Lib.Harmony` 2.4.2 `PackageReference`. (AC-13)

### Task 6: Documentation alignment — AC-7, AC-8, AC-14, AC-15, AC-16
- [x] Update `README.md` version badges to include RimWorld 1.6 (full 1.1–1.6 support range), matching `About/About.xml` `supportedVersions`. (AC-14)
- [x] Expand the `README.md` description to mention schedule management and the Harmony + LordKuper.Common dependencies, consistent with `concept.html`; link to the SSoT instead of duplicating the full feature list. (AC-15)
- [x] After Tasks 1–5 land, re-verify `design/product/concept.html` and `design/architecture/stack.html` against current `Source/` + project files; confirm still accurate (no edit) or update in lockstep with `updated` bumped. (AC-16)
- [x] Update `About/About.xml` `<description>` to mention scheduling only if other description text is touched (low priority). (AC-16)
- [x] Verify-only: confirm `design/architecture/tech-reference/RimWorld-1.6.md` and `dotnet-framework-4.7.2.md` exist, are version-accurate, and that every technology in `stack.html` has a corresponding reference with no reference pointing to an unused version (these two docs were authored in design / design-promote per ADR-0004 — do not re-create). (AC-7, AC-8)

### Task 7: Build + quality gate — AC-17, AC-18
- [ ] Run `jb cleanupcode Source\WorkManager.slnx` before the build.
- [ ] Run the production build `dotnet build Source/WorkManager.slnx -c Release` and confirm zero errors and zero warnings, with `TreatWarningsAsErrors` enabled in Debug and Release. (AC-17)
- [ ] Run the full test suite `dotnet test Source/WorkManager.slnx` and confirm green, with no skipped/ignored tests masking AC-2…AC-6 coverage. (AC-18)
- [ ] Run `dotnet format Source/WorkManager.slnx --verify-no-changes` and confirm clean.
- [ ] Run `jb inspectcode Source\WorkManager.slnx -o=".\TestResults\jb-inspect.sarif" --build` and confirm the sarif has no error/warning entries.

## Risks
- Engine refactor regression: the Instance null-contract change (Task 3) touches code with no pre-existing automated net; Tasks 1–2 land the pure-logic test net first to mitigate silent behavior change (verification-driven, audit Risks).
- AC-12 automation gap: a true game-less no-NRE assertion may not be runnable without a live RimWorld instance; mitigated by the code-level entry-point guard audit and a manual in-game verification note (see DoD).
- Localization parity: legacy 1.1–1.5 folders ship English-only keyed text; Task 4 adds the new key only where each locale set exists (1.6 EN/RU/zh-Hans; 1.1–1.5 EN).
- RimWorld-1.6.md grounding: some API signatures in the tech-reference are marked unverified (wiki WebFetch 403 at design time); Task 6 verify-only does not block on closing those, but flags any discovered inaccuracy.

## Dependencies
- Task 2 depends on Task 1 (test infrastructure must exist before unit tests).
- Task 3 should land after Task 2 where the changed units are test-covered (regression net first).
- Task 6 (concept/stack re-verification) depends on Tasks 1–5 (verify against the post-change code).
- Task 7 depends on all prior tasks (final build + quality gate over the complete change set).

## Out of scope
- UI/UX changes, UX-spec, or design-system work (audit/refactor sprint; localization preserves identical visible text).
- New mod features or behavior changes to the assignment/schedule engines beyond the uniform null contract.
- Editing the `LordKuper.Common` upstream library.
- Modifying ASD workflow infrastructure (`.asd/rules/`, `.asd/templates/`, `.claude/`, `CLAUDE.md`).
- A full project requirements document (`design/product/requirements.html`) — deferred to a future requirements sprint.
- Tests for live-game-dependent engine paths (multi-pass priority assignment, schedule scoring) — only the audit-named pure-logic units are covered.
