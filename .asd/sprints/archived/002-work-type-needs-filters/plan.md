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

This plan implements the WorkManager-local needs filter on `WorkTypeAssignmentRule` (sprint `002-work-type-needs-filters`). The player can add any `NeedDef` with a 0–100% threshold to any work-type rule. While a pawn's level for any configured need is strictly below its threshold, the work type is not assigned to that pawn (priority 0), on every assignment path, including the dedicated-worker fail-safe. The work is split into eight tasks that trace to the 49 acceptance criteria (AC-1…AC-49) of the sprint PRD.

The approach follows the three accepted ADRs. ADR-0006 adds the `NeedLimit` entry type, the tri-state `FilterNeeds` flag and the `NeedLimits` list, their persistence, validation, merge and construction, and a pure static `IsNeedBlocked` seam called from `IsAllowedWorker`. ADR-0007 changes one condition in the `AssignDedicatedWorkersForDay` fail-safe so that need-blocked pawns are excluded. ADR-0008 builds the Needs settings section from Common's public API only, adds a Needs part to the rule summary, and adds 8 new and rewords 3 existing keys in the three 1.6 locales with real Russian and Chinese translations. Unit tests cover the pure logic through a test-only `FakeDefProvider`. The Scribe round trip, the live `pawn.needs` adapter, the fail-safe and the UI are verified by the user in RimWorld through manual steps. A final gate builds, tests and checks the change set. The needs research (AC-45) is already documented in audit.md and promoted to the tech-reference; it is verify-only here.

## Context

- Sprint scope: [sprint.md](./sprint.md)
- Audit findings (gaps G-1…G-7, risks, build/test environment under Dependencies): [audit.md](./audit.md)
- Sprint requirements (US-1…US-8, AC-1…AC-49): [prd.html](./design/prd.html)
- Persistent requirements (feature set NF, NF-AC-n = sprint AC-n): [requirements.html](../../../design/product/requirements.html)
- ADR-0006 — Needs filter model and evaluation: [adr-0006-needs-filter-model-and-evaluation.html](../../../design/architecture/adr/adr-0006-needs-filter-model-and-evaluation.html)
- ADR-0007 — Needs block absolute in the fail-safe: [adr-0007-needs-block-absolute-in-failsafe.html](../../../design/architecture/adr/adr-0007-needs-block-absolute-in-failsafe.html)
- ADR-0008 — Needs UI, summary and strings: [adr-0008-needs-ui-summary-and-strings.html](../../../design/architecture/adr/adr-0008-needs-ui-summary-and-strings.html)
- Tech reference: [RimWorld-1.6.md](../../../design/architecture/tech-reference/RimWorld-1.6.md) (Needs section), [LordKuper.Common-1.6.md](../../../design/architecture/tech-reference/LordKuper.Common-1.6.md) (DefCache latch, DefProvider seam, public UI helpers)
- Project rules: [custom-coding-rules.md](../../project/custom-coding-rules.md) (zero warnings, FluentAssertions 7.x only, static-state isolation, no design-doc references in code)
- Commands SSoT: [commands.yaml](../../project/commands.yaml). Its entries are Windows-oriented. On this Linux/WSL machine the verified equivalents from audit.md Dependencies apply:
  - build: `RIMWORLD_DIR="/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld" LORDKUPER_COMMON_DIR="/mnt/c/Program Files (x86)/Steam/steamapps/workshop/content/294100/3531352422" /tmp/dotnet10/dotnet build Source/WorkManager.slnx -c Release`
  - test: build the test project with `-p:RimWorldManagedDirForTests=C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed`, copy `Source/WorkManager.Tests/bin/Release/net472/*` to a Windows-local directory, run `nunit3-console.exe` 3.20.1 through WSL interop (baseline 63/63).
  - lint: `dotnet format Source/WorkManager.slnx --verify-no-changes` with the same env vars and SDK.
  - jb-cleanup / jb-inspect: run only if `jb` is on PATH; otherwise record the skip.

## Definition of Done

The sprint is done when all of the following hold:

