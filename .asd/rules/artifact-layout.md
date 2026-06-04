# Artifact Layout

## Subsystem decomposition modes

Set by `project.subsystem_decomposition` in config (`enabled` | `disabled`). Layout differs.

## Paths (decomposition enabled)

```
<repo root>/
├── .asd/
│   ├── rules/
│   ├── templates/
│   ├── project/
│   │   ├── config.yaml
│   │   ├── commands.yaml
│   │   ├── custom-common-rules.md
│   │   ├── custom-design-rules.md
│   │   ├── custom-coding-rules.md
│   │   ├── decisions-log.md
│   │   └── stubs.md
│   └── sprints/
│       ├── <NNN-slug>/
│       │   ├── sprint.md
│       │   ├── state.json
│       │   ├── audit.md
│       │   ├── design/
│       │   │   ├── prd.html
│       │   │   ├── ux-spec.html
│       │   │   ├── adr.html
│       │   │   ├── design-md-delta.yaml
│       │   │   └── c4-full/{model/*.c4, views.c4, dist/}
│       │   ├── plan.md
│       │   ├── manual-steps.md
│       │   └── reviews/
│       │       ├── design/iter-NN/<reviewer>.md
│       │       └── impl/iter-NN/<reviewer>.md
│       └── archived/<NNN-slug>/
├── .claude/{agents/, skills/, hooks/, settings.json}
├── design/
│   ├── product/
│   │   ├── concept.html
│   │   └── requirements/<subsystem>.html
│   ├── architecture/
│   │   ├── stack.html
│   │   ├── c4/                          # subsystem registry + views; layout per project.diagram_tool
│   │   │   # likec4 mode: model/*.c4, views.c4, dist/
│   │   │   # mermaid mode: subsystems.yaml, architecture.html
│   │   ├── adr/<subsystem>/adr-NNNN-<slug>.html
│   │   ├── api/<subsystem>.html
│   │   └── tech-reference/<tech>-<version>.md
│   └── ux/
│       ├── DESIGN.md
│       ├── design-system.html
│       ├── accessibility.html
│       └── <subsystem>.html             # ux-spec per subsystem
└── CLAUDE.md
```

## Paths (decomposition disabled)

`design/` becomes flat:

```
design/
├── product/{concept.html, requirements.html}
├── architecture/
│   ├── stack.html
│   ├── adr/adr-NNNN-<slug>.html
│   ├── api.html
│   └── tech-reference/<tech>-<version>.md
└── ux/{DESIGN.md, design-system.html, accessibility.html, ux-spec.html}
```

No `c4/` directory. No subsystem subfolders.

## Subsystem registry

When decomposition enabled, registry lives in `design/architecture/c4/`. Layout per `project.diagram_tool`:

- **likec4**: `model/*.c4` (LikeC4 DSL). Subsystem id = container/component id. `likec4 build` produces `dist/` interactive HTML.
- **mermaid**: `subsystems.yaml` (machine registry). Subsystem id = entry id. Agent renders `architecture.html` with embedded Mermaid C4 views.

New subsystems added only via `design-promote`, with user approval, regardless of diagram tool.

## Document provenance

User-facing artifacts may carry a `provenance` frontmatter field:

- `original` (default) — designed within an ASD sprint from scratch
- `reverse-engineered` — built from existing code without source docs
- `migrated` — translated from an external document in another format/location

When `provenance != original`, set `source: <path or URL>`. HTML shows a provenance badge. Reviewers apply lighter checks on absent alternatives for non-original docs.

## Document representation rule

User-facing artifacts are HTML only. No parallel Markdown source. Exceptions:

- `DESIGN.md` — Google Labs format (YAML + Markdown), machine source for the design system. Spec: https://github.com/google-labs-code/design.md — agents fetch the current spec from upstream when creating/editing it.
- `commands.yaml` — machine source, not user-facing
- LikeC4 `.c4` files — DSL source

`design-system.html` is generated from DESIGN.md by the Documentation agent: all tokens/rules with live examples (color swatches with hex, typography samples, spacing scale, component previews). Regenerated when DESIGN.md changes.

## HTML shell wrapping (mandatory)

Every user-facing HTML artifact (prd, ux-spec, adr, concept, stack, accessibility, api, design-system, architecture) MUST be wrapped in `t_html-shell.html`. The fragment template (`t_prd.html`, …) supplies the `<section>` content filling `{{CONTENT}}`. Creators emit a complete HTML document, not a bare fragment.

**Placeholder fill** — creators compute and inline these when writing:

