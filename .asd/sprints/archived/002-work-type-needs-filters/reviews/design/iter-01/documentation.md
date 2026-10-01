[REVIEW-design-documentation]: FAIL

# Review — documentation

- **Phase**: design-review
- **Iteration**: 1 (severity floor: low, all findings count)
- **Scope**: `design/prd.html`, `design/adr.html` (sprint 002-work-type-needs-filters), cross-checked against `sprint.md`, `audit.md`, `.asd/project/decisions-log.md`, `.asd/project/commands.yaml`, persistent `design/`, `custom-*-rules.md`, templates `t_prd.html`, `t_adr.html`, `t_html-shell.html`.

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | high | `design/adr.html` §`#implementation-notes` "Appendix: build, test and dependency notes" (l.533-566) | **SSoT iron rule violation + ADR responsibility breach.** (a) The build invocation (l.537) and the Linux/WSL test route (l.543-548: build with `RimWorldManagedDirForTests`, copy to `/mnt/c/.../asd002-tests/bin`, run `nunit3-console.exe`, baseline 63/63) are copied verbatim from `audit.md` "Dependencies (optional)" (l.226-235), which is their home in this sprint; the canonical command SSoT is `.asd/project/commands.yaml` ("machine-only SSoT for project build/test/run commands"). The appendix even cites "see audit.md, Dependencies" and then copies it anyway. (b) The "LordKuper.Common version check" facts (clone HEAD `b74c441`, csproj 1.6.4.2, DLL size, doc-XML parity, SHA-256 prefixes, drift risk/mitigation, l.549-557) are new facts whose home is `audit.md` Dependencies now and `design/architecture/tech-reference/LordKuper.Common-1.6.md` after promotion (audit migration plan item 2 already targets that file); they do not belong in an ADR. (c) "Manual in-game checks for the plan" (l.558-564) is task/verification content owned by `plan.md` / `manual-steps.md`. The ADR fragment declares `excludes: requirements, ux, code`, and the appendix self-describes as "implementation facts, not decisions". If promoted, persistent ADR-0006…0008 would carry machine-specific, ephemeral paths (`/tmp/dotnet10`, `C:\Users\coops\...`) that will drift. | Delete the appendix section (and its TOC entry). Replace with one sentence in the Overview linking `../audit.md` (Dependencies) for the verified build/test route. Move the Common clone-vs-workshop parity facts into `audit.md` Dependencies and carry them to `LordKuper.Common-1.6.md` via migration plan item 2 at design-promote. Leave manual-check enumeration to the plan phase (the ADR Consequences already state which ACs are manual: AC-9, AC-11, AC-12, AC-17 adapter, AC-24). |
| 2 | low | `design/adr.html` l.1-15 | Two responsibility comment blocks. The fragment block is placed before `<!DOCTYPE html>`, and the shell's own block (`owns: html shell wrap for all asd user-facing html docs`) is retained after it, so the artifact declares two conflicting `owns` scopes. `prd.html` handles this differently (single fragment block after the DOCTYPE, shell block dropped), so the two drafts are inconsistent. | Keep a single fragment responsibility block (with `provenance`/`source`) directly after `<!DOCTYPE html>`, as in `prd.html`; drop the shell's template-level block. |
| 3 | low | `design/prd.html` l.20, l.291, l.24-121 | Shell placeholder fill deviates from `artifact-layout.md` §HTML shell wrapping: (a) `<meta name="responsibility">` holds an edited `owns` plus the `excludes` clause, but `{{RESPONSIBILITY}}` = the fragment's `owns:` line (`adr.html` l.26 does this correctly); (b) footer reads `by asd-ba`, but `{{GENERATED_BY}}` = `ASD workflow` (`adr.html` l.568 correct); (c) the shell `<style>` is a trimmed subset (no `pre`, `blockquote`, `.mermaid`, `.badges .provenance-*` rules) and the Mermaid `<script>` pair is dropped, so the PRD is not the `t_html-shell.html` shell verbatim. | Re-wrap the PRD fragment in the unmodified `t_html-shell.html`; set responsibility meta to the `owns:` line verbatim and the footer to `by ASD workflow`. |
| 4 | low | `design/prd.html` §`#user-stories` (l.176-189) | Traceability gap: AC-2 (threshold clamp), AC-3 (duplicate-entry removal) and AC-27 (Needs section present in the editor) are not referenced by any user story, while the other 46 ACs are. AC-27 is the entry point for US-1's UI flow. | Add AC-27 to US-1 (or US-5), and AC-2/AC-3 to US-6 (settings robustness) or US-2. |
| 5 | low | `design/prd.html` AC-45 (l.246) vs `design/adr.html` Overview (l.231) | AC-45 requires the needs research to be "documented as design background in the sprint design artefacts", but it lives in `audit.md` "Background research" R-1…R-8, which is not under `<sprint>/design/`. The ADR cites `audit.md` as satisfying AC-45. The criterion and its fulfilment point to different homes, so a strict reading fails the AC. | Reword AC-45 to name `audit.md` "Background research" (R-1…R-8) as the sprint-level home, with promotion to `design/architecture/tech-reference/RimWorld-1.6.md` at design-promote (audit migration plan item 1). |
| 6 | low | `.asd/project/decisions-log.md` (no 2026-09-29 entries for these items) | Both drafts assert user approval on 2026-09-29: PRD D1–D9 and D6a (`prd.html` l.258, l.266), ADR-0006…0008 `accepted` status, and Complication Approval CA-1…CA-5 (`adr.html` l.520). The decisions log has entries only for scope, audit, G-3 and no-hysteresis. `artifact-layout.md` §Decisions log requires one entry per approved decision (ADR, scope shift, and so on). D5 in particular records a deliberate departure from the ADR-0005 localization precedent, and it has no durable record outside the sprint draft. | PM appends 2026-09-29 entries for PRD requirement decisions D1–D9/D6a (noting the D5 departure from ADR-0005), ADR-0006/0007/0008 acceptance, and Complication Approval CA-1…CA-5. |

