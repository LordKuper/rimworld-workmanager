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
{{what plan covers, prose}}

## Context
- [requirements/{{subsystem}}.html](../../design/product/requirements/{{subsystem}}.html)
- [adr-{{NNNN}}-{{slug}}.html](../../design/architecture/adr/{{subsystem}}/adr-{{NNNN}}-{{slug}}.html)
- [ux/{{subsystem}}.html](../../design/ux/{{subsystem}}.html)

## Definition of Done
{{prose checklist of completion criteria — NO checkboxes here}}

### Task 1: {{title}}
- [ ] {{subtask}}
- [ ] {{subtask}}

### Task 2: {{title}}
- [ ] {{subtask}}

## Risks (optional)
- {{risk}}

## Dependencies (optional)
- Task {{N}} depends on Task {{M}}

## Out of scope (optional)
- {{exclusion}}