| Placeholder | Source / value |
|---|---|
| `{{DOC_TYPE}}` | one of `PRD`, `ADR`, `UX-spec`, `Concept`, `Stack`, `Accessibility`, `API`, `Design-system`, `Architecture` |
| `{{SUBSYSTEM}}` | subsystem id when persistent per-subsystem; `sprint` for sprint drafts; `project` for project-wide docs |
| `{{SPRINT_ID}}` | active `state.json.sprint_id` for sprint drafts; empty for persistent docs |
| `{{STATUS}}` | `draft` (design) / `in-review` (design-review) / `approved` (post design-promote) / `locked` (archived). ADR also reflects ADR status: `proposed`/`accepted`/`superseded`/`deprecated` |
| `{{UPDATED_AT}}` | ISO date (YYYY-MM-DD) of last write |
| `{{RESPONSIBILITY}}` | the `owns:` line from the fragment's responsibility frontmatter |
| `{{PROVENANCE}}` | `original` \| `reverse-engineered` \| `migrated` (from fragment frontmatter) |
| `{{SOURCE}}` | the `source:` field; empty when provenance=original |
| `{{SOURCE_SUFFIX}}` | ` (from {{SOURCE}})` in the provenance badge when source non-empty; else empty |
| `{{TITLE}}` | doc title — e.g. `PRD — Sprint 001 · <slug>` or `ADR-0007 · <decision title>` |
| `{{STATS}}` | doc-type chip strip; PRD: `N goals · N stories · N AC · N non-goals · updated …`; ADR: `status · subsystem · updated`; UX-spec: `N flows · N mockups`; Stack: `N langs · N frameworks · N components`; others: at least `updated …` |
| `{{TOC}}` | `<ol>` of links to each `<section id>` in fragment order, auto-generated from `<h2>` text |
| `{{CONTENT}}` | fragment body (everything after the frontmatter comment) |
| `{{GENERATED_BY}}` | `ASD workflow` |
| `{{GENERATED_AT}}` | same as `{{UPDATED_AT}}` |

**Badge omission**: omit the provenance badge when `PROVENANCE == original` (do not emit the `<span class="provenance-original">` block).

**Fragment invariants**: fragment files in `.asd/templates/` (`t_prd.html` etc.) must NOT include `<html>`, `<head>`, `<body>`, `<style>`, `<script>` — content-only, rely on the shell for chrome/styling. Reviewers FAIL fragments that duplicate shell chrome.

## Tech reference docs (mandatory for every chosen tech)

`design/architecture/tech-reference/<tech>-<version>.md` per `t_tech-reference.md`. Owner: Architect. Created for every chosen library, framework, runtime, external service. Includes canonical source URL, API surface used, version specifics, deprecations, project conventions.

**Refuse-to-implement rule**: Backend Dev, Frontend Dev, Test Engineer MUST verify `tech-reference/<tech>-<version>.md` exists before implementing with a tech. If missing → emit `FAILED — tech-reference missing for <tech>@<version>` and request it from Architect. No implementation without verified reference.

## Manual steps

`<sprint>/manual-steps.md` per `t_manual-steps.md`. Per-sprint, created lazily. Owners: dev agents (append entries); PM (validates necessity).

A manual step = an operational action a human must perform for the plan to complete (provision a secret, create a cloud resource, hand-run a migration, set an env var, register a third-party account). NOT a code stub (`stubs.md`) nor manual QA verification (reviews `testing.md`).

- When a subtask cannot proceed without a human-only operational action, the dev appends an `MS-N` entry (full step-by-step instructions + a `Verification` field) and marks the subtask `BLOCKED: MS-N` in `plan.md`.
- `Verification` is mandatory: states how the workflow confirms the action was done (a `commands.yaml` check, observable state, or explicit user confirmation).
- PM validates every new entry before the phase halts. Kept only when the action genuinely cannot be done autonomously (needs access, a secret, an external account, an authority the agent lacks). Otherwise rejected, returned to the dev to implement directly.
- Status `pending` → `done`. The registering dev flips to `done` only after running `Verification`.
- Sprint-scoped; archived with the sprint.

## Single Source of Truth (iron rule)

Each fact has exactly one home file. Other files link to it, never copy. Violation = `FAIL` from Documentation reviewer.

## Document responsibility

Every template in `.asd/templates/` MUST declare its responsibility in frontmatter:

```yaml
---
responsibility:
  owns: <SSoT scope>
  excludes: <what belongs elsewhere>
  delegates_to: <other docs>
---
```

Agents preserve the block. Reviewers verify content respects the declared scope.

## Naming

- kebab-case English filenames
- Sprint slug derived from scope, max 30 chars
- Sprint number zero-padded to 3 digits
- ADR filenames `adr-NNNN-<slug>.html`, NNNN globally unique across the project

## Sprint archival

On `pr` success, sprint folder moves from `.asd/sprints/<NNN-slug>/` to `.asd/sprints/archived/<NNN-slug>/`. Archived sprints never modified.

## Decisions log

Every approved decision (concept change, new subsystem, ADR, scope shift, custom-rule update) appends one entry to `.asd/project/decisions-log.md`:

```markdown
## YYYY-MM-DD — <one-line summary>

- **Decision**: <what was decided>
- **Rationale**: <why>
- **Affected docs**: <links> (optional)
```

Owner: PM agent. Never edited or removed.