- Every acceptance criterion AC-1…AC-49 is satisfied and traced to a completed task: AC-1…AC-12 (Task 1); AC-13…AC-23 (Task 2); AC-24…AC-26 (Task 3); AC-37, AC-40…AC-44, AC-49 strings (Task 4); AC-27…AC-40 UI and summary (Task 5); AC-47, AC-48 (Task 6); AC-9, AC-11, AC-12, AC-17, AC-24 and the UI and locale checks in the game (Task 7); AC-43, AC-44, AC-45, AC-46, AC-47 (Task 8).
- `NeedLimit`, `FilterNeeds`, `NeedLimits`, `ValidateNeedsFilter`, both `IsNeedBlocked` overloads and `GetAddableNeeds` exist with the shapes and semantics fixed in ADR-0006 and ADR-0008. The load path reads only `DefName` and `Threshold`, never `.Def` or `.Label`. No shipped rule has needs entries or `FilterNeeds == true`.
- The `AssignDedicatedWorkersForDay` fail-safe excludes need-blocked pawns only. With every combined rule Off or empty, its candidates and the resulting priorities are identical to the previous version.
- The Needs section, the rule-summary Needs part and the tooltip rewording match ADR-0008. The section uses only Common's public API and vanilla widgets, with no reflection into Common.
- Each 1.6 locale (English, Russian, ChineseSimplified) has the same 55 `LordKuper.WorkManager.Settings.WorkTypes.*` keys. The Russian and ChineseSimplified values of the 8 new and 3 reworded keys are real translations. No 1.1–1.5 language file changes.
- The Release build of `Source/WorkManager.slnx`, with the verified Linux command above, reports 0 warnings and 0 errors (`TreatWarningsAsErrors` stays on). Building from the repo root rewrites the tracked `1.6/Assemblies/LordKuper.WorkManager.{dll,pdb,xml}`. This is expected, and the rebuilt output is committed as `build: rebuild mod assembly`, as in sprint 001.
- The full suite passes through the Windows NUnit console route: the 63 pre-existing tests plus the new needs-filter tests, with no skipped or ignored tests. New tests use FluentAssertions 7.x only, isolate static state (`DefProvider.Current`, default-rule caches) per test, and cover AC-2, AC-3, AC-4, AC-6, AC-7, AC-8, AC-10, AC-13…AC-19 and the add-menu selection logic.
- `dotnet format --verify-no-changes` is clean. `jb cleanupcode` and `jb inspectcode` are run and the sarif has no error or warning entries, or, if `jb` is not installed on this machine, the skip is recorded in the Task 8 result.
- Source and tests contain no references to ASD artefacts (AC-n, ADR, PRD, Task n, sprint ids).
- The manual in-game checks in `manual-steps.md` (Task 7) have been run by the user in RimWorld 1.6 and are marked `done`.
- All impl-review reviewers return APPROVE.

### Task 1: Needs filter data model, persistence, validation and merge (ADR-0006) — owner: backend-dev — AC-1…AC-12
- [x] Add `internal sealed class NeedLimit : DefCache<NeedDef>, IExposable` in `Source/WorkManager/Settings/NeedLimit.cs` with `public float Threshold`, `internal const float DefaultThreshold = 0.5f`, and constructors `NeedLimit()`, `NeedLimit(string defName)`, `NeedLimit(NeedDef def)`; XML `<summary>` on every member. (AC-1, AC-32)
- [x] Implement `public new void ExposeData()`: `base.ExposeData()` then `Scribe_Values.Look(ref Threshold, …, DefaultThreshold)`; persist the defName only, never `Scribe_Defs` / `LookMode.Def`. (AC-9, AC-11, AC-12)
- [x] Add rule fields after `AllowedWorkers` in `WorkTypeAssignmentRule`: `public bool? FilterNeeds` (null = Inherit, false = Off, true = On) and `public List<NeedLimit>? NeedLimits = []`. (AC-1)
- [x] Extend `WorkTypeAssignmentRule.ExposeData` with `Scribe_Values.Look(ref FilterNeeds, …)` and `Scribe_Collections.Look(ref NeedLimits, …, LookMode.Deep)`, then call `ValidateNeedsFilter()` when `Scribe.mode == LoadSaveMode.LoadingVars`. Do not bump `Settings_Version`. (AC-9, AC-10, AC-11)
- [x] Add `internal void ValidateNeedsFilter()`: `NeedLimits ??= []`; drop null entries and entries with empty `DefName`; remove duplicate `DefName`s (OrdinalIgnoreCase, keep first); clamp `Threshold` with `Mathf.Clamp01`; on the default rule (`DefName == null`) set `FilterNeeds ??= false`. Read only `DefName` and `Threshold`. Call it from `Validate()` too. (AC-2, AC-3, AC-4, AC-10, AC-12)
- [x] Update `CreateRule(defName)`: `FilterNeeds = defName == null ? false : null`, `NeedLimits = []`. Set `FilterNeeds = false` explicitly on the default rule in `DefaultRules`; add no needs entries to any shipped rule. (AC-4, AC-5, AC-6)
- [x] Extend `Combine(main, fallback)`: take `source = main.FilterNeeds.HasValue ? main : fallback`, copy `FilterNeeds` and a shallow copy `[.. source.NeedLimits ?? []]`. (AC-7, AC-8)
- [x] Build with the verified Linux command; 0 warnings, 0 errors. Commit.

