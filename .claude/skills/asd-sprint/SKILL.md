---
name: asd-sprint
description: "Starts a new ASD sprint or resumes the active one, dispatching the matching asd-phase-* skill and routing phase signals back to the user. Use when the user runs /asd-sprint or asks to start, continue, resume, or work on an ASD sprint."
metadata:
  asd-role: dispatcher
  version: "0.1"
allowed-tools: "Read Glob Grep Bash AskUserQuestion Skill"
---

# ASD Sprint

## Preconditions
- `.asd/project/config.yaml` exists (else: tell user to run `/asd-init`)
- One or zero active sprints in `.asd/sprints/<NNN-slug>/` (archived/ excluded)

## Tool policy
- Read/Glob/Grep — detect active sprint, read state.json, config.yaml, custom-common-rules.md
- Bash — `git status` (uncommitted check), `git branch --show-current`
- AskUserQuestion — new-sprint confirmation, resume/abort choice
- Skill — dispatch phase skills only
- No Write/Edit — phase skills and PM agent own all writes

## Workflow

### Step 1: detect active sprint
- Glob `.asd/sprints/*/state.json` (skip `archived/`)
- 0 active → new-sprint flow
- 1 active → resume flow
- >1 active → emit FAILED "multiple active sprints found, manual cleanup needed"

### Step 2A: new-sprint flow
1. Read `.asd/project/config.yaml` to confirm init complete
2. Check `git status` — if dirty, AskUserQuestion: commit / stash / abort
3. AskUserQuestion: confirm start new sprint; collect scope (free-form)
4. Dispatch `asd-phase-scope` via Skill, passing scope text
5. On COMPLETED from `asd-phase-scope` → advance per Step 3

### Step 2B: resume flow
1. Read `.asd/sprints/<NNN-slug>/state.json`
2. Show user: sprint id, current phase, review iteration (`reviews.design.iteration` when phase is `design-review`, `reviews.impl.iteration` when phase is `impl-review`), last review verdict (if any)
3. AskUserQuestion: resume (default) | re-run current phase | re-run earlier phase | abort sprint
4. Dispatch matching phase skill via Skill. *re-run earlier phase* is a rollback: when the target phase is strictly earlier than a review's input-producing phase, the target phase skill's PM state update resets that review counter and severity floor per the **rollback reset** rule in `sprint-lifecycle.md` (`reviews.design.iteration` resets when rolling back to `scope`/`audit`; `reviews.impl.iteration` resets when rolling back to `scope`…`plan`).

### Step 3: phase chain advancement
After any phase skill returns:
- `COMPLETED` → read the `NEXT:` field of the phase skill's return contract and dispatch that phase skill. `NEXT:` is authoritative — it follows the default linear order in `.asd/rules/sprint-lifecycle.md` except that `impl` and `impl-review` cycle: `impl-review` returns `NEXT: impl` when it found unresolved findings (routes the sprint to impl fix mode) or `NEXT: pr` on DoD met; `impl` always returns `NEXT: impl-review`. On `pr` COMPLETED the sprint is archived and the chain ends.
- `FAILED` → relay to user, halt
- `QUESTION` → relay pending question to user, halt until reply
- `ABORT — precondition not met` → relay, halt

User may interrupt at any time; asd-sprint re-detects state on next invocation.

## Artefacts produced
None directly. All file writes happen inside phase skills (via PM, creators, reviewers).

## Agents dispatched
None directly. Phase skills dispatch agents via Task.

## Skills dispatched
Phase skills listed in `.asd/rules/sprint-lifecycle.md`. No other skill set.

## Return contract (single line)
```
SPRINT: <NNN-slug> | PHASE: <phase> | STATUS: <complete|in-progress|blocked|aborted> | NEXT: <next-phase|done|halted-on-question|halted-on-failure>
```

## References
- `.asd/rules/sprint-lifecycle.md` (phase chain, signals, exit criteria)
- `.asd/rules/checkpoints.md` (precondition chain, auto-abort)
- `.asd/rules/core.md` (interaction protocol)
