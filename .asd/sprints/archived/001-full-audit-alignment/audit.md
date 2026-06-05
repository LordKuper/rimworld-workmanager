---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

## Scope reference
[sprint.md](./sprint.md)

Full audit of WorkManager — code AND docs — reconciled against four reference standards: (1) `.asd` rules (`rules/*` + `custom-*-rules.md`), (2) tech stack (`design/architecture/stack.html`), (3) `LordKuper.Common` upstream contract, (4) code conventions (code-style, nullable, XML docs). This file's docs-side sections are owned by asd-ba; code-side sections are owned by asd-architect.

## Touched areas

Documentation areas this sprint touches (docs side):

- `README.md`: project root README — drifts from current metadata/stack; in scope for alignment.
- `design/product/concept.html`: reverse-engineered concept — verify it still matches current code/About.xml.
- `design/architecture/stack.html`: reverse-engineered stack — verify it matches `WorkManager.csproj` / `WorkManager.Tests.csproj` after audit code changes.
- `design/architecture/tech-reference/*.md`: six tech-reference docs — verify they cover every chosen tech and stay version-accurate after any dependency change.
- `About/About.xml`, `1.6/Languages/**`, `1.*/Defs/**`: mod-shipped metadata and keyed text — checked as source-of-truth inputs that docs must reflect; About `<description>` consistency is a docs concern.
- `design/architecture/adr/`: ADR directory — absent; audit decisions of this sprint may need recording.
- `.asd/project/decisions-log.md`: append-only decision chronology — PM-owned, surfaced here for traceability only.

Code-side touched areas (`Source/**`, build/config) enumerated by asd-architect:

- `Source/packages/Lib.Harmony.2.3.6/`: stale vendored Harmony 2.3.6 package tree (DLLs, nupkg, pdbs) still present though both projects use a `Lib.Harmony` 2.4.2 `PackageReference`. No project references it; it is dead weight that contradicts the pinned stack. Removal target.
- `Source/WorkManager/Patches/**` (5 files): Harmony patches — null-safety of `WorkManagerGameComponent.Instance` access is inconsistent (see Gaps/Risks).
- `Source/WorkManager/PawnColumnWorkers/{AutoWorkPriorities,AutoWorkSchedule}.cs`: dereference `Instance` without null guard.
- `Source/WorkManager/Cache/PawnCache.cs`: dereferences `Instance` without null guard (`IsAllowedWorker`, `IsManagedWork`, `Update`).
- `Source/WorkManager/WorkPriorityUpdater.cs`, `ScheduleUpdater.cs`: core assignment/schedule logic; `Instance` dereferenced unguarded in `WorkPriorityUpdater`.
- `Source/WorkManager/Settings/Settings_Schedules.cs`: two hardcoded English UI strings (`"Work shift #{i + 1}"`) bypass the keyed-localization surface used everywhere else.
- `Source/WorkManager.Tests/WorkShiftTests.cs`: lone placeholder test using `Assert.Pass()` (NUnit assertion) — no FluentAssertions coverage of any production behavior yet.
- `Source/WorkManager/WorkManager.csproj`, `WorkManager.Tests.csproj`, `Source/Directory.Build.props`, `Source/WorkManager.slnx`: build/config — verified against the stack (see Existing implementation found).

## Existing docs found

Inventory of all in-repo documentation, excluding read-only ASD infrastructure (`.asd/rules/`, `.asd/templates/`, `.claude/`) and machine config. Language / format / freshness / relevance to the four standards noted.

