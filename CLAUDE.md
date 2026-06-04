# CLAUDE.md

## Agentic Software Development Rules

<!-- BEGIN: Agentic Software Development Rules -->
### Core rules

@.asd/rules/core.md

### Project-specific rules

@.asd/project/custom-common-rules.md

Phase-scoped rules (loaded by agents per phase, not globally): `.asd/project/custom-design-rules.md` (design / design-review), `.asd/project/custom-coding-rules.md` (impl / impl-review).

### Language policy

@.asd/rules/language-policy.md

### Slash commands

- `/asd-init` — initialize workflow or edit settings
- `/asd-sprint` — start new sprint or continue active one

### Configuration

Workflow settings live in `.asd/project/config.yaml`.

### Folder structure

Authoritative path map is in `.asd/rules/artifact-layout.md`.

When subsystem decomposition is enabled (`project.subsystem_decomposition` in config), persistent design docs are organized per subsystem.

### Rule docs (`.asd/rules/`)

- `core.md` — model, interaction protocol, invariants
- `sprint-lifecycle.md` — phases, signals, plan format
- `checkpoints.md` — pauses, approvals, preconditions
- `git-strategy.md` — branches, commits, TODO stubs, PR
- `artifact-layout.md` — paths, ownership, SSoT, archival
- `review-policy.md` — severity, iteration floor, autofix vs escalation
- `external-review.md` — Codex CLI integration
- `language-policy.md` — language matrix
- `design-principles.md` — design-time principles

### Hard rules

- Never modify workflow infrastructure (`.asd/rules/`, `.asd/templates/`, `.claude/`). Only `/asd-init` may edit settings.
- All project work flows through `/asd-sprint`. No ad-hoc edits to project code outside a sprint.
- One active sprint at a time. New sprint blocked until the active one is archived.
<!-- END: Agentic Software Development Rules -->
