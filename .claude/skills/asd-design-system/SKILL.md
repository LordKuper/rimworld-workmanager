---
name: asd-design-system
description: "Forms or edits the project design system (design/ux/DESIGN.md, design-system.html, accessibility.html) via asd-ux-designer, branching by silent detection into one of three flows (greenfield / constraints / brownfield extraction). Fetches the Google Labs DESIGN.md spec, lints tokens, regenerates design-system.html previews, and authors the accessibility baseline. Use when the user runs /asd-design-system, when asd-init or asd-phase-design detects missing DESIGN.md/design-system.html/accessibility.html and suggests this skill, or when the user asks to define, draft, refine, edit, augment, or reverse-engineer the project design system, design tokens, or accessibility baseline."
metadata:
  asd-role: artifact
  version: "0.1"
allowed-tools: "Read Glob Grep AskUserQuestion Task"
---

# ASD Design System

## Preconditions
- `.asd/project/config.yaml` exists (run `/asd-init` first)
- `design/product/concept.html` exists (run `/asd-concept` first; concept seeds visual direction)
- `design/architecture/stack.html` exists (run `/asd-stack` first; stack constrains UI platform — web/native/cli)
- No active sprint required

## Tool policy
- Read — `.asd/project/config.yaml`, concept.html, stack.html, existing DESIGN.md/design-system.html/accessibility.html, source CSS/components, theme files
- Glob/Grep — silent scan for brownfield signals (CSS, SCSS, Tailwind config, theme.ts, styled-components, design exports)
- AskUserQuestion — variant choice, constraints, section approvals, lock-in/revise loop
- Task — dispatch `asd-ux-designer` (author, WebFetch spec, lint, render previews, accessibility baseline), `asd-pm` (decisions-log)

## Phase 1 — silent detection (NO asking)

Scan in order:
1. `design/ux/DESIGN.md` exists with non-empty content → mode = **edit**, skip to Edit-mode flow
2. CSS / SCSS / theme files / Tailwind config / styled-components / design-system package detected → flag as brownfield candidate (default variant C)
3. No code, no styles → flag as greenfield candidate (no default)
4. Continue to Phase 2

## Phase 2 — variant choice (only if Phase 1 did not route to edit)

AskUserQuestion (3 options) — `question`, `header`, all option `label`s and `description`s rendered in `language.chat` per `language-policy.md` §AskUserQuestion options:
- **A** — Greenfield, designer proposes from concept + stack
- **B** — I have constraints (brand color, typography, density, platform)
- **C** — Brownfield, extract from existing code/styles

Phase 1 brownfield candidates auto-suggest C as default.

## Phase 3 — flow per variant (each dispatches `asd-ux-designer`)

**Variant A — greenfield**
- Dispatch `asd-ux-designer` with payload: concept.html, stack.html, language settings, targets = `t_design-system.html`, `t_accessibility.html`
- Designer reads concept (vision, target users, value prop, tone) and stack (UI platform, framework constraints)
- Proposes 2-3 candidate token sets per section (color palette, typography scale, spacing scale, radii, motion)
- AskUserQuestion: pick set or request alternatives — labels/descriptions rendered in `language.chat`
- Proceed to Phase 4

**Variant B — constrained**
- AskUserQuestion with `tabs` form (multi-field one-shot) — every field label, hint, and option rendered in `language.chat`:
  - Brand color (hex / "no preference")
  - Typography preference (system / serif / sans-serif / monospace / specific family / "no preference")
  - Density (compact / comfortable / spacious / "no preference")
  - Target platform (web / native-mobile / native-desktop / cli / mixed)
- Dispatch designer with concept + stack + constraints
- Designer proposes within constraints
- Proceed to Phase 4

**Variant C — brownfield extraction**
- Dispatch designer with style/component paths (Glob results)
- Designer extracts tokens from CSS variables, Tailwind config, theme objects, styled-components themes, design exports
- Draft sets `provenance: reverse-engineered` + `source: <primary file>` in frontmatter
- Proceed to Phase 4

## Phase 4 — convergence (universal across variants)

Section-by-section discussion in `language.chat`. Order per Google Labs DESIGN.md spec:
1. Colors (palette + semantic tokens)
2. Typography (scale + families)
3. Spacing scale
4. Radii / borders
5. Shadows / elevation
6. Motion / timing
7. Components (button, input, card, etc.) — only those needed per concept

For each section:
- Designer presents current content
- WebFetch latest Google Labs DESIGN.md spec on first section; cache for session
- AskUserQuestion (options form) — labels/descriptions rendered in `language.chat`: **A) Lock in / B) Revise this section / C) Skip (optional sections only)**
- on B: collect feedback, designer revises, re-present, re-ask
- repeat until A
- proceed to next section

