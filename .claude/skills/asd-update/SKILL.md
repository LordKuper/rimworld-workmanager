---
name: asd-update
description: Updates the ASD framework infrastructure (.asd/rules, .asd/templates, ASD agents/skills/hooks) in a consumer project to the latest version by fetching them from the configured ASD repo's main branch, replacing only framework-managed paths and never touching consumer-owned config, sprints, design docs, or custom skills/agents/hooks. Use when the user runs /asd-update or asks to update, upgrade, or pull the latest ASD framework / workflow version.
---

# asd-update

Pull latest ASD framework files into this consumer project. Overwrites **framework-managed** paths only; leaves consumer-owned files intact.

## What it touches

Managed set = SSoT in `.asd/update-manifest.json`:
- `managed.trees` — dirs ASD owns fully, replaced wholesale (`.asd/rules`, `.asd/templates`).
- `managed.paths` — individual ASD agents, skill dirs, hook files. Listed one-by-one because they share `.claude/agents`, `.claude/skills`, `.claude/hooks` with the user's own custom items — those are never deleted.

Never touched: `.asd/project/**`, `.asd/sprints/**`, `design/**`, `CLAUDE.md`, `.claude/settings.json`, any non-ASD skill/agent/hook.

## Run

1. Confirm with user (it overwrites framework files): show what will update, offer dry run.
2. From project root: `node .claude/skills/asd-update/update.js`
   - Preview first: append `--dry-run` (lists deletes/copies, mutates nothing).
3. Updater flow (atomic-ish): fetch tarball from `repo`@`branch` → extract → validate every new managed path exists → **then** delete by local manifest list → copy by new list → refresh local `.asd/update-manifest.json`.
   - Fetch/validation failure = nothing changed.
4. Report version `old -> new` + count from script output.

## After

- Remind user: `.claude/settings.json` is **not** auto-updated (holds their permissions + hook registration). If the update changed hook files or added skills, they may need to reconcile it manually.
- Requires `tar` on PATH (ships with Win10 1803+/macOS/Linux) and Node >= 16.7.
