---
name: asd-phase-design
description: "Runs the ASD design phase for the active sprint: dispatches creators sequentially per the precondition chain (asd-ba for prd.html, a design-system gate, asd-ux-designer for ux-spec.html, asd-architect for adr.html and optional c4-full/) to author sprint design drafts. Use when asd-sprint dispatches the design phase, or when the user explicitly asks to run or re-run design for the active sprint."
metadata:
  asd-role: phase
  asd-order: "3"
  version: "0.1"
allowed-tools: "Read Glob AskUserQuestion Task Skill"
---

# ASD Phase: Design

## Preconditions
- Active sprint at `.asd/sprints/<NNN-slug>/`
- `audit.md` approved (per checkpoints precondition chain)
- `state.json.phase` advanced from `audit`

## Tool policy
- Read — `.asd/project/config.yaml`, `state.json`, `sprint.md`, `audit.md`, any audit-produced reverse/migrated drafts in `<sprint>/design/`
- Glob — design-system gate: check existence of `design/ux/DESIGN.md`, `design/ux/design-system.html`, `design/ux/accessibility.html`
- AskUserQuestion — rare, only on phase-level user escalation
- Task — sequential dispatch: BA, UX Designer, Architect, optional Architect (c4-full); plus PM for state update
- Skill — invoke `asd-design-system` when design-system gate detects missing files

## Workflow

1. Read `.asd/project/config.yaml` (`project.subsystem_decomposition`, `project.diagram_tool`, `language.chat`, `language.docs`)
2. Read `<sprint>/state.json`, `<sprint>/sprint.md`, `<sprint>/audit.md`
3. List existing drafts in `<sprint>/design/` (from audit phase, with `provenance` flag)
4. Dispatch `asd-pm` via Task: update `state.json` (phase=design)
5. **Step PRD**: dispatch `asd-ba` via Task:
   - inputs: sprint.md, audit.md, existing prd draft in `<sprint>/design/` (if any from audit), `language.chat`, `language.docs`
   - template: `t_prd.html`
   - instruction: integrate existing draft (preserve `provenance` + `source` if present); author full sprint PRD covering all scope; discuss each section with user in `language.chat`; on approval translate to `language.docs` and write to `<sprint>/design/prd.html`; emit COMPLETED
6. **Step Design-system gate**: on BA COMPLETED → check existence (Glob) of all three:
   - `design/ux/DESIGN.md`
   - `design/ux/design-system.html`
   - `design/ux/accessibility.html`
   - if ANY missing → invoke `asd-design-system` skill via Skill tool; halt until it emits COMPLETED; on FAILED/aborted → relay and halt phase
   - if all present → proceed to Step 7
7. **Step UX-spec**: on Design-system gate cleared → dispatch `asd-ux-designer` via Task:
   - inputs: prd.html, audit.md, existing ux-spec draft (if any), current `design/ux/DESIGN.md`, `design/ux/design-system.html`, `design/ux/accessibility.html`
   - templates: `t_ux-spec.html`, `t_design-md-delta.yaml`
   - instruction: integrate existing draft; author flows + UI mockups using ONLY existing DESIGN.md tokens; when a needed token is missing or must change, pause mockup work, AskUserQuestion to approve token addition/update, append entry to `<sprint>/design/design-md-delta.yaml` (create file on first entry per `t_design-md-delta.yaml`), THEN continue mockup referencing the new token; discuss each ux-spec section in `language.chat`; on approval translate + write `<sprint>/design/ux-spec.html`; emit COMPLETED (delta file produced inline iff any token gap surfaced — otherwise omitted)
8. **Step ADR**: on UX COMPLETED → dispatch `asd-architect` via Task:
   - inputs: prd.html, ux-spec.html, existing adr draft (if any), `design/architecture/stack.html`, existing adr/, `design/architecture/tech-reference/`
   - template: `t_adr.html`
   - instruction: integrate existing draft; author one or more ADRs for sprint scope (repeated `<article>` blocks); for any new tech proposed, create/update `tech-reference/<tech>-<version>.md` via WebFetch + `t_tech-reference.md`; discuss each decision in `language.chat`; on approval translate + write `<sprint>/design/adr.html`; emit COMPLETED
9. **Optional Step c4-full**: on ADR COMPLETED → if `project.subsystem_decomposition: enabled`:
   - dispatch `asd-architect` via Task
   - templates per `project.diagram_tool`:
     - likec4: `t_c4-model.c4`, `t_c4-views.c4`; produce `<sprint>/design/c4-full/model/*.c4`, `views.c4`, run `likec4 build` → `dist/`
     - mermaid: `t_subsystems.yaml`; produce `<sprint>/design/c4-full/subsystems.yaml` and `architecture.html` (mermaid-rendered)
   - instruction: full schema covering sprint scope (not delta — delta computed in design-promote); discuss overall view in `language.chat`; on approval write files; emit COMPLETED
   - if `subsystem_decomposition: disabled` → skip
10. On all required steps COMPLETED → dispatch `asd-pm` to update `state.json` (drafts ready), append decisions-log entry summarising drafts produced
11. Emit phase COMPLETED with return contract
12. Any creator QUESTION → relay to user, halt; resumes on user answer
13. Any creator FAILED / ABORT → relay, halt

## Artefacts produced
- `<sprint>/design/prd.html` (required)
- `<sprint>/design/ux-spec.html` (required)
- `<sprint>/design/adr.html` (required)
- `<sprint>/design/design-md-delta.yaml` (optional, produced inline by UX-spec step iff token gaps surfaced during mockup work)
- `<sprint>/design/c4-full/` (optional, when `subsystem_decomposition: enabled`; layout per `diagram_tool`)
- New or updated `design/architecture/tech-reference/<tech>-<version>.md` entries (when new tech proposed in ADR)

Indirect (via design-system gate): `design/ux/DESIGN.md`, `design/ux/design-system.html`, `design/ux/accessibility.html` (when gate dispatches `asd-design-system`).

## Agents dispatched
- `asd-pm` (state + decisions-log)
- `asd-ba` (PRD)
- `asd-ux-designer` (UX-spec; inline delta)
- `asd-architect` (ADR; optional c4-full; tech-reference)

## Skills dispatched
- `asd-design-system` (only when design-system gate detects missing DESIGN.md / design-system.html / accessibility.html)

## Return contract (single line)
```
PHASE: design | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: design-review
```

## References
- `.asd/rules/sprint-lifecycle.md` (design phase contract, precondition chain inside phase)
- `.asd/rules/checkpoints.md` (per-artifact approval)
- `.asd/rules/language-policy.md` (section approval flow, quote translation)
- `.asd/rules/artifact-layout.md` (sprint design folder, provenance, c4 mode layouts)
- Templates: `t_prd.html`, `t_ux-spec.html`, `t_adr.html`, `t_design-md-delta.yaml`, `t_c4-model.c4`, `t_c4-views.c4`, `t_subsystems.yaml`, `t_tech-reference.md`