Checked, no issue:
- AC→ADR traceability: all 49 ACs map to an ADR decision, a test-approach bullet, or a stated manual check.
- ADR numbering: 0006–0008 continue after the persistent `adr-0005`. Relative links to `adr-0002` and `adr-0005` resolve.
- D-3 convention conflict: a scoped exception is recorded in ADR-0006 §8, with the `LordKuper.Common-1.6.md` amendment deferred to design-promote.
- Custom design rules: the threshold's settings surface is named (`WorkTypeAssignmentRule` / `NeedLimit.Threshold`); the `0.01f` UI step is justified as not a tunable; compatibility-shim impact is stated in ADR-0007; determinism is covered (stateless, AC-21).
- Provenance: `original` with empty `source` in both drafts, and the badge is correctly omitted.
- PRD stats chips match the content (5/8/49/9).
- Skipped UX/design-system steps are recorded in the PRD Design context with the precedent cited.

## Verdict
FAIL: 6 (high 1, low 5)

FAIL is mandated by `artifact-layout.md` §Single Source of Truth ("Violation = FAIL from Documentation reviewer") for finding #1. All six findings can be fixed without escalation: #1–#3 and #5 by the creators (asd-architect: #1, #2; asd-ba: #3, #4, #5), and #6 by PM.

## Next action
PM surfaces the FAIL to the user as required by the SSoT rule. The recommended resolution is to let the creators autofix: the architect removes the ADR appendix and replaces it with a link (moving the Common parity facts into `audit.md` Dependencies) and fixes the duplicate responsibility block; the BA re-wraps the PRD in the unmodified shell, adds the missing story→AC links and rewords AC-45; PM appends the missing decisions-log entries. Then run design-review iteration 2.

## Escalations (optional)
- finding #1: surfaced because the SSoT iron rule makes this a FAIL. No Complication Approval is needed: the fix removes a duplicate and adds no abstraction, scope, or contract change.