### Task 2: Needs evaluation in IsAllowedWorker (ADR-0006) — owner: backend-dev — AC-13…AC-23
- [x] Add the pure seam `internal static bool IsNeedBlocked(bool? filterNeeds, IReadOnlyList<NeedLimit>? limits, Func<NeedDef, float?> getLevelPercentage)`: false unless the state is On and the list is non-empty; skip entries with `Def == null`; skip a null level; block when `level < Threshold` for any entry (strict); keep no state. (AC-13, AC-14, AC-15, AC-16, AC-17, AC-18, AC-19, AC-21)
- [x] Add `internal bool IsNeedBlocked(Pawn pawn)` as a one-line call into the seam with `def => pawn.needs?.TryGetNeed(def)?.CurLevelPercentage`. Use `CurLevelPercentage`, not `CurLevel`. (AC-13, AC-16, AC-17)
- [x] Change `IsAllowedWorker(pawn)` to `AllowedWorkers!.SatisfiesFilter(pawn, Def) && !IsNeedBlocked(pawn)`. (AC-20)
- [x] Confirm by code reading that `PawnCache.IsAllowedWorker` stays unchanged, memoises per update and is cleared on every refresh, and that priority 0 stays the default outcome of `UpdateCache` (no new write path). (AC-22, AC-23)
- [x] Build with the verified Linux command; 0 warnings, 0 errors. Commit.

### Task 3: Absolute needs block in the dedicated-worker fail-safe (ADR-0007) — owner: backend-dev — AC-24…AC-26
- [x] In `WorkPriorityUpdater.AssignDedicatedWorkersForDay`, change the fail-safe candidate loop to `if (!goodWorkers.Contains(pc) && !rule.IsNeedBlocked(pc.Pawn)) availableWorkers.Add(pc);`, using the combined rule already in scope. (AC-24, AC-25)
- [x] Add a code comment explaining that the fail-safe excludes need-blocked pawns only and deliberately does not apply the allowed-workers filter (the stricter restriction is a known follow-up). No ASD references in the comment.
- [x] Confirm by code reading that no other assignment path changes and that, with the filter Off or empty, `IsNeedBlocked` returns false before it reads the pawn, so candidates and priorities are identical to the previous version. (AC-26)
- [x] Build with the verified Linux command; 0 warnings, 0 errors. Commit.

### Task 4: Needs-filter strings and localization (ADR-0008) — owner: frontend-dev — AC-37, AC-40…AC-44, AC-49
- [x] Add 8 members to `Resources.Strings.Settings.WorkTypes`, keyed `LordKuper.WorkManager.Settings.WorkTypes.<Name>` with the nested `nameof` form: `NeedsLabel`, `NeedsTooltip`, `FilterNeedsLabel`, `FilterNeedsOnTooltip`, `FilterNeedsOffTooltip`, `NeedsEmptyLabel`, `NeedUnavailableLabel` (`{0}` = defName), `NeedUnavailableTooltip`. (AC-42)
- [x] Add `GetFilterNeedsTooltip(bool triState)` following the `GetAssignEveryoneTooltip` + `AppendUndefinedSettingTooltip` pattern. (AC-29)
- [x] Add the 8 keys to the 1.6 English `WorkManager_Keyed.xml`. `NeedsTooltip` makes the four AC-37 statements: disabled while any listed need is below its threshold; overrides "ensure worker assigned" and "minimum workers"; pawns without a listed need are unaffected; fast-changing needs (Beauty, Comfort, Food, Rest, Mood) may switch the work type on and off between updates. (AC-37, AC-43)
- [x] Reword the English `EnsureWorkerAssignedOnTooltip` and `MinWorkerNumberTooltip` to add that pawns blocked by the needs filter are never assigned, and `AvailablePawnsTooltip` to add that the needs filter is not applied to the preview. (AC-40, AC-41)
- [x] Add the 8 keys and the 3 rewordings to the 1.6 Russian and ChineseSimplified files as real translations, not English text. (AC-43, AC-49)
- [x] Do not touch any 1.1–1.5 language folder. (AC-44)
- [x] Check that each 1.6 locale has the same 55 `Settings.WorkTypes.*` keys. (AC-43)
- [x] Build with the verified Linux command; 0 warnings, 0 errors. Commit.

