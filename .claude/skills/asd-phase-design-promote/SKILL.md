---
name: asd-phase-design-promote
description: "Runs the ASD design-promote phase: asd-pm orchestrates user-approved per-subsystem decomposition, then asd-architect, asd-ba, and asd-ux-designer promote sprint drafts to persistent design/ in parallel, gated by a final user confirmation. Use when asd-sprint dispatches design-promote, or when the user explicitly asks to run or re-run design-promote for the active sprint."
metadata:
  asd-role: phase
  asd-order: "5"
  version: "0.1"
allowed-tools: "Read AskUserQuestion Task"
---

# ASD Phase: Design Promote

## Preconditions
- Active sprint at `.asd/sprints/<NNN-slug>/`
- Design drafts present and approved by design-review (DoD met)
- `state.json.phase` advanced from `design-review`

## Tool policy
- Read — `.asd/project/config.yaml`, `state.json`, sprint design drafts, audit.md, persistent `design/`
- AskUserQuestion — rare, phase-level escalation only (PM and creators handle per-doc/per-subsystem approvals)
- Task — dispatch PM (orchestrator), Architect, BA, UX Designer (domain promoters)

## Workflow

1. Read `.asd/project/config.yaml` (`project.subsystem_decomposition`, `project.diagram_tool`, `system.tools.likec4`, `system.tools.designmd`, `language.chat`, `language.docs`)
2. Read `<sprint>/state.json` → confirm design-review DoD met
3. Read sprint drafts + audit migration plan
4. **PM orchestration step** — dispatch `asd-pm` via Task:
   - update `state.json` (phase=design-promote)
   - if `subsystem_decomposition: enabled`:
     - propose overall per-subsystem split of drafts (which subsystems touched, which fragments go where); AskUserQuestion; iterate on changes
     - detect new subsystems inferred from drafts; for each: AskUserQuestion (name, parent container, description); collect approvals
     - distribute migration plan items to domain (architecture / product / ux / api)
   - if `subsystem_decomposition: disabled`: skip decomposition, mark all writes flat
   - emit decomposition map (per-domain target paths + new subsystems + migration distribution) to caller
5. **New subsystem registry update** (only if new subsystems approved in step 4) — dispatch `asd-architect` via Task:
   - patch c4 registry per `project.diagram_tool` (likec4 model OR subsystems.yaml)
   - create empty domain folders for each new subsystem
   - run `likec4 build` for likec4 mode (regen `c4/dist/`)
   - emit COMPLETED
6. **Parallel domain promotion** — dispatch via Task in parallel:
   - **`asd-ba`** with payload (prd.html, decomposition map for product domain, migration items tagged product):
     - per subsystem (or flat): write decomposed PRD content into `design/product/requirements/<subsystem>.html` (or `requirements.html`); merge with existing if present
     - process product migration items (with `provenance: migrated|reverse-engineered` and `source`)
     - AskUserQuestion before each persistent write; show diff vs existing
     - emit COMPLETED
   - **`asd-architect`** with payload (adr.html, c4-full, decomposition map for architecture domain, migration items tagged architecture):
     - per subsystem (or flat): split adr.html decisions into `design/architecture/adr/<subsystem>/adr-NNNN-<slug>.html` (new files; NNNN globally unique)
     - merge any new API contracts into `design/architecture/api/<subsystem>.html` (or `api.html`)
     - update `design/architecture/stack.html` and `design/architecture/tech-reference/` entries if sprint introduced new tech
     - compute c4 delta from `<sprint>/design/c4-full/` vs persistent `design/architecture/c4/`; apply patches; regenerate `c4/dist/` (likec4 mode) or `architecture.html` (mermaid mode)
     - process architecture migration items
     - AskUserQuestion before each persistent write
     - emit COMPLETED
   - **`asd-ux-designer`** with payload (ux-spec.html, design-md-delta.yaml if present, decomposition map for ux domain, migration items tagged ux):
     - per subsystem (or flat): split ux-spec content into `design/ux/<subsystem>.html` (or `ux-spec.html`); merge with existing
     - if `design-md-delta.yaml` present: apply add/update/remove ops to `design/ux/DESIGN.md`; if `system.tools.designmd` is true, run `designmd-lint` from `commands.yaml`; halt on lint errors. On Windows, run `designmd-install` once per session before the first `designmd-lint`/`-diff`/`-export` invocation (no-op on Linux/macOS). Never inline the linter binary — always go through the `designmd-*` commands.
     - regenerate `design/ux/design-system.html` from patched DESIGN.md (only if DESIGN.md changed)
     - process ux migration items
     - AskUserQuestion before each persistent write
     - emit COMPLETED
7. Wait all three creator COMPLETED
8. **Final mutation confirmation** — dispatch `asd-pm` via Task:
   - present summary of all persistent writes performed (per-domain counts + new subsystems + files touched)
   - AskUserQuestion: confirm finalize / rollback / partial rollback
   - on confirm: compose decisions-log entries (decomposition, each new subsystem, each promoted artefact, DESIGN.md patch, c4 patch) and append to `.asd/project/decisions-log.md`
   - update `state.json` (phase=design-promote done)
   - emit COMPLETED
9. Emit phase COMPLETED with return contract
10. Any agent QUESTION / FAILED / ABORT → relay, halt

## Artefacts produced
- Persistent docs under `design/` per domain (per subsystem when decomposition enabled, flat when disabled)
- Patched `design/ux/DESIGN.md` (when delta present)
- Regenerated `design/ux/design-system.html` (when DESIGN.md changed)
- Patched `design/architecture/c4/` (when c4-full present); regenerated `c4/dist/` (likec4) or `architecture.html` (mermaid)
- New `design/architecture/tech-reference/<tech>-<version>.md` entries if applicable
- Appended decisions-log entries

## Agents dispatched
- `asd-pm` (orchestration, new subsystem proposals, state, decisions-log, final confirm)
- `asd-architect` (architecture domain + c4 registry updates)
- `asd-ba` (product domain)
- `asd-ux-designer` (ux domain)

## Skills dispatched
None.

## Return contract (single line)
```
PHASE: design-promote | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: plan
```

## References
- `.asd/rules/sprint-lifecycle.md` (design-promote phase contract)
- `.asd/rules/artifact-layout.md` (path map per decomposition mode, c4 modes, promotion rules)
- `.asd/rules/checkpoints.md` (per-promotion approval, new-subsystem approval, final mutation confirm)
- `.asd/rules/language-policy.md`
- Templates: `t_prd.html`, `t_adr.html`, `t_ux-spec.html`, `t_api.html`, `t_design-system.html`, `t_subsystems.yaml`, `t_tech-reference.md`, `t_decisions-log.md`