| # | Path | Type | Lang | Format | Status / freshness | Relevance |
|---|---|---|---|---|---|---|
| 1 | [README.md](../../../README.md) | Project README | en | Markdown | STALE — version badges list RimWorld 1.1–1.5 (missing 1.6); thin description ("automates assigning work priorities") omits scheduling, dependencies, stack | Std 2 (stack), Std 3 (Common dep) — README must reflect 1.1–1.6 range and LordKuper.Common dependency |
| 2 | [design/product/concept.html](../../../design/product/concept.html) | Concept (vision/users/value) | en | HTML (shell-wrapped) | CURRENT — created 2026-06-04, status approved, provenance reverse-engineered from About/About.xml; correctly states 1.1–1.6 and Common-on-1.6 | Baseline product doc; verify still matches code after audit |
| 3 | [design/architecture/stack.html](../../../design/architecture/stack.html) | Tech stack | en | HTML (shell-wrapped) | CURRENT — created 2026-06-04, approved, reverse-engineered from WorkManager.csproj; documents net472, C# 14, Harmony 2.4.2, Common 1.6, upgraded test stack | Std 2 (stack SSoT), Std 4 (zero-warning/nullable/XML-doc build discipline captured) |
| 4 | [tech-reference/Lib.Harmony-2.4.2.md](../../../design/architecture/tech-reference/Lib.Harmony-2.4.2.md) | Tech reference | en | Markdown | CURRENT (2026-06-04) | Std 2 — mandatory per-tech reference |
| 5 | [tech-reference/LordKuper.Common-1.6.md](../../../design/architecture/tech-reference/LordKuper.Common-1.6.md) | Tech reference | en | Markdown | CURRENT (2026-06-04) | Std 2 + Std 3 — upstream contract reference |
| 6 | [tech-reference/FluentAssertions-7.2.2.md](../../../design/architecture/tech-reference/FluentAssertions-7.2.2.md) | Tech reference | en | Markdown | CURRENT (2026-06-04) | Std 2 + Std 4 — test assertion lib (7.x license guard) |
| 7 | [tech-reference/NUnit-4.6.1.md](../../../design/architecture/tech-reference/NUnit-4.6.1.md) | Tech reference | en | Markdown | CURRENT (2026-06-04) | Std 2 + Std 4 — test framework |
| 8 | [tech-reference/NUnit3TestAdapter-6.2.0.md](../../../design/architecture/tech-reference/NUnit3TestAdapter-6.2.0.md) | Tech reference | en | Markdown | CURRENT (2026-06-04) | Std 2 — test adapter |
| 9 | [tech-reference/Microsoft.NET.Test.Sdk-18.6.0.md](../../../design/architecture/tech-reference/Microsoft.NET.Test.Sdk-18.6.0.md) | Tech reference | en | Markdown | CURRENT (2026-06-04) | Std 2 — test host |
| 10 | [About/About.xml](../../../About/About.xml) | Mod metadata | en | XML | CURRENT — supportedVersions 1.1–1.6, modDependenciesByVersion (Common on 1.6), loadAfter list | Source-of-truth input; README + concept must agree with it |
| 11 | `1.6/Languages/{English,Russian,ChineseSimplified}/Keyed/WorkManager_Keyed.xml` | Keyed UI text | en/ru/zh-Hans | XML | Present for 1.6 (3 locales) | User-facing strings; not ASD docs but a localization surface — coverage parity worth noting |
| 12 | `1.1–1.5/Languages/English/Keyed/WorkManager_Keyed.xml` | Keyed UI text | en | XML | Present (English only for legacy versions) | Localization surface for legacy game versions |
| 13 | XML doc comments across `Source/WorkManager/**` (33 files) | API doc comments | en | C# `///` | PRESENT — ~1600 doc-comment lines, every source file carries some; depth/accuracy per public member is a code-side check | Std 4 — XML documentation on public surface (asd-architect verifies completeness) |
| 14 | [Source/packages/Lib.Harmony.2.3.6/README.md](../../../Source/packages/Lib.Harmony.2.3.6/README.md) | Third-party package README | en | Markdown | VENDORED + STALE — Harmony 2.3.6 package dir lingers while stack pins 2.4.2 (PackageReference, no packages/ needed) | Std 2 (stack mismatch) — not project doc; flagged so asd-architect removes the stale vendored package dir |

Notes:
- No `docs/` directory, no wiki/Confluence export, no `CHANGELOG`, no `CONTRIBUTING`, no `LICENSE` file (license stated inline in README + About only).
- No PRD (`design/product/requirements.html`) exists — expected for a brownfield project with no prior ASD design sprint; not required by this audit sprint (see Documentation migration plan).
- No ADR directory/files (`design/architecture/adr/`) exist yet.
- No UX docs (`design/ux/`) exist — WorkManager UI is IMGUI mod-settings + a work-tab column; UX docs are out of scope for this audit.

## Existing implementation found

