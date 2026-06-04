# Language Policy

## Matrix

| Artifact type | Language |
|---|---|
| Chat with user | `language.chat` from config |
| User-facing artifacts (PRD, ADR, UX-spec, plan.md, sprint.md, reviews, design/* docs) | `language.docs` from config |
| Machine-readable files (state.json, config.yaml, settings.json, env files) | English, dense, always |
| Workflow infrastructure (rules, agents, skills, hooks, CLAUDE.md) | English, dense imperative prose, always |
| File names, git branches, commit subjects, PR titles | English, always |
| `DESIGN.md` (Google Labs format) | English, format-mandated YAML + Markdown |
| LikeC4 `.c4` files | English, format-mandated DSL |

## Translate at write time

Agents may reason internally in `language.chat`. Before `Write`/`Edit` to a user-facing artifact, translate to `language.docs`. Before writing a machine file, render in English.

## Quote translation

When an agent quotes a user-facing artefact during chat, the quote MUST be rendered in `language.chat`, even if the source file is in another language. Translate inline. Cite the source path so the user can open the original.

## AskUserQuestion options

Every interactive option presented to the user — `AskUserQuestion` `question`, `header`, every option `label`/`description`, the Problem/Options/Recommended/Consequences block, any free-text approval prompt — MUST be rendered in `language.chat`. Includes standard control options (`Lock in` / `Revise this section` / `Skip` / `Approve` / `Edit` / `Reject` / `Confirm` / `Rollback` / etc.). Translate at the call site; never emit English option labels when `language.chat` is non-English.

Internal signal tokens crossing the agent boundary (`COMPLETED`, `FAILED`, `QUESTION`, `ABORT`, `APPROVE`, `CONCERNS`, return-contract strings) stay in English — machine signals, not user-facing text.

Free-text approval ("ok", "да", "approve") is NOT a substitute for `AskUserQuestion` at any HARD gate in `checkpoints.md`. The gate requires a discrete-option call so approval is unambiguous and auditable.
