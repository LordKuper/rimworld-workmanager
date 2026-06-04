---
name: asd-phase-plan
description: "Runs the ASD plan phase: dispatches asd-pm to author plan.md from the sprint design docs, decomposing work into Task N sections with checkbox subtasks traced to PRD acceptance criteria. Use when asd-sprint dispatches the plan phase, or when the user explicitly asks to run or re-run plan for the active sprint."
metadata:
  asd-role: phase
  asd-order: "6"
  version: "0.1"
allowed-tools: "Read AskUserQuestion Task"
---

# ASD Phase: Plan

## Preconditions
- Active sprint at `.asd/sprints/<NNN-slug>/`
- design-promote done: persistent `design/` docs reflect approved sprint design
- `state.json.phase` advanced from `design-promote`

## Tool policy
- Read — `.asd/project/config.yaml`, `state.json`, sprint.md, persistent design/ docs touched by sprint
- AskUserQuestion — rare, phase-level escalation only (PM handles section approvals)
- Task — dispatch `asd-pm` (author + state + decisions-log)

## Workflow

1. Read `.asd/project/config.yaml` (`language.chat`, `language.docs`, `project.subsystem_decomposition`)
2. Read `<sprint>/state.json` → confirm design-promote done
3. Read `<sprint>/sprint.md`, persistent design/ docs referenced (per-subsystem files updated this sprint, plus shared concept.html, stack.html, DESIGN.md, accessibility.html) and `.asd/project/commands.yaml`
4. Dispatch `asd-pm` via Task with payload:
   - sprint.md path, list of relevant persistent doc paths, `language.chat`, `language.docs`
   - template: `t_plan.md`
   - instruction:
     - update `state.json` (phase=plan)
     - author plan.md skeleton first
     - per-section discussion with user in `language.chat` per QODDA + language-policy section approval flow
     - **Stub inclusion step** (before task decomposition):
       - read `<sprint>/audit.md` "Related open stubs" section
       - if non-empty: AskUserQuestion per stub: include resolution in this sprint / defer (leave open) / mark accepted-debt
       - for each "include" choice: add explicit `### Task N: Resolve stub <ref>` to plan with owner derived from stub Owner column
       - for each "accepted-debt" choice: dispatch creator to edit stubs.md Reason field prepending `(accepted-debt)`
       - decisions-log entry summarising stub decisions
     - **Task decomposition rules**:
       - one Task per coherent unit of work
       - each Task references AC-N from PRD it satisfies (cite in Task body or Context link)
       - subtasks as checkboxes inside `### Task N:` block only
       - checkboxes are allowed in Task blocks only (parser-critical)
       - assign owner role per Task in Task body (backend-dev / frontend-dev / test-engineer)
       - list dependencies between tasks if non-trivial
     - **Definition of Done**:
       - all PRD acceptance criteria covered by Tasks
       - test scope explicit (unit / integration / e2e / manual)
       - all reviewers green at impl-review phase
     - on approval translate to `language.docs` and write `<sprint>/plan.md`
     - append decisions-log entry ("plan approved for sprint <NNN-slug>")
     - emit COMPLETED
5. On PM COMPLETED → emit phase COMPLETED with return contract
6. On PM QUESTION / FAILED / ABORT → relay, halt

## Artefacts produced
- `<sprint>/plan.md`
- Updated `state.json` (phase=plan)
- decisions-log entry

## Agents dispatched
- `asd-pm` (single Task)

## Skills dispatched
None.

## Return contract (single line)
```
PHASE: plan | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: impl
```

## References
- `.asd/rules/sprint-lifecycle.md` (plan phase contract)
- `.asd/rules/checkpoints.md` (plan approval gate)
- `.asd/rules/language-policy.md` (section approval flow)
- `.asd/rules/artifact-layout.md`
- Templates: `t_plan.md` (canonical plan structure)