Source tree: `Source/WorkManager/` (33 production `.cs` files, root namespace `LordKuper.WorkManager`, assembly `LordKuper.WorkManager`) + `Source/WorkManager.Tests/` (1 test file). Build output `1.6/Assemblies/`. The mod is a mature, complete brownfield implementation; the audit is alignment, not greenfield.

Subsystems / responsibilities found (no formal C4; decomposition disabled):

- **Mod bootstrap** — `WorkManagerMod` (`Mod`): logs version, loads `Settings`, applies `Harmony.PatchAll`, initializes the three compatibility layers once (`_isPatched` guard). `Resources` (strings via `.Translate()` + `[StaticConstructorOnStartup]` textures).
- **Game state** — `WorkManagerGameComponent` (`GameComponent`, singleton via `Instance`): persists disabled pawns/work-types/schedules via `Scribe_*`; builds combined rules (`SortedSet` + O(1) dict), assign-everyone map, dedicated-work-types set; `Validate()` prunes destroyed pawns / missing defs.
- **Priority engine** — `WorkPriorityUpdater` (`MapComponent`): tick-gated (`& 0x3F`, frequency in days) recompute. Pipeline: `AssignCommonWork` → dedicated-workers OR by-skill → passion → learning-rate → leftover → idle. Schedule-aware dedicated-worker selection (`TryAssignDedicatedWorkersBySchedule`) with day-level fallback. Deterministic tie-breaks on `thingIDNumber`/`defName`. Wrapped in try/catch logging to project `Logger`.
- **Schedule engine** — `ScheduleUpdater` (`MapComponent`): assigns timetable per shift, splitting regular vs NightOwl groups, coverage + lover-clustering + evenness scoring. Try/catch guarded.
- **Caches** — `PawnCache` (per-tick refresh) composes `PawnSkillCache` + `PawnWorkCache` (both `LordKuper.Common.Cache.TimedCache`). Per-call memo dictionaries.
- **Settings** — partial `Settings : ModSettings` across 6 files (WorkPriorities / WorkTypes / Schedules / Version) + `WorkTypeAssignmentRule : DefCache<WorkTypeDef>`, `DedicatedWorkerSettings`, `WorkShift`, enums. Versioned schema (`CurrentVersion = 1`), clamped validation, default rule sets.
- **UI** — vanilla pawn columns (`AutoWorkPriorities`/`AutoWorkSchedule`) + Harmony postfix/prefix patches for work tab, work-priority column header, and work boxes; built on `LordKuper.Common.UI` helpers.
- **Compatibility** — `WorkTab`, `PriorityMaster`, `MoreThanCapable` (reflection via `AccessTools`/`Traverse`, delegate caching, graceful failure to logged error), plus `Vse` (VanillaSkillsExpanded) reads in settings.

Standards alignment confirmed OK (no action needed):

- **Stack** — `WorkManager.csproj`: `net472`; `Lib.Harmony` 2.4.2 with `PrivateAssets=all` + `ExcludeAssets=runtime` (compile-only); `LordKuper.Common` `<Private>False</Private>` file reference resolved via `$(LordKuperCommonAssembliesDir)`; RimWorld/Unity refs via `$(RimWorldManagedDir)` with `<Private>False</Private>`; `Nullable enable`; `GenerateDocumentationFile=True`; `WarningLevel 9999` + `TreatWarningsAsErrors=True` in both Debug and Release. All match `stack.html`.
- **Test stack** — `WorkManager.Tests.csproj`: `net472`, `Nullable enable`, NUnit 4.6.1, NUnit3TestAdapter 6.2.0, Microsoft.NET.Test.Sdk 18.6.0, FluentAssertions **7.2.2** (correct Apache-2.0 pin, not 8.x), global `<Using>` for both `NUnit.Framework` and `FluentAssertions`. Matches rules.
- **Common contract** — only public surface consumed (`TimedCache`, `DefCache<T>`, `PawnFilter`, `PawnHelper`, `PassionHelper`, `MathHelper`, `Layout`/`Buttons`/`IconButton`, `RimWorldTime`, shared `Resources.Strings`). No fork/reimplementation observed; matches the `LordKuper.Common-1.6.md` reference. Reflection is used only for third-party mods (WorkTab/PriorityMaster/MoreThanCapable), never against Common.
- **Conventions** — XML doc comments present across the public surface; only one comment-pragma family in use is attribute-based (`[SuppressMessage(...)]` with real justifications, `[UsedImplicitly]`); no `#pragma warning disable` / `// ReSharper disable` comment suppressions found; no in-code references to ASD documents. Guard clauses + `ArgumentNullException` at boundaries are consistent in the cache/rule layer.

