---
responsibility:
  owns: append-only chronology of approved decisions across project lifetime
  excludes: sprint state, code review notes, custom rules
  delegates_to: .asd/sprints/ (sprint state), reviews/ (review notes), custom-common-rules.md / custom-design-rules.md / custom-coding-rules.md (rules)
---

# Decisions Log

Append-only. Never edited or removed. New entries appended below.

## Entry format

```markdown
## YYYY-MM-DD — <one-line summary>

- **Decision**: <what was decided>
- **Rationale**: <why>
- **Affected docs**: <links> (optional)
```

## Entries

<!-- entries appended below this line -->

## 2026-06-04 — ASD initialized for WorkManager

- **Decision**: ASD workflow initialized. mode=brownfield, decomposition=disabled, diagram_tool=n/a, OS=windows. language chat=ru, docs=en. backward_compat=none. external_review=enabled (codex 0.136.0). git base_branch=master, gh_enabled=true, auto_pr=true.
- **Rationale**: Existing RimWorld mod (C#/.NET, net472, NUnit tests). Single-project mod — subsystem decomposition unnecessary. Custom rules inherited from the upstream `LordKuper.Common` library and adapted to WorkManager's actual setup (consumes Common.dll; tests use NUnit Assert, no FluentAssertions/jb tooling).
- **Affected docs**: .asd/project/config.yaml, commands.yaml, custom-common-rules.md, custom-design-rules.md, custom-coding-rules.md, CLAUDE.md

## 2026-06-04 — Adopt parent FluentAssertions + jb tooling standards

- **Decision**: WorkManager adopts the parent `LordKuper.Common` standards in full: tests use FluentAssertions 7.x (Apache-2.0, never 8.x) instead of NUnit `Assert`; lint/build flow runs `jb-cleanup` before build and `jb-inspect` after lint (sarif must be error/warning-clean). Added `jb-cleanup` and `jb-inspect` to commands.yaml.
- **Rationale**: User directive — these must be used to the same extent as in the parent library. Existing `WorkManager.Tests` (NUnit Assert placeholder, no FA package, jb not wired) is treated as not-yet-migrated; the FA 7.x package is added on first real test.
- **Affected docs**: .asd/project/commands.yaml, custom-coding-rules.md, custom-common-rules.md

## 2026-06-04 — Concept reverse-engineered from brownfield

- **Decision**: `design/product/concept.html` created via /asd-concept variant D (brownfield extraction). Sections: vision, target-users, value-proposition (required) + pillars, anti-pillars, constraints (optional). Optional sections core-identity, success-metrics, unique-hook omitted. Supported RimWorld range fixed at 1.1–1.6 (per About.xml/code, not README). Frontmatter provenance=reverse-engineered, source=About/About.xml.
- **Rationale**: Existing RimWorld Work Manager mod had no concept doc; reconstructed from About.xml, README.md and Source/WorkManager code. User locked each required section (target-users revised to drop third-party mod names) and selected the optional set.
- **Affected docs**: design/product/concept.html

## 2026-06-04 — Tech stack reverse-engineered + test stack upgraded

- **Decision**: `design/architecture/stack.html` created via /asd-stack variant D (brownfield). Stack: C# 14 (LangVersion=latest via SDK 10.0.300) on net472; prod deps Lib.Harmony 2.4.2 + RimWorld Assembly-CSharp/Unity modules + LordKuper.Common (all reference/compile-only). Test stack UPGRADED and applied to WorkManager.Tests.csproj now (user-authorized direct edit, outside a sprint): NUnit 4.2.2→4.6.1, NUnit3TestAdapter 4.6.0→6.2.0, Microsoft.NET.Test.Sdk 17.11.1→18.6.0, and FluentAssertions 7.2.2 added (Apache-2.0, 8.x forbidden) with global Usings for NUnit.Framework + FluentAssertions. Sections included: languages, frameworks, runtime-infrastructure, tooling, constraints, architecture-principles; layers-diagram omitted. Six tech-reference docs created (Lib.Harmony-2.4.2, NUnit-4.6.1, NUnit3TestAdapter-6.2.0, Microsoft.NET.Test.Sdk-18.6.0, FluentAssertions-7.2.2, LordKuper.Common-1.6); RimWorld-1.6 game-API reference deferred.
- **Rationale**: Existing mod had no stack doc; reconstructed from manifests. User opted to adopt parent-library standards in full and upgrade the lagging test stack immediately. Major bumps (adapter 4→6, test-sdk 17→18) verified net472-supported via changelog WebFetch; `dotnet test` passes (1/1) after the bump. Knowledge-risk MEDIUM for test packages (latest releases post Jan-2026 cutoff) — mitigated by tech-reference docs.
- **Affected docs**: design/architecture/stack.html, design/architecture/tech-reference/*.md, Source/WorkManager.Tests/WorkManager.Tests.csproj, Source/WorkManager.Tests/WorkShiftTests.cs

## 2026-06-05 — Impl fix for iter-01: findings resolved

- **Decision**: impl fix for iter-01: findings resolved (2026-06-05). [medium] IsInitialized now checks Current.Game != null (stale-Instance/quit-to-menu fixed, AC-12); [medium] StateIsolationTestBase uses typeof + fails loudly on missing field (AC-1 isolation restored); [medium] ASD-artifact refs removed from test doc-comments; [low] added AC-2 valid-mapping, AC-4 Validate clamping (x4), AC-3 ordering test clarified, GetTargetWorkersCount specific exception, reverted stray zh quote-glyph. Build 0/0; 60 tests green. Returning to impl-review.

## 2026-06-05 — Impl-review iter 02: fix-all routed to impl fix mode

- **Decision**: impl-review iter 02: documentation CONCERNS + external FAIL → user chose fix-all → impl fix mode. Findings: [medium] AC-4 Validate clamping tests are placeholders (never call Validate); [medium] AC-5 PassionHelper fixture empty (0 tests); [medium] AC-2 valid hour→assignment mapping not exercised; [medium] AC-3 only defName fallback tested, not skill-count/priority ordering; [medium] ADR-0001 persistent doc stale (says 'Instance is not null', as-built is 'Current.Game != null && Instance is not null').

## 2026-06-05 — Impl fix for iter-02: findings resolved

- **Decision**: impl fix for iter-02: findings resolved (2026-06-05). AC-4 Validate clamping tests made genuine (call Validate, assert clamping); AC-3 tie-breaker + Combine tests added; AC-2 GetTimeAssignment + AC-5 GetPassionScore confirmed game-context-only → MS-3/MS-2 registered and user-verified PASS in-game; ADR-0001 persistent doc synced to as-built (Current.Game != null && Instance is not null). Build 0/0; 64 tests green. Returning to impl-review.

## 2026-06-05 — Impl-review iter 03: external FAIL + implementation CONCERNS → impl fix mode

- **Decision**: impl-review iter 03: external FAIL + implementation CONCERNS → impl fix mode. HIGH finding: PassionHelperTests.cs:42 GetPassionScore_UnknownPassion_ReturnsFallback uses Assert.Pass() placeholder — violates AC-1 (no Assert.*) and AC-18 (fake-green test). Fix: remove placeholder or make it a genuine FluentAssertions assertion (AC-5 game-context behavior already covered by user-verified MS-2). UI reviewer ABORTed in error (no UI artifacts by approved decision) — to be re-run correctly. iter-02 mediums AC-2/AC-3/AC-4 confirmed genuinely resolved.

## 2026-06-05 — Impl fix for iter-03: HIGH finding resolved

- **Decision**: impl fix for iter-03: HIGH finding resolved (2026-06-05) — deleted PassionHelperTests.cs Assert.Pass() placeholder; AC-1/AC-18 now hold; AC-5 game-context behavior covered by user-verified MS-2. Build 0/0; 63 tests green. Returning to impl-review.

## 2026-06-05 — Impl-review iter 04: testing FAIL overridden → impl fix mode

- **Decision**: impl-review iter 04: testing FAIL overridden by user (AC-5 accepted game-context/MS-2; unit test infeasible). simplification CONCERNS (inline typeof wrappers in StateIsolationTestBase) → impl fix mode. quality/implementation/ui/documentation/performance/external all APPROVE.

## 2026-06-05 — Impl fix for iter-04: simplification resolved

- **Decision**: impl fix for iter-04: simplification resolved (2026-06-05) — inlined typeof wrappers in StateIsolationTestBase, removed restating comments; no behavior change. Build 0/0; 63 tests green. Returning to impl-review.

## 2026-06-05 — Impl-review iter 05: APPROVE — DoD met

- **Decision**: impl-review iter 05: APPROVE — DoD met (all 8 reviewers APPROVE: quality, implementation, testing, ui, simplification, documentation, performance, external). Sprint ready for PR. Cycle summary: iter-01 CONCERNS→fix, iter-02 FAIL(external test gaps)→fix-all, iter-03 FAIL(Assert.Pass placeholder)→fix, iter-04 testing FAIL overridden(AC-5 manual)+simplification→fix, iter-05 all APPROVE.

## 2026-06-05 — Sprint 001-full-audit-alignment completed + archived

- **Decision**: sprint 001-full-audit-alignment completed + archived 2026-06-05; PR: https://github.com/LordKuper/rimworld-workmanager/pull/20. DoD met: build 0/0, 63 tests green, lint clean, impl-review iter-05 all APPROVE; MS-1/2/3 user-verified.


## 2026-09-29 — Sprint 002-work-type-needs-filters scope approved

- **Decision**: Scope for sprint 002-work-type-needs-filters approved (AskUserQuestion, relayed by orchestrator). Add a WorkManager-local needs filter to `WorkTypeAssignmentRule`, next to `AllowedWorkers`: any `NeedDef` can be added per work type, each with a 0–100% threshold; a pawn below any configured threshold gets the work type disabled (priority 0), re-checked on every WorkManager update; UI-editable, saved/loaded, merged with the default rule. Needs block is absolute (`EnsureWorkerAssigned`/`MinWorkerNumber` do not override it). No shipped default rules (user configures Mining with Outdoors + Beauty). Research of RimWorld 1.6 needs mechanics and related mods is required as design background. Slug kept: 002-work-type-needs-filters.
- **Rationale**: User answers: Q1 "WorkManager-local (Recommended)" (LordKuper.Common is not editable from this repo); Q2 "Below threshold (Recommended)"; Q3 "Any need, no defaults"; Q4 "Disable work type (Recommended)"; Q5 "Needs block absolute"; Q7 keep slug; approval "Approve".
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/sprint.md, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 audit approved

- **Decision**: `.asd/sprints/002-work-type-needs-filters/audit.md` approved by the user (AskUserQuestion, relayed by orchestrator; answer "Approve"). Sprint 002 audit phase exit criteria met.
- **Rationale**: Merged BA + architect audit covers RimWorld 1.6 needs mechanics, the current `WorkTypeAssignmentRule`/`IsAllowedWorker` evaluation path, and gaps G-1..G-n for the needs filter.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/audit.md

## 2026-09-29 — Sprint 002 G-3: dedicated-worker fail-safe excludes need-blocked pawns only

- **Decision**: Resolve G-3 with option (a). The `AssignDedicatedWorkersForDay` fail-safe (`WorkPriorityUpdater.cs` l.279-288) must skip pawns that fail the needs filter. Its existing behaviour for `AllowedWorkers` stays unchanged. User answer: "Exclude need-blocked only (Recommended)".
- **Rationale**: This enforces the absolute needs block (scope decision 5) with the smallest blast radius, and it keeps the AC "existing default rules behave exactly as before". Option (b), restricting the fail-safe to allowed workers, fixes the latent `AllowedWorkers` bypass and is recorded as a follow-up outside this sprint.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/audit.md

## 2026-09-29 — Sprint 002: needs filter uses a stateless threshold (no hysteresis)

- **Decision**: No hysteresis. The needs filter is a stateless per-need threshold check (level < threshold → blocked), re-evaluated on each WorkManager update.
- **Rationale**: The user asked to "Look at how other similar filters work - add hysteresis if others do". Verification found that no similar existing filter uses hysteresis, so the needs filter follows the existing stateless pattern.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/sprint.md

## 2026-09-29 — Sprint 002 design phase completed: PRD + 3 ADRs approved, UX/design-system skipped

- **Decision**: Design phase for sprint 002-work-type-needs-filters is complete, and the sprint advances to design-review. Drafts produced:
  - `design/prd.html`: 5 goals, 8 stories, 49 ACs, 9 non-goals. User approved D1–D9 at the recommended options. D2: the preview stays unchanged and gets a tooltip. D5: the agent translates ru/zh. D7: the default threshold is 50%. AC-30 was amended to use Common's shared "Add" string.
  - `design/adr.html`: ADR-0006 covers the data model and evaluation, with a scoped exception to the PawnFilter convention. ADR-0007 makes the fail-safe exclude need-blocked pawns. ADR-0008 builds the settings UI from Common's public API only and covers localization. User answers: Complication Approval CA-1..CA-5 "Approve all (Recommended)"; "Add" label "Reuse "Add" (Recommended)"; ADR split "Approve, 3 ADRs (Recommended)".
  - UX-spec and design-system: SKIPPED.
  - No C4 changes (subsystem decomposition is disabled). No new tech-reference.
- **Rationale**: The user answered "clone the lordkuper.common repo in a separate directory and look at it", so LordKuper.Common was inspected. It is cloned at ../rimworld-common. Common's decisions-log shows that its sprint 004, an IMGUI widget sprint, skipped UX-spec and design-system with the reason "IMGUI library, no web UI". This sprint follows that precedent. All approvals were collected via AskUserQuestion and relayed by the orchestrator.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/design/prd.html, .asd/sprints/002-work-type-needs-filters/design/adr.html, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 design-review iter 01: FAIL → user-approved autofix

- **Decision**: Design-review iter 01 verdicts: documentation FAIL, simplification CONCERNS, external APPROVE, ui N/A (no ux-spec). The user approved autofix of all findings; the answer was "Apply all fixes (Recommended)" via AskUserQuestion, relayed by the orchestrator. Fixes applied:
  - ADR: the build/test appendix was removed and replaced by a link to the audit.md Dependencies section (high, SSoT finding). A duplicate shell comment was removed. ADR-0006 now uses a single state check in the static `IsNeedBlocked` seam (simplification, medium).
  - PRD: re-wrapped in the unmodified shell. AC-2, AC-3 and AC-27 are now traced to user stories. AC-45 now points to audit.md R-1..R-8.
  - Documentation finding #6 (missing approval entries) needed no change. The 2026-09-29 "design phase completed" entry already resolves it; the reviewer read the log before that entry was written.
  - Design-review advances to iteration 2.
- **Rationale**: The fixes resolve the high SSoT finding and the medium simplification finding without changing scope or approved design decisions.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/design/adr.html, .asd/sprints/002-work-type-needs-filters/design/prd.html, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 design-review iter 02: APPROVE — DoD met

- **Decision**: Sprint 002 design-review iter 02: APPROVE — DoD met. Verdicts: documentation APPROVE, simplification APPROVE, external APPROVE, ui N/A (no ux-spec). There were 0 findings at the medium severity floor. The sprint advances to design-promote.
- **Rationale**: Iteration trace:
  - Iter-01: documentation FAIL on the ADR build/test appendix (SSoT). Simplification CONCERNS on the duplicated state check in ADR-0006. External APPROVE. Both findings were autofixed with user approval.
  - Iter-02: every reviewer returned APPROVE with 0 findings at the medium floor, so the DoD is met.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/design/adr.html, .asd/sprints/002-work-type-needs-filters/design/prd.html, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 design-promote confirmed: 3 ADRs, 2 tech-reference updates, new requirements.html

- **Decision**: The final design-promote mutation for sprint 002-work-type-needs-filters is confirmed. User answer via AskUserQuestion, relayed by the orchestrator: "Confirm (Recommended)". Promoted to persistent `design/`:
  - New, status accepted: `design/architecture/adr/adr-0006-needs-filter-model-and-evaluation.html` (NeedLimit entry type, tri-state `FilterNeeds` + `NeedLimits`, static `IsNeedBlocked` seam, scoped exception to the PawnFilter eligibility convention), `adr-0007-needs-block-absolute-in-failsafe.html` (the `AssignDedicatedWorkersForDay` fail-safe excludes need-blocked pawns only, G-3 option (a)), `adr-0008-needs-ui-summary-and-strings.html` (Needs section from Common's public API only, rule-summary Needs part, 8 new and 3 reworded keys in the three 1.6 locales).
  - Updated: `design/architecture/tech-reference/RimWorld-1.6.md` gains a Needs section (Need/NeedDef/`pawn.needs` API, level semantics, Outdoors and Beauty tables, settings-load-before-defs convention) and fixes the localization key-naming convention to document the `LordKuper.WorkManager.Settings.WorkTypes.*` form (D-2).
  - Updated: `design/architecture/tech-reference/LordKuper.Common-1.6.md` records the PawnFilter-convention exception (ADR-0006, D-3), the `DefCache` lazy-resolve latch, the `DefProvider.Current` test seam, the public vs internal UI helpers, and the verified build version 1.6.4.2.
  - New: `design/product/requirements.html`, the first persistent requirements doc, with feature set NF (work-type needs filter) and 45 durable acceptance criteria (NF-AC-n, mapped to sprint AC-n).
  - Subsystem decomposition is disabled: no new subsystems, no C4 changes, no DESIGN.md.
- **Rationale**: The design drafts passed design-review iteration 2 with every reviewer at APPROVE. Promotion makes the as-built decisions, research and requirements persistent before planning.
- **Follow-ups** (non-blocking, raised by BA, left unchanged this sprint): (1) `design/product/concept.html`, the "Guaranteed coverage" pillar is now qualified by the opt-in needs filter (an absolute block can leave a work type without workers); the concept text does not say so yet. (2) `concept.html` frontmatter `delegates_to` names `requirements/` while the promoted doc is the single file `requirements.html` (cosmetic).
- **Affected docs**: design/architecture/adr/adr-0006-needs-filter-model-and-evaluation.html, design/architecture/adr/adr-0007-needs-block-absolute-in-failsafe.html, design/architecture/adr/adr-0008-needs-ui-summary-and-strings.html, design/architecture/tech-reference/RimWorld-1.6.md, design/architecture/tech-reference/LordKuper.Common-1.6.md, design/product/requirements.html, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Plan approved for sprint 002-work-type-needs-filters

- **Decision**: `.asd/sprints/002-work-type-needs-filters/plan.md` is approved. User answers via AskUserQuestion, relayed by the orchestrator after it presented the full task table and DoD summary: tests "Separate test task (Recommended)"; plan "Approve". The plan has 8 tasks: data model and persistence (backend-dev), evaluation (backend-dev), fail-safe absolute block (backend-dev), strings and localization (frontend-dev), Needs settings section and rule summary (frontend-dev), unit tests (test-engineer), manual in-game verification MS-1…MS-5 (authored by test-engineer, run by the user in RimWorld), and a verification gate (backend-dev). Together they trace AC-1…AC-49.
- **Rationale**: A separate test task keeps one owner for the FluentAssertions and static-isolation rules and builds the shared `FakeDefProvider` once, following the sprint 001 layout. The DoD records the Linux/WSL build reality: SDK 10 at `/tmp/dotnet10` with `RIMWORLD_DIR`/`LORDKUPER_COMMON_DIR` overrides, tests through the Windows NUnit console (baseline 63/63), jb run or skip recorded, and the rebuilt tracked `1.6/Assemblies` output committed as `build: rebuild mod assembly`.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/plan.md

## 2026-09-29 — Sprint 002 impl halted at manual-steps gate: MS-1..MS-5 pending

- **Decision**: Impl tasks 1–8 of sprint 002-work-type-needs-filters are done. Verification gate: build 0 warnings / 0 errors, tests 119/119 passed, format clean, jb unavailable (skipped and recorded), rebuilt assembly committed in 09c4209. The impl phase halts at the manual-steps gate. MS-1…MS-5 in `manual-steps.md` stay `pending` until the user runs them in RimWorld 1.6. At the user's request ("You copy it for me"), the orchestrator installed the dev build to `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\WorkManager-dev`. Its settings file is `Mod_WorkManager-dev_WorkManagerMod.xml`. The phase resumes when the user gives the continue command.
- **Rationale**: The orchestrator checked MS-1…MS-5 for necessity. All five need a running game with UI interaction, so an agent cannot do them. An escalation entry of type `manual-steps` was added to `state.json`.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/manual-steps.md, .asd/sprints/002-work-type-needs-filters/state.json, .asd/sprints/002-work-type-needs-filters/plan.md

## 2026-09-29 — Sprint 002 impl assessment approved; manual checks passed, enter impl-review

- **Decision**: Impl assessment for sprint 002-work-type-needs-filters is approved. User answer via AskUserQuestion, relayed by the orchestrator: "Approve". The user reported the in-game checks verbatim: "Everything passed as expected. Continue the sprint until completion." MS-1…MS-5 in `manual-steps.md` are marked `done` (verified by user 2026-09-29), the Task 7 user-verification subtasks in `plan.md` are ticked, and the `manual-steps` escalation in `state.json` is resolved. The sprint advances to impl-review, iteration 1.
- **Rationale**: Impl summary: Tasks 1–8 done; AC-1…AC-49 covered; build 0 warnings / 0 errors; tests 119/119 passed; format clean; jb skipped (unavailable); MS-1…MS-5 passed in RimWorld 1.6; commits 671c59e..c992cca. The impl completion gate is met.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/manual-steps.md, .asd/sprints/002-work-type-needs-filters/plan.md, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 impl-review iter 01: FAIL/CONCERNS → impl fix

- **Decision**: Impl-review iter 01 for sprint 002-work-type-needs-filters did not meet the DoD. The sprint returns to impl in fix mode (`review_fixes_pending: iter-01`).

  | Reviewer | Verdict |
  |---|---|
  | quality | CONCERNS (2 low) |
  | implementation | APPROVE |
  | testing | APPROVE |
  | ui | APPROVE |
  | simplification | CONCERNS (2 critical checklist hits, 1 low) |
  | documentation | FAIL (1 high, 2 medium, 2 low) |
  | performance | CONCERNS (1 medium, 1 low) |
  | external | APPROVE |

  User decisions via AskUserQuestion, relayed by the orchestrator:
  - Documentation #1 (high: `CopyRimWorldTestDeps` reverses a rejected alternative of ADR-0002): "Keep copy, amend ADR-0002 (Recommended)". The target stays. It copies only into the gitignored test `bin/` and is never committed or shipped, the same approach as LordKuper.Common's test csproj. The Architect amends ADR-0002 in this sprint.
  - Fix scope: "Apply all (Recommended)". Performance #2 (low, per-frame `Description` rebuild) was excluded: it existed before this sprint, and the fix would add caching state. It is resolved by user decision as a follow-up.
- **Fix routing**:
  - Code, one dev: quality #1 (NaN guard in `ValidateNeedsFilter`, plus a test) and #2 (Add row height uses `Buttons.ActionButtonHeight`); simplification #1 (drop the `limit?.` guard in `GetAddableNeeds` and its obsolete test), #2 (drop the `removeIndex < limits.Count` check) and #3 (use `DefCache.Label` fallback in the summary); performance #1 (build the addable-needs list on click, and use a non-allocating visibility check).
  - Docs, Architect: documentation #1 (amend ADR-0002; ADR-0006 test approach / CA-5 link to it), #2 (test-discovery fact kept in one tech reference, the others link to it), #3 (ADR-0008 tooltip route verified as `TipSignal`), #4 (`RimWorld-1.6.md` UI API subsection) and #5 (ADR-0006 / ADR-0008 `satisfies` trace chips).
- **Rationale**: All code fixes are local and remove code or avoid allocation. None adds an abstraction. The ADR-0002 amendment records a real constraint: NUnit loads test types at discovery, before the resolver runs. The "never ship the host" principle still holds because `bin/` is untracked.
- **Follow-ups**: performance #2, a lazy or cached rule-summary tooltip in `DoWorkTypeRule`.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/reviews/impl/iter-01/, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 impl fix for iter-01: findings resolved; enter impl-review iter 02

- **Decision**: The impl fix for iter-01 of sprint 002-work-type-needs-filters is done: all iter-01 findings are resolved. `review_fixes_pending` is cleared and the sprint returns to impl-review, iteration 2.
  - Code (commit a9f5154): quality #1 (NaN guard in `ValidateNeedsFilter`, with a test), quality #2 (Add row height uses `Buttons.ActionButtonHeight`), performance #1 (addable-needs list is built on click; the visibility check does not allocate), simplification #1–#3. The assembly was rebuilt in 29f5336. Build 0 warnings / 0 errors, tests 119/119 passed, format clean.
  - Docs (commit 8aa1af8): documentation #1 (ADR-0002 amendment, linked from ADR-0006 / CA-5); #2 (the test-discovery fact lives in `NUnit-4.6.1.md`, and `RimWorld-1.6.md` and `LordKuper.Common-1.6.md` link to it); #3 (ADR-0008 tooltip route marked verified); #4 (`RimWorld-1.6.md` UI API section); #5 (trace chips).
  - Performance #2: resolved by user decision as a follow-up (see the iter 01 entry).
- **Rationale**: Every finding routed to fix mode has a matching commit, and the verification gate passes. Moving from impl to impl-review is the normal impl⇄impl-review cycle step, so no review counter is reset.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 impl-review iter 02: CONCERNS → impl fix

- **Decision**: Impl-review iter 02 for sprint 002-work-type-needs-filters did not meet the DoD. No reviewer returned FAIL, so the findings go to autofix: the sprint returns to impl in fix mode (`review_fixes_pending: iter-02`).

  | Reviewer | Verdict |
  |---|---|
  | quality | APPROVE |
  | implementation | APPROVE |
  | testing | APPROVE |
  | ui | CONCERNS (1 medium) |
  | simplification | APPROVE |
  | documentation | CONCERNS (1 medium) |
  | performance | APPROVE |
  | external | APPROVE |

- **Fix routing**: ui #1 and documentation #1 have the same root cause. The needs-filter Add button is always shown, but ADR-0008 and LordKuper.Common hide it when nothing can be added. Fix it in code, one dev: hide the Add button when there are no addable needs, and use a non-allocating check so the iter-01 performance fix #1 still holds. After the fix, ADR-0008 matches the code, so no doc change is needed.
- **Dropped**: one quality low finding fell below the severity floor. The orchestrator checked it and found it to be a false positive, because `DefCache.Label` already falls back to `DefName`.
- **Rationale**: A single local code change resolves both findings. It adds no abstraction and keeps the documented design (ADR-0008) as the source of truth.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/reviews/impl/iter-02/, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 impl fix for iter-02: findings resolved; enter impl-review iter 03

- **Decision**: The impl fix for iter-02 of sprint 002-work-type-needs-filters is done: all iter-02 findings are resolved. `review_fixes_pending` is cleared and the sprint returns to impl-review, iteration 3.
  - Code (commit b7077ee): ui #1 and documentation #1 (same root cause). The needs-filter Add button is shown only when an addable need exists. The check uses the non-allocating `HasAddableNeeds` with a shared `IsAddableNeed` predicate, so the iter-01 performance fix #1 still holds. 5 new tests. The assembly was rebuilt in 8389d4d. Build 0 warnings / 0 errors, tests 124/124 passed, format clean.
  - ADR-0008 now matches the code; no doc change was needed.
- **Rationale**: Every finding routed to fix mode has a matching commit, and the verification gate passes. Moving from impl to impl-review is the normal impl⇄impl-review cycle step, so no review counter is reset.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 impl-review iter 03: CONCERNS → impl fix

- **Decision**: Impl-review iter 03 for sprint 002-work-type-needs-filters did not meet the DoD. No reviewer returned FAIL, so the findings go to autofix: the sprint returns to impl in fix mode (`review_fixes_pending: iter-03`).

  | Reviewer | Verdict |
  |---|---|
  | quality | APPROVE |
  | implementation | APPROVE |
  | testing | APPROVE |
  | ui | APPROVE |
  | simplification | CONCERNS (1 critical) |
  | documentation | APPROVE |
  | performance | APPROVE |
  | external | APPROVE |

- **Fix routing**:
  - Simplification #1 (critical, checklist item "defensive code for impossible-by-contract case"): one dev removes the unreachable `if (addable.Count == 0) return;` in the `DoRuleNeeds` Add click handler (`Settings_WorkTypes.cs`). The button is drawn only when `HasAddableNeeds` is true, and the action runs synchronously in the same `OnGUI` call with the same inputs and the same `IsAddableNeed` predicate, so the list can never be empty there. No behaviour change.
  - Documentation (three drifts below the severity floor, routed anyway): the architect aligns the docs with the code in the same round. (a) ADR-0008: the `GetAddableNeeds` parameter type is `IReadOnlyList`. (b) ADR-0008 / CA-4: name `HasAddableNeeds`. (c) ADR-0006: state that `ValidateNeedsFilter` resets NaN to the default.
- **Rationale**: The code fix only deletes a line and adds no abstraction. The doc drifts are small, and fixing them in the same round keeps the ADRs as the source of truth without an extra iteration.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/reviews/impl/iter-03/, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002 impl fix for iter-03: findings resolved; enter impl-review iter 04

- **Decision**: The impl fix for iter-03 of sprint 002-work-type-needs-filters is done: all iter-03 findings are resolved. `review_fixes_pending` is cleared and the sprint returns to impl-review, iteration 4.
  - Code (commit 7fa925a): simplification #1. The unreachable `if (addable.Count == 0) return;` guard in the `DoRuleNeeds` Add click handler (`Settings_WorkTypes.cs`) is removed. No behaviour change. The assembly was rebuilt in 1812638.
  - Docs (commit 70653c9): the three below-floor drifts are fixed. ADR-0008 uses the `IReadOnlyList` parameter type for `GetAddableNeeds` and names `HasAddableNeeds`; ADR-0006 states that `ValidateNeedsFilter` resets NaN to the default.
  - Build 0 warnings / 0 errors, tests 124/124 passed, format clean.
- **Rationale**: Every finding routed to fix mode has a matching commit, and the verification gate passes. Moving from impl to impl-review is the normal impl⇄impl-review cycle step, so no review counter is reset.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/state.json, design ADR-0006, ADR-0008

## 2026-09-29 — Sprint 002 impl-review iter 04: APPROVE — DoD met

- **Decision**: Impl-review iter 04 for sprint 002-work-type-needs-filters meets the DoD. The user approved the final impl-review gate via AskUserQuestion ("Proceed to PR"), so the sprint advances to the pr phase.

  | Reviewer | Verdict |
  |---|---|
  | quality | APPROVE |
  | implementation | APPROVE |
  | testing | APPROVE |
  | ui | APPROVE (below-floor finding dropped) |
  | simplification | APPROVE |
  | documentation | APPROVE |
  | performance | APPROVE |
  | external | APPROVE |

- **Rationale**: The ui reviewer returned CONCERNS with a single `medium` finding. That is below the iter-04 `high` severity floor (`review-policy.md`), so it does not count toward the gate. It is also a false positive: Common's `DefCache.Label` returns the defName when the def is unresolved, so the rule summary already shows the defName (AC-39). The orchestrator checked this in the Common source and the workshop DLL. All other reviewers returned APPROVE. Advancing from impl-review to pr moves forward, so no review counter is reset.
- **Affected docs**: .asd/sprints/002-work-type-needs-filters/reviews/impl/iter-04/, .asd/sprints/002-work-type-needs-filters/state.json

## 2026-09-29 — Sprint 002-work-type-needs-filters completed, archived, PR #22

- **Decision**: Sprint closed. User confirmed the PR gate via AskUserQuestion ("Open PR") and target ("It goes upstream"). Sprint folder archived to `.asd/sprints/archived/002-work-type-needs-filters/` (524e5ee); branch pushed to the RobertCoop fork; PR opened against upstream: https://github.com/LordKuper/rimworld-workmanager/pull/22 (base `master`, head `RobertCoop:sprint/002-work-type-needs-filters`). The auto-mode classifier initially blocked the archive/push/PR steps; the user then explicitly authorized them in chat and they were run by the orchestrator.
- **Rationale**: DoD met — all 49 ACs covered, impl-review iter 04 APPROVE (all 8 lenses at the high floor), 124/124 tests, build 0 warnings, format clean, MS-1..MS-5 passed in-game.
- **Follow-ups**: per-frame rule `Description` rebuild (pre-existing); G-3 option (b) — restrict the day-level fail-safe to allowed workers; concept.html "Guaranteed coverage" pillar wording vs the opt-in needs filter; concept.html `delegates_to` path cosmetic.
- **Affected docs**: .asd/sprints/archived/002-work-type-needs-filters/state.json, .asd/project/decisions-log.md

## 2026-09-30 — Sprint 002 follow-up: Available pawns preview applies the needs filter

- **Decision**: After in-game testing the user reported the preview did not reflect the needs filter (by design per PRD D2 / AC-40) and asked for it to hide need-blocked pawns, delivered on the sprint 002 branch / PR #22. The user explicitly asked for this as a minor update "with less process", so it was done without a new ASD sprint. `UpdateAllowedWorkers` now drops pawns blocked by the rule's effective needs filter (own, or the default rule's when inheriting) via `IsNeedBlocked(Pawn)`; `DoRuleNeeds` refreshes the preview on state/threshold/add/remove changes. `AvailablePawnsTooltip` reworded in en/ru/zh.
- **Rationale**: Preview should match actual assignment; the user found the mismatch confusing in play.
- **Verification**: build 0/0, format clean, 124/124 tests (preview needs a live map; in-game check by the user).
- **Affected docs**: design/architecture/adr/adr-0008-needs-ui-summary-and-strings.html, adr-0006 (Negative consequences), design/product/requirements.html (NF-AC-40). Archived sprint 002 PRD left unchanged (immutable).

## 2026-10-01 — Sprint 002 follow-up: needs filter threshold becomes a two-sided range

- **Decision**: At user request ("needs selector is a two-sided threshold slider similar to the skill level selector"), delivered on the sprint 002 branch / PR #22 as a direct update with light process (no new sprint, no review phases). `NeedLimit.Threshold` is replaced by an inclusive `FloatRange Limit` (default 50%-100%, equivalent to the old 50% threshold). A pawn is blocked when a resolved entry's level is `< min` or `> max`; a [0,1] range never blocks. Validation resets NaN bounds, clamps to [0,1] and swaps min > max. The UI uses Common's `Fields.DoLabeledPercentRangeSlider`; the summary shows `label [min..max]`; tooltips (en/ru/zh) say "outside its allowed range". Legacy settings with a `Threshold` and no `Limit` migrate to `[threshold, 1]` on load, touching values only (never `.Def`).
- **Rationale**: Players want to block on too-high as well as too-low levels and to set arbitrary intervals; the capacity-limit range slider already exists in Common.
- **Verification**: build 0 warnings, format clean, unit tests pass (see commit).
- **Affected docs**: design/architecture/adr/adr-0006-needs-filter-model-and-evaluation.html, adr-0008-needs-ui-summary-and-strings.html, design/product/requirements.html (NF-G1, NF-AC-1/2/9/12-15/23/32/33/37/39). ADR-0006's rejected "FloatRange per need" alternative is superseded. Archived sprint 002 docs left unchanged (immutable).