After all DESIGN.md sections approved:
- Designer runs `designmd-lint` via Bash (`commands.yaml` alias). On Windows, ensure `designmd-install` ran once this session.
- Pass criteria per `.asd/rules/design-system.md` §11: ≥1 error OR ≥1 un-excluded warning = fail.
- Fail → designer fixes, re-lint. For each persistent warning, AskUserQuestion to exclude; on approval record decision + rationale in DESIGN.md lint-exclusions block.
- Clean pass → continue

## Phase 5 — design-system.html regeneration

- Designer renders `design/ux/design-system.html` per `t_design-system.html` from approved DESIGN.md
- Live previews: color swatches with hex, typography samples, spacing scale, component previews using applied tokens
- AskUserQuestion before write — labels/descriptions rendered in `language.chat`
- Wrap in `t_html-shell.html` (DOC_TYPE=Design-system, SUBSYSTEM=project)

## Phase 6 — accessibility baseline

- Designer authors `design/ux/accessibility.html` per `t_accessibility.html`
- Sections: visual (contrast, color-blind, motion), motor (target size, keyboard), cognitive (language, predictability), auditory (captions, transcripts), platform (focus order, ARIA, screen reader)
- Section-by-section AskUserQuestion lock-in — labels/descriptions rendered in `language.chat`
- Wrap in `t_html-shell.html` (DOC_TYPE=Accessibility, SUBSYSTEM=project)

## Phase 7 — final approval + write

- Designer shows full assembled design system + accessibility summary
- AskUserQuestion — labels/descriptions rendered in `language.chat`: **A) Approve, write all three files / B) Revise specific section** (on B re-enter Phase 4 or Phase 6)
- on A: translate to `language.docs`, write `design/ux/DESIGN.md`, `design/ux/design-system.html`, `design/ux/accessibility.html`
- emit COMPLETED

## Phase 8 — handoff

- Dispatch `asd-pm` to append decisions-log entry ("design system defined: <N tokens, M components>" / "design system edited" / "design system reverse-engineered, source: <path>")
- Print handoff suggestion: "Next: run `/asd-sprint` to start the first sprint" (or continue current sprint if dispatched from `asd-phase-design`)
- NO auto-dispatch

## Edit mode (Phase 1 routed here)

- Show existing DESIGN.md token summary, design-system.html freshness, accessibility.html sections
- AskUserQuestion: multi-select which files / sections to edit (DESIGN.md sections, regenerate design-system.html only, accessibility.html sections) — labels/descriptions rendered in `language.chat`
- per chosen section: enter Phase 4 or Phase 6 loop
- Phase 5, 7, 8 as usual

## AskUserQuestion form rules

- Multi-field one-shot (constraints tabs) → use `tabs` field
- Single-choice branching (A/B/C, lock-in/revise) → use `options` field
- Never mix; mixing produces "Invalid tool parameters" error

## Hard rules

- EVERY `AskUserQuestion` call (question text, header, all option labels, all option descriptions, tab field labels and hints) MUST be rendered in `language.chat` from `.asd/project/config.yaml`. Applies to control options too (Lock in / Revise / Skip / Approve / etc.). Per `.asd/rules/language-policy.md` §AskUserQuestion options. Internal signal tokens (`COMPLETED`, `FAILED`, `QUESTION`, `ABORT`) stay English — they are machine signals.
- NEVER author accessibility rules without checking concept's target users
- Token authoring + review bound by `.asd/rules/design-system.md`; UX shaping bound by `.asd/rules/ux-principles.md`
- design-system.html MUST be regenerated whenever DESIGN.md changes; stale render = FAIL
- `designmd-lint` MUST pass before write (clean pass per `.asd/rules/design-system.md` §11); warning exclusions need user approval + recorded rationale
- Every component listed in DESIGN.md MUST have a live preview in design-system.html

## Artefacts produced
- `design/ux/DESIGN.md` (created, edited, or reverse-engineered)
- `design/ux/design-system.html` (regenerated from DESIGN.md)
- `design/ux/accessibility.html` (created or edited)
- decisions-log entry

## Agents dispatched
- `asd-ux-designer` (author / scanner / lint / preview render / accessibility baseline)
- `asd-pm` (decisions-log)

## Skills dispatched
None.

## Return contract (single line)
```
DESIGN-SYSTEM: <fresh|edit|reverse-engineered> | VARIANT: <A|B|C|edit> | TOKENS: <count> | COMPONENTS: <count> | STATUS: <complete|aborted> | NEXT: <suggested-skill-or-sprint>
```

## References
- `.asd/templates/t_design-system.html`, `t_accessibility.html`, `t_html-shell.html`
- `.asd/rules/core.md`
- `.asd/rules/language-policy.md` (binding for every AskUserQuestion call in this skill)
- `.asd/rules/artifact-layout.md` (DESIGN.md path, design-system.html path, accessibility.html path)
- Google Labs DESIGN.md spec: https://github.com/google-labs-code/design.md
