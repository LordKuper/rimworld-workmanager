---
name: asd-phase-scope
description: "Runs the ASD scope phase of a sprint: creates the sprint folder, state.json, and git branch, then dispatches asd-pm to refine the raw user scope into an approved sprint.md. Use when asd-sprint dispatches the scope phase for a new sprint, or when the user explicitly asks to run or re-run the scope phase for the active sprint."
metadata:
  asd-role: phase
  asd-order: "1"
  version: "0.1"
allowed-tools: "Read Glob Bash AskUserQuestion Task"
---

# ASD Phase: Scope

## Preconditions
- `.asd/project/config.yaml` exists
- No active sprint OR user explicitly re-runs scope for current sprint
- `git status` clean (else: bail with FAILED)

## Tool policy
- Read — `.asd/project/config.yaml`, `.asd/sprints/` listing
- Glob — count existing sprints (active + archived) for next NNN
- Bash — `git status`, `git rev-parse`, `git checkout -b <branch>`
- AskUserQuestion — only if raw scope text was not provided by `asd-sprint`
- Task — dispatch `asd-pm` to refine scope and obtain approval

## Workflow

1. Read `.asd/project/config.yaml` (`git.base_branch`, `git.branch_pattern`)
2. Verify `git status` clean; if dirty → FAILED
3. Count existing sprints (`.asd/sprints/*/` + `.asd/sprints/archived/*/`) → NNN = max + 1, zero-padded
4. Derive slug from raw scope text (kebab-case, ≤30 chars) — provisional, may change after refinement
5. Construct sprint id `<NNN>-<slug>` and branch from `git.branch_pattern`
6. Checkout `git.base_branch`, pull (optional, confirm with user), create branch
7. Create folder `.asd/sprints/<NNN-slug>/`
8. Dispatch `asd-pm` via Task with payload:
   - **raw scope text** from user (treated as draft, not final)
   - sprint id, branch
   - templates: `t_sprint.md`, `t_state.json`
   - instruction (MUST follow in this exact order; skipping any step is a protocol violation):
     1. **Refine** the raw scope into a coherent, finished statement of the sprint goal (full sentences, written in `language.docs`, not caveman); preserve every concrete requirement the user mentioned. Refinement happens in chat only — DO NOT write any file yet.
     2. **Clarify** via AskUserQuestion when the raw text is ambiguous, contradictory, or missing concrete acceptance signals. Mandatory if any of: scope verb is vague ("improve", "refactor", "support X"), no measurable outcome, ≥2 plausible interpretations, missing target users/surface/data shape.
     3. **Present** the refined version to the user for explicit approval via AskUserQuestion with options `approve` / `edit` / `reject`. The approval call is mandatory even when the user's raw text looked complete — implicit approval is NOT allowed.
     4. If `edit` or `reject` → re-refine with feedback; loop back to step 3 until explicit `approve`.
     5. If the refined goal implies a better slug, propose new slug via AskUserQuestion and rename folder/branch only after confirmation.
     6. **Only after explicit `approve`**: write `<sprint>/sprint.md` per `t_sprint.md` and initial `state.json` (phase=scope, iteration=0, branch, created_at) per `t_state.json`. Append a decisions-log entry recording the approved scope.
     7. Emit COMPLETED.

   Hard gates (any violation → emit FAILED and halt):
   - No `Write`/`Edit` to `sprint.md` or `state.json` before AskUserQuestion approval has returned `approve`.
   - No phase advance signal (`COMPLETED`) before the file write has happened.
   - No batching of "refine + write" into a single turn without the intermediate AskUserQuestion.
9. On PM `COMPLETED` → emit COMPLETED with return contract
10. On PM `QUESTION` → relay to user, halt
11. On PM `FAILED` / `ABORT` → relay, halt

## Artefacts produced
- `.asd/sprints/<NNN-slug>/sprint.md` — approved refined scope
- `.asd/sprints/<NNN-slug>/state.json` — initial state
- git branch `sprint/<NNN>-<slug>` (slug may have been renamed during refinement)

## Agents dispatched
- `asd-pm` (single Task)

## Skills dispatched
None.

## Return contract (single line)
```
PHASE: scope | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: audit
```

## References
- `.asd/rules/sprint-lifecycle.md` (scope phase contract — SSoT)
- `.asd/rules/checkpoints.md` (approval gates)
- `.asd/rules/git-strategy.md` (branch creation, dirty tree rule)
- `.asd/rules/language-policy.md` (refined scope in `language.docs`)
- Templates: `t_sprint.md`, `t_state.json`
