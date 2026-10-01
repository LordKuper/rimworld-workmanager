---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-design-documentation]: APPROVE

# Review — documentation

- **Phase**: design-review
- **Iteration**: 2 (severity floor: medium; low findings dropped)

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings | — |

### Scope checked

- **HTML shell** (`artifact-layout.md`): both `design/prd.html` and `design/adr.html` are complete `t_html-shell.html` documents. DOC_TYPE, SUBSYSTEM (`sprint`), SPRINT_ID, STATUS, UPDATED_AT, RESPONSIBILITY (matches the fragment `owns:`), PROVENANCE, SOURCE, TITLE, STATS, TOC and CONTENT are all filled. No leftover `{{…}}` placeholders. No shell chrome is duplicated inside the content. The PRD STATS chip counts (5 goals, 8 stories, 49 AC, 9 non-goals) match the body.
- **Responsibility and provenance**: the fragment responsibility comments are kept verbatim from `t_prd.html` / `t_adr.html`. `provenance: original` with an empty `source`, and the provenance badge is correctly omitted.
- **Template adherence**: the PRD keeps all template sections and adds a "Design context" section that records requirement-level decisions D1–D9 and the approved UX/design-system skip. This is within scope because it holds no architecture decisions. The ADR draft repeats the `<article class="adr">` block for each decision (0006–0008) with Context / Decision / Consequences / Alternatives / Related, and uses prefixed heading ids so they stay unique.
- **ADR numbering**: persistent `design/architecture/adr/` holds 0001–0005, so 0006–0008 are the next unique numbers. The relative links to ADR-0002 and ADR-0005 resolve from the sprint draft location.
- **SSoT**: the research facts link to `audit.md` "Background research" R-1…R-8 (AC-45, ADR Overview), and their promotion target is recorded in audit migration plan item 1. The build/test environment links to `audit.md` "Dependencies" and is not copied. The scope decisions link to `sprint.md` and `decisions-log.md`. The D-3 convention conflict with `LordKuper.Common-1.6.md` is handled by a scoped exception in ADR-0006 and an amendment at design-promote (audit migration plan item 2). The D-2 key-form drift in `RimWorld-1.6.md` is tracked in migration plan item 1, and AC-42 names the `LordKuper.WorkManager.Settings.WorkTypes.*` form explicitly.
- **Traceability**: every AC-1…AC-49 is referenced by at least one user story, and every story maps to a goal. Every sprint.md Acceptance bullet is covered by ACs. ACs that involve an architectural choice map to ADRs: persistence, evaluation and combine to ADR-0006; the absolute block and fail-safe to ADR-0007; the settings UI, summary and i18n to ADR-0008. The test-coverage AC-48 maps to the ADR-0006 test approach, and the AC-9/11/12/17/24 exceptions are stated explicitly as manual checks.
- **Consistency with the codebase**: the ADR-0008 key counts (47 existing `Settings.WorkTypes.*` keys in each 1.6 locale, 55 after the change) match `1.6/Languages/*/Keyed/WorkManager_Keyed.xml`. The reused keys `WorkTypeRuleUndefinedSectionTooltip`, `WorkTypeRuleDisabledSettingTooltip`, `EnsureWorkerAssignedOnTooltip`, `MinWorkerNumberTooltip` and `AvailablePawnsTooltip` exist. The `GetAssignEveryoneTooltip` / `AppendUndefinedSettingTooltip` pattern that `GetFilterNeedsTooltip` follows exists in `Resources.cs`.
- **Custom rules**: the new tunable's settings surface is named (`WorkTypeAssignmentRule` in mod settings; the threshold is a per-entry setting). The compatibility shim impact (WorkTab, PriorityMaster, MoreThanCapable) is called out in ADR-0007, as `custom-design-rules.md` requires. Determinism is addressed: the check is stateless and has no hysteresis. The UX/design-system skip is recorded in the PRD Design context and in the decisions-log (2026-09-29).

## Verdict
APPROVE

## Next action
None from the documentation reviewer. At design-promote, the owners should carry out audit migration plan items 1–3: extend `RimWorld-1.6.md` with the needs API and correct the key form, amend the `LordKuper.Common-1.6.md` eligibility convention to cite ADR-0006, and promote the PRD. They should also split the ADR draft into `adr-0006`/`0007`/`0008` files and fix the relative links for the new location.

## Escalations (optional)
- none