### Task 5: Needs settings section and rule summary (ADR-0008) — owner: frontend-dev — AC-27…AC-40
- [x] Add `internal static List<NeedDef> GetAddableNeeds(IEnumerable<NeedDef> all, IReadOnlyCollection<NeedLimit> existing)`: exclude `playerMechsOnly`, `defName == "Authority"` and defNames already present (OrdinalIgnoreCase); include `showOnNeedList == false`; sort by `listPriority` descending, then label (CurrentCulture). (AC-30)
- [x] Add `Settings_WorkTypes.DoRuleNeeds(Rect rect, bool defaultRule, out Rect remRect)` and call it in `DoWorkTypeRule` right after `DoRuleAllowedWorkers`, with a cached `_needsSectionContentHeight` updated on `EventType.Layout`, inside `Sections.DoLabeledSectionBox(…, NeedsLabel, NeedsTooltip, …)`. (AC-27, AC-37)
- [x] State row: `Fields.DoLabeledCheckbox(ref bool)` on the default rule (Off / On) and `Fields.DoLabeledCheckbox(ref rule.FilterNeeds)` on work-type rules (Inherit / Off / On), tooltips from `GetFilterNeedsTooltip`. (AC-28, AC-29)
- [x] Entry rows: `Fields.DoLabeledPercentSlider(…, 0f, 1f, 0.01f, …)` with an `IconButton(TexUI.DismissTex, remove, Common.Resources.Strings.Actions.Delete)`; resolved rows show the need label with the def description as tooltip; unresolved rows show `NeedUnavailableLabel` with the defName and `NeedUnavailableTooltip`, same layout, removable. Remove through a deferred index. (AC-33, AC-34, AC-35)
- [x] Empty state: with state On and no entries, show `NeedsEmptyLabel` via `Labels.DoLabel`. (AC-36)
- [x] Add button: `Buttons.DoActionButton` labelled `Common.Resources.Strings.Actions.Add`, shown only while an addable need exists; it opens a vanilla `FloatMenu` from `GetAddableNeeds(DefProvider.Current.AllDefsListForReading<NeedDef>(), …)` with `def.GetLabel()` options; choosing one adds `new NeedLimit(def)` at 50%. (AC-30, AC-32)
- [x] Option tooltip: put `def.description` in `FloatMenuOption.tooltip`; if that member does not exist in the 1.6 `Assembly-CSharp`, use `mouseoverGuiAction` with `TooltipHandler.TipRegion`. Record which route was used in the commit message. (AC-31)
- [x] Rule summary (`Description`): after the Allowed workers part, a coloured `NeedsLabel` header at indent 1, then at indent 2: Inherit → `WorkTypeRuleUndefinedSectionTooltip`; Off → `WorkTypeRuleDisabledSettingTooltip`; On with no entries → `NeedsEmptyLabel`; On → one line per entry `"<label>: <Threshold.ToStringPercent()>"`, defName for unresolved entries. (AC-39)
- [x] Leave the Available pawns list unchanged; only its tooltip changes (Task 4). Confirm edits apply through the existing `WriteSettings → UpdateSettingsCache → ForceUpdateAssignments` flow without restart. (AC-38, AC-40)
- [x] Use only Common's public API and vanilla widgets; no reflection into Common.
- [x] Build with the verified Linux command; 0 warnings, 0 errors. Commit.