## Gaps

- **G1 — No real test coverage.** `WorkShiftTests` is a single `Assert.Pass()` placeholder using an NUnit assertion. Custom-coding-rules require FluentAssertions 7.x and forbid `Assert.*`; code-style requires every acceptance criterion covered and an 80% floor. No isolation base class (`[SetUp]`/`[TearDown]` snapshot/restore), no global `[SetUpFixture]` registering the RimWorld `AssemblyResolve` handler — both mandated before any RimWorld-typed test loads. Pure-logic units that are trivially testable without a live game (`WorkShift` hour mapping/validation, `WorkTypeAssignmentRuleComparer`, `DedicatedWorkerSettings.Combine`/`Validate` clamping, `WorkTypeAssignmentRule.Combine`, `PassionHelper.GetPassionScore` normalization, `GetTargetWorkersCount` for non-PawnCount modes) have zero coverage.
- **G2 — Missing tech-reference docs.** Six references exist, but two adopted technologies have none: **RimWorld 1.6 game API** (`Assembly-CSharp` + Unity modules — the largest external surface: `MapComponent`, `GameComponent`, `Mod`, `Scribe_*`, `DefDatabase`, `Pawn`/`WorkTypeDef`/`TimeAssignmentDef`, `HarmonyLib` via game) and **.NET Framework 4.7.2** (the TFM). The RimWorld reference was explicitly deferred at init (decisions-log 2026-06-04); the .NET 4.7.2 gap is unrecorded. Rule: no tech adopted without a reference. Record both; plan phase decides whether this sprint closes them.
- **G3 — Hardcoded English UI strings.** `Settings_Schedules.cs` lines 148/201 emit `"Work shift #{i + 1}"` directly to a `FloatMenuOption`, bypassing the `.Translate()` keyed surface every other label uses — untranslatable in ru/zh-Hans and inconsistent with code-style "no hardcoded UI values". (The `"Anything"`/`"NightOwl"` literals are RimWorld def names, not user-facing text — those are correct as-is.)
- **G4 — Inconsistent `Instance` null handling.** `WorkManagerGameComponent.Instance` is typed `null!` and set in the ctor. Some call sites guard (`WorkManagerMod` uses `?.`; `MainTabWindowWorkPatch`, `WorkTabPatch`, `ScheduleUpdater.UpdateNow`, `WidgetsWorkPatch` check `== null`), but many dereference unguarded (`WidgetsWorkPatch.DrawWorkBoxForPrefix` calls `GetPawnWorkTypeEnabled` before the prefix's own guard reads the same field at line 58 with no null check; `PawnColumnWorkerWorkPriorityPatch.DoHeaderPostfix`; `AutoWorkPriorities`/`AutoWorkSchedule.DoCell`; `PawnCache`; `WorkPriorityUpdater`). The mixed pattern is the gap — decide one contract (guaranteed-non-null after component init, or guard everywhere) and apply it uniformly.

## Risks

- **`Instance` NRE on early UI / no-game paths**: impact=potential `NullReferenceException` from a Harmony UI patch or pawn column if `Instance` is dereferenced before a `Game` (and thus `WorkManagerGameComponent`) exists — UI patches can run on screens without an active game; mitigation=resolve G4 with a single null-handling contract and guard the UI/patch entry points consistently.
- **Stale vendored Harmony 2.3.6 shipped/committed**: impact=2.3.6 binaries in `Source/packages/` diverge from the 2.4.2 pin, risk of confusion or an accidental wrong-version reference; not currently referenced by either csproj, so no build impact; mitigation=delete the `Source/packages/Lib.Harmony.2.3.6/` tree (record as an audit decision; consider an ADR only if its presence was deliberate, which the code gives no sign of).
- **Reflection-based compat brittleness**: impact=WorkTab/PriorityMaster/MoreThanCapable integration binds to other mods' private members by string (`AccessTools.TypeByName`/`Method`/`Field`, `Traverse`); a third-party update silently breaks a delegate; mitigation already present (each `Initialize` is try/catch and degrades to logged error + disabled flag) — low residual risk, no change required, noted for awareness.
- **Localization parity**: impact=legacy game folders (1.1–1.5) ship English-only keyed text and the two hardcoded strings (G3) are never localized; mitigation=fix G3; locale-file parity for legacy versions is a docs/localization concern (asd-ba side), not blocking.
- **No automated regression net for the assignment/schedule engines**: impact=the most complex logic (multi-pass priority assignment, schedule scoring) has no tests, so audit refactors (e.g. G4 null contract) risk silent behavior change; mitigation=add the pure-logic unit tests in G1 before/with any engine touch (verification-driven per code-style §17).

## Related open stubs

No `.asd/project/stubs.md` registry exists, and no `// TODO`/`// FIXME` stub markers were found in `Source/**`. No related open stubs.

## Documentation migration plan

This sprint's docs work: bring existing documentation into alignment with the four standards and the audited code. No external-format documents need migrating into `design/` (everything user-facing is already HTML or is mod-shipped metadata that stays where RimWorld expects it). The work below is alignment/remediation of existing docs, not format migration.

Decomposition is DISABLED — persistent docs are flat project-wide (`design/product/{concept,requirements}.html`, `design/architecture/{stack,api}.html`, `adr/adr-NNNN-*.html`), no per-subsystem layout.

| # | Source (path) | Format | Proposed target in `design/` | Type | Notes |
|---|---|---|---|---|---|
| 1 | `README.md` | Markdown | — (stays at repo root) | alignment | Update version badges to include RimWorld 1.6; expand Description to mention schedule management and the Harmony + LordKuper.Common dependencies; align with `concept.html` value proposition. README is repo-facing, NOT a `design/` doc — do not migrate, just correct drift. SSoT: link to About.xml / concept, do not duplicate the full feature list. |
| 2 | `design/product/concept.html` | HTML | `design/product/concept.html` (in place) | alignment | Re-verify against current `Source/WorkManager/**` and About.xml after code audit; bump `updated` only if content changes. Currently accurate (1.1–1.6, Common-on-1.6) — likely no change. |
| 3 | `design/architecture/stack.html` | HTML | `design/architecture/stack.html` (in place) | alignment | Re-verify against `WorkManager.csproj` / `WorkManager.Tests.csproj` AFTER asd-architect's code/build remediation. If the architect changes any dependency, TFM, or build property, stack.html + the matching tech-reference must be updated in lockstep (SSoT). The vendored `Lib.Harmony.2.3.6` package dir (doc #14) contradicts the documented 2.4.2 pin — flag for architect removal so docs and code agree. |
| 4 | `design/architecture/tech-reference/*.md` | Markdown | in place | alignment | Mandatory per-tech reference coverage rule: one `<tech>-<version>.md` per chosen tech. Six exist. After audit, ensure no chosen tech is missing a reference and no reference points to a version the code no longer uses. RimWorld 1.6 game-API reference was explicitly deferred at init (decisions-log 2026-06-04) — decide in plan phase whether this audit closes that gap or keeps it deferred. |
| 5 | `About/About.xml` `<description>` | XML | — (mod metadata, stays in place) | alignment | `<description>` ("Automatic work priority management mod") omits scheduling; consistency with README + concept value proposition is a docs concern. Low priority — Steam/in-game text, edit only if other description text is touched. |
| 6 | — (new) `design/architecture/adr/adr-NNNN-*.html` | HTML | `design/architecture/adr/` | new (conditional) | No ADRs exist. If the audit makes a non-trivial, contestable decision (e.g. removing the vendored packages dir, closing/keeping the deferred RimWorld API reference, any stack change), record it as an ADR via the design phase. ADR authorship is asd-architect's; flagged here for traceability. Routine fixes need no ADR. |

No reverse-engineered or migrated PRD drafts are produced for this audit sprint: scope is broad alignment, not a single feature, so an inventory + alignment plan is sufficient (per task instruction 4). A full project PRD (`design/product/requirements.html`) remains a future-sprint candidate, not this sprint's deliverable — deferred to design-promote/a later requirements sprint.
