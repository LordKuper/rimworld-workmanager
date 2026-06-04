---
name: asd-init
description: "Initializes the ASD (Agentic Software Development) workflow in a project, or edits existing ASD settings in diff mode. Auto-detects build commands and external tools, collects config via AskUserQuestion, generates .asd/project/config.yaml and seeds infrastructure-only design/ docs; concept, stack, and design system are owned by dedicated skills. Use when the user runs /asd-init or asks to set up, initialize, configure, or change ASD workflow settings."
metadata:
  asd-role: init
  version: "0.1"
allowed-tools: "Read Write Edit Glob Grep Bash AskUserQuestion"
---

# ASD Init

## Preconditions
- Repo at project root
- Infra present: `.asd/rules/`, `.asd/templates/`, `.claude/`

## Modes
- **Fresh**: no `.asd/project/config.yaml` → full setup
- **Re-init**: config exists → diff editor

## Always first (both modes)

0. **Sync `CLAUDE.md` ASD section** before any other step (see "CLAUDE.md sync" below). Runs unconditionally on every invocation, fresh or re-init, regardless of subsequent user choices or aborts.

## Workflow (fresh)

1. Detect greenfield vs brownfield via Glob on source files
2. AskUserQuestion batch: chat lang, docs lang, subsystem_decomposition, backward_compat, external_review
3. If decomposition enabled → AskUserQuestion: diagram_tool (`likec4` | `mermaid`)
4. Detect OS via Bash (silent; no confirm yet)
5. Detect external tools (silent; record results, do not prompt per-tool yet):
   - `likec4 --version` (only if diagram_tool=likec4)
   - designmd (always): check `node --version` and `npm --version`. Tooling invoked via `commands.yaml` (`designmd-*`); no `designmd` binary on PATH required.
   - `codex --version` (only if external_review=enabled)
   Record paths and missing flags for the consolidated proposal
6. Pick review iteration defaults (low=1 medium=1 high=2 critical=10) — include in proposal, do not prompt yet
7. Pick git defaults (base_branch from `git symbolic-ref refs/remotes/origin/HEAD` or `main`; branch_pattern `sprint/<NNN>-<slug>`; gh_enabled from `gh --version`; auto_pr=false) — include in proposal
8. Auto-detect build commands from:
   - manifests: package.json scripts, Cargo.toml, pyproject.toml, go.mod, Makefile
   - code analysis: CI configs (.github/workflows, .gitlab-ci.yml, etc.), Dockerfile RUN lines, README command patterns
   Record into proposal; do not prompt per-command yet
8a. **Consolidated proposal & edit gate** — present every auto-detected/defaulted value in one structured block in `language.chat`:
    - OS, external tools (with missing flags + install hint), review iteration limits, git settings, detected build/test/lint/run commands
    Then AskUserQuestion: `accept-all` | `edit-section` | `abort`.
    - `edit-section` → AskUserQuestion which section (os | tools | review | git | commands), collect new values, re-show proposal, loop until `accept-all`
    - Missing required tools (designmd always; likec4 if decomp+likec4; codex if external_review) → must be resolved here: install / override path / disable feature. Do NOT silently proceed with missing required tools.
    Only after `accept-all` proceed to write.
9. Write `.asd/project/config.yaml` from `t_config.yaml` with all approved fields (including `project.diagram_tool` when decomp enabled)
10. Ask user what custom rules to add (separately for common / design / coding scopes); write three files from templates: `.asd/project/custom-common-rules.md`, `custom-design-rules.md`, `custom-coding-rules.md`. Empty scope still writes the template stub (header + intro), so agents always find the file.
11. Write `.asd/project/decisions-log.md` from `t_decisions-log.md`
12. Write `.asd/project/commands.yaml` (from `t_commands.yaml` + detected + OS-specific `custom.designmd-*`)
13. If decomp enabled:
    - **likec4 mode**: seed `c4/model/main.c4`, `c4/views.c4` from templates; run `likec4 build` → `c4/dist/`
    - **mermaid mode**: seed `c4/subsystems.yaml` from `t_subsystems.yaml`; render `c4/architecture.html` with initial mermaid context view
14. Append decisions-log entry ("ASD initialized for project; decomposition=X, diagram_tool=Y, OS=Z")
15. **Post-init artefact checks** — suggest dedicated skill for each missing required artefact (do NOT auto-dispatch). Order: concept → stack → design-system:
    - `design/product/concept.html` absent → suggest `/asd-concept`
    - `design/architecture/stack.html` absent → suggest `/asd-stack`
    - `design/ux/DESIGN.md` OR `design/ux/design-system.html` OR `design/ux/accessibility.html` absent → suggest `/asd-design-system`