### Task 6: Unit tests for the needs filter (ADR-0006, ADR-0008) — owner: test-engineer — AC-47, AC-48
- [x] Add a test-only `FakeDefProvider : IDefProvider` (dictionary-backed) in `Source/WorkManager.Tests/`; install it into `DefProvider.Current` in per-test `[SetUp]` and restore the previous provider in `[TearDown]`; copy `Assembly-CSharp.dll` into the test output (csproj `CopyRimWorldTestDeps` target) so the fake loads during NUnit discovery; mark such fixtures `[NonParallelizable]`; derive from `StateIsolationTestBase` where default-rule caches are touched.
- [x] Build `NeedDef` instances with object initialisers; fall back to `FormatterServices.GetUninitializedObject` + field assignment if the initialiser needs the game runtime. (Object initialisers work; no fallback needed.)
- [x] `IsNeedBlocked` static seam with a dictionary-backed level lookup: below threshold blocks; equal passes; 0% never blocks; missing need passes; all-null levels (no needs tracker) pass; unresolved def skipped; Off / Inherit / On-with-no-entries never block; any-entry aggregation. (AC-13, AC-14, AC-15, AC-16, AC-17, AC-18, AC-19)
- [x] `ValidateNeedsFilter`: clamp below 0 and above 1; duplicate defNames removed keeping the first (case-insensitive); null and empty-defName entries dropped; default rule Inherit/null normalised to Off; null `FilterNeeds`/`NeedLimits` (older settings) handled. (AC-2, AC-3, AC-4, AC-10)
- [x] `DefaultRules` and the default rule: no rule has needs entries or `FilterNeeds == true`; `CreateRule` gives Inherit and no entries for work-type rules, where testable without `WorkManagerMod.Settings`. (AC-5, AC-6)
- [x] `Combine`: Inherit takes the default rule's state and a copy of its entries (a different list instance); On and Off keep the rule's own state and entries. Extend the existing `WorkTypeAssignmentRuleComparerTests` fixture or add a sibling. (AC-7, AC-8)
- [x] `GetAddableNeeds`: excludes `playerMechsOnly`, Authority and already-present defNames (case-insensitive); keeps `showOnNeedList == false`; orders by `listPriority` descending, then label. (AC-30)
- [x] Use FluentAssertions `.Should()` only; no `Assert.*`; no test depends on execution order; no ASD references in test names or comments.
- [x] Run the suite through the Windows NUnit console route; all 63 pre-existing tests and all new tests pass, none skipped or ignored. Commit. (AC-47, AC-48)

### Task 7: Manual in-game verification spec — owner: test-engineer (author), user (runs in RimWorld) — AC-9, AC-11, AC-12, AC-17, AC-24 + UI and locale checks
- [x] Register `MS-1` in `manual-steps.md` (persistence): configure Mining with Outdoors 60% and Beauty 35% on the Mining rule and an On state on the default rule; save, quit to desktop, restart RimWorld, reopen the settings; states, defNames and thresholds are unchanged, and the filter blocks in the loaded game without a second restart (settings load before defs). Existing pre-sprint settings load with no errors. Hand-edit the settings file to add an entry with a nonexistent defName; it loads, shows as unavailable, and survives another save. (AC-9, AC-10, AC-11, AC-12) — setup, steps, expected result and a `Verification` field per entry.
- [x] Register `MS-2` (live evaluation): a colonist mining under an overhead-mountain roof loses Mining (priority 0) once Outdoors drops below the threshold at the next update, and regains it after its Outdoors need recovers; no errors in the log with pawns that lack a configured need (e.g. an Undergrounder or a guest). (AC-13, AC-16, AC-17, AC-22, AC-23)
- [x] Register `MS-3` (fail-safe): with schedule-aware dedicated workers off, Mining in a dedicated mode with EnsureWorkerAssigned on and MinWorkerNumber ≥ 1, and every candidate need-blocked, no pawn gets the Mining dedicated priority; with the needs filter Off, assignments match the pre-sprint behaviour. (AC-24, AC-25, AC-26)
- [x] Register `MS-4` (settings UI): Needs section placement next to Allowed workers on the default and work-type rules; state control Inherit/Off/On vs Off/On with the undefined-setting tooltip; Add opens the sorted need menu (includes Mood and RoomSize, excludes Authority and already-added needs) with description tooltips; new entry at 50%; slider 0–100% in 1% steps; remove works; empty-state text; Needs tooltip; rule summary Needs part for Inherit/Off/On/empty; Available pawns tooltip and the reworded EnsureWorkerAssigned/MinWorkerNumber tooltips; changes apply without restart. (AC-27…AC-41)
- [x] Register `MS-5` (locales): switch the game to Russian, then ChineseSimplified; every Needs-section string, tooltip and summary line is translated and no raw key is shown. (AC-43, AC-49)
- [x] MS-1 verified by the user (persistence, load before defs, unresolved entry).
- [x] MS-2 verified by the user (live evaluation and recovery).
- [x] MS-3 verified by the user (fail-safe absolute block).
- [x] MS-4 verified by the user (settings UI and summary).
- [x] MS-5 verified by the user (Russian and ChineseSimplified display).

