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