16. Brownfield: prompt user to start sprint with audit-only scope (optional)
17. Print summary + return contract

## Workflow (re-init)

1. Read current `.asd/project/config.yaml`
2. **Dump full current config to chat** in `language.chat` before any edit prompt. Render every field as a structured block. User MUST see complete current state before being asked what to change. Do NOT skip or summarise — full values verbatim.
3. AskUserQuestion which sections to edit
4. Per section: ask new value → add to pending change-set (do not write yet)
5. Show consolidated diff of all pending edits → AskUserQuestion: `accept-all` | `edit-section` | `abort`; loop until accepted
6. Apply diff; write config
7. Append decisions-log entry per change

## CLAUDE.md sync

Idempotent. Section is delimited by exact marker lines:

```
<!-- BEGIN: Agentic Software Development Rules -->
<!-- END: Agentic Software Development Rules -->
```

Body between markers = verbatim contents of `.asd/templates/t_CLAUDE.md` (no transformation).

Algorithm:

1. If `CLAUDE.md` absent at repo root → create it with:
   ```
   # CLAUDE.md

   ## Agentic Software Development Rules

   <!-- BEGIN: Agentic Software Development Rules -->
   <contents of .asd/templates/t_CLAUDE.md>
   <!-- END: Agentic Software Development Rules -->
   ```
2. If present and both markers found → extract body between markers, byte-compare to template. If equal → no-op. If different → replace body in place (preserve everything outside markers, including the `## Agentic Software Development Rules` heading and any other user content).
3. If present but markers missing → append the `## Agentic Software Development Rules` section (with markers and template body) to end of file. Do NOT modify pre-existing content.
4. If only one marker is found (malformed) → STOP, ask user via AskUserQuestion: `repair` (replace from first marker to EOF) | `abort` | `manual`. Never silently guess.

Rules:
- Marker lines are exact, case-sensitive, no trailing spaces.
- Never touch content outside markers.
- Trailing newline of template preserved.

## OS-specific commands written to .asd/project/commands.yaml

Four custom commands always emitted. Linter is always invoked via `designmd-lint`; `designmd-install` is a session-scoped prerequisite on Windows (no-op elsewhere).

**Windows** (run from project root):
- `designmd-install: "npm install @google/design.md"`
- `designmd-lint: "node_modules\\.bin\\design.md.cmd lint design\\ux\\DESIGN.md"`
- `designmd-diff: "node_modules\\.bin\\design.md.cmd diff"` (path args supplied at call time)
- `designmd-export: "node_modules\\.bin\\design.md.cmd export --format json-tailwind design\\ux\\DESIGN.md"`

**Linux/macOS**:
- `designmd-install: ""` (no-op; `npx` fetches on demand)
- `designmd-lint: "npx @google/design.md lint design/ux/DESIGN.md"`
- `designmd-diff: "npx @google/design.md diff"`
- `designmd-export: "npx @google/design.md export --format json-tailwind design/ux/DESIGN.md"`

## Artefacts produced

- `.asd/project/config.yaml`
- `CLAUDE.md` (created or ASD-section synced from `t_CLAUDE.md`)
- `.asd/project/custom-common-rules.md`, `custom-design-rules.md`, `custom-coding-rules.md`, `decisions-log.md`
- `.asd/project/commands.yaml`
- `design/architecture/c4/` content per `diagram_tool` (decomp only)

Concept, stack, and design system are NOT produced here; they are owned by `/asd-concept`, `/asd-stack`, and `/asd-design-system` respectively.

## Agents dispatched

None. Init runs solo; no sprint context yet.

## Return contract (single line)

```
INIT: <fresh|re-init> | MODE: <greenfield|brownfield> | DECOMP: <enabled|disabled> | DIAGRAM: <likec4|mermaid|n/a> | TOOLS: likec4=<ok|missing|skip|n/a> designmd=<ok|missing|skip> codex=<ok|missing|skip>
```

Followed by file-creation summary.

## References

- Templates in `.asd/templates/`
- `.asd/rules/core.md`, `.asd/rules/artifact-layout.md`
- Google Labs DESIGN.md spec: https://github.com/google-labs-code/design.md