### Task 8: Verification gate — owner: backend-dev — AC-43, AC-44, AC-45, AC-46, AC-47
- [x] Confirm `/tmp/dotnet10/dotnet --version` reports SDK 10; if `/tmp` was cleared, reinstall with `dotnet-install.sh --channel 10.0 --install-dir /tmp/dotnet10` (audit.md Dependencies).
- [x] Check for `jb` on PATH. If present, run `jb cleanupcode` before the build and `jb inspectcode … --build` after lint, and confirm the sarif has no error or warning entries. If absent, record "jb unavailable on this machine, jb-cleanup/jb-inspect skipped" in the task result.
- [x] Run the Release build from the repo root with the verified Linux command; confirm 0 warnings and 0 errors across both projects. (AC-46)
- [x] Commit the rewritten tracked `1.6/Assemblies/LordKuper.WorkManager.{dll,pdb,xml}` as `build: rebuild mod assembly`.
- [x] Run `dotnet format Source/WorkManager.slnx --verify-no-changes` with the same SDK and env vars; confirm clean, or record why it cannot run here.
- [x] Run the full suite through the Windows NUnit console route; confirm all tests pass (63 pre-existing + new), none skipped or ignored. (AC-47)
- [x] Confirm the three 1.6 `WorkManager_Keyed.xml` files have identical `Settings.WorkTypes.*` key sets (55 each) and that `git diff master -- 1.1 1.2 1.3 1.4 1.5` shows no language changes. (AC-43, AC-44)
- [x] Grep `Source/` for ASD references (`AC-`, `ADR`, `PRD`, `Task `, sprint id) in code and comments; none found.
- [x] Verify-only: audit.md "Background research" R-1…R-8 exists and `design/architecture/tech-reference/RimWorld-1.6.md` contains the promoted Needs section. (AC-45)

## Risks
- `/tmp` toolchain is ephemeral: SDK 10 at `/tmp/dotnet10` and the Windows-side NUnit runner can disappear between sessions; Task 8 reinstalls per audit.md Dependencies.
- DefCache latch: any `.Def` / `.Label` read on the load path makes an entry inert until restart. Unit tests cannot catch this; MS-1 (filter blocks in the loaded game without a second restart) is the check.
- Null list after loading older settings: `Scribe_Collections` yields `null`; mitigated by `ValidateNeedsFilter` on `LoadingVars` and tested by simulating null fields.
- `FloatMenuOption.tooltip` is unverified in 1.6; Task 5 carries the `mouseoverGuiAction` fallback.
- Workshop Common 1.6.4.2 is compile-only (`Private=False`); a runtime mismatch with the installed Common would surface as MissingMethod in game, caught only by MS-4.
- Best-effort ChineseSimplified translation (PRD D5); reviewers and MS-5 check for missing or raw keys, not linguistic quality.
- Fast-changing needs can toggle a work type between updates (accepted; tooltip guidance only).

## Dependencies
- Task 2 depends on Task 1 (fields and `NeedLimit`).
- Task 3 depends on Task 2 (`IsNeedBlocked(Pawn)`).
- Task 4 has no code dependencies and can run in parallel with Tasks 1–3.
- Task 5 depends on Tasks 1 and 4 (fields to edit, string members).
- Task 6 depends on Tasks 1, 2 and 5 (seams under test, including `GetAddableNeeds`).
- Task 7 authoring can start after Task 5; the user's in-game runs depend on Tasks 1–5 being built.
- Task 8 depends on all prior tasks.

## Out of scope
- Any change to `LordKuper.Common`, including a needs dimension in `PawnFilter`.
- Shipped needs rules for Mining or any work type; lowering priority instead of disabling; hysteresis or latching.
- Restricting the fail-safe to allowed workers (G-3 option (b)) and fixing the existing `EnsureWorkerAssigned` tooltip inaccuracy about the allowed-workers filter (follow-ups).
- Applying the needs filter to the Available pawns preview or to `DedicatedWorkerMode.PawnCount` target counting.
- Legacy 1.1–1.5 builds and locales; per-pawn exemption; per-need threshold hints on the slider.
- Editing `commands.yaml` for the Linux/WSL environment (workflow config, owned by `/asd-init`).
- The concept.html follow-ups from design-promote ("Guaranteed coverage" pillar wording, `delegates_to` path).
- Modifying ASD workflow infrastructure (`.asd/rules/`, `.asd/templates/`, `.claude/`, `CLAUDE.md`).
