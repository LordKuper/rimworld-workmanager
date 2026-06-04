---
name: asd-phase-pr
description: "Runs the final ASD pr phase: dispatches asd-pm to verify the Definition of Done, compose and open (or prepare) the PR per git config, then archive the sprint folder and finalize state. Use when asd-sprint dispatches the pr phase, or when the user explicitly asks to run or re-run pr for the active sprint."
metadata:
  asd-role: phase
  asd-order: "9"
  version: "0.1"
allowed-tools: "Read Glob Grep AskUserQuestion Task"
---

# ASD Phase: PR

## Preconditions
- Active sprint at `.asd/sprints/<NNN-slug>/`
- impl-review DoD met (all required reviewers APPROVE same iteration)
- `state.json.phase` advanced from `impl-review`

## Tool policy
- Read — `.asd/project/config.yaml`, `state.json`, plan.md, reviews/, `.asd/project/stubs.md`
- Glob/Grep — scan code for `// TODO(sprint-<NNN-slug>):` markers; verify against stubs.md
- AskUserQuestion — final PR-opening confirmation, rollback on failure
- Task — dispatch `asd-pm` for DoD check, PR creation, archival, decisions-log

## Workflow

1. Read `.asd/project/config.yaml` (`git.base_branch`, `git.branch_pattern`, `git.gh_enabled`, `git.auto_pr`, `language.chat`, `language.docs`)
2. Read `<sprint>/state.json` → confirm impl-review DoD met
3. Dispatch `asd-pm` via Task: update `state.json` (phase=pr)
4. **DoD verification** — dispatch `asd-pm` via Task with payload (config, sprint paths, stubs path, commands.yaml):
   - **Plan completion**: read `<sprint>/plan.md`, verify every `- [ ]` is `- [x]`
   - **AC coverage**: cross-check PRD AC-N references in plan tasks against impl-review documentation verdict
   - **Reviews green**: read the latest `<sprint>/reviews/impl/iter-NN/` (highest `reviews.impl.iteration`), parse first-line gate verdict tokens for all required reviewers; ALL must be `APPROVE`
   - **Stub block**:
     - read `.asd/project/stubs.md`; filter `Sprint = <current-NNN-slug>` AND Reason NOT starting with `(accepted-debt)` → must be empty
     - Grep code for `// TODO(sprint-<current-NNN-slug>):` markers; cross-check every marker has matching stubs.md entry (orphan markers = block)
   - **Tests pass**: run `commands.yaml` `test` command via Bash; non-zero exit = block
   - **Lint clean**: run `commands.yaml` `lint`; non-zero exit = block
   - **PR self-review checklist** (per `git-strategy.md`): PM confirms each item explicitly (studied existing code, can explain every line, scoped to feature, why-not-what commits)
   - on ANY block: relay specific failure to user; halt phase until fixed (user may dispatch fix or accept-debt)
5. **PR creation confirmation** — dispatch `asd-pm` via Task:
   - compose PR title and body via `t_pr-description.md`
   - AskUserQuestion in `language.chat`: confirm open PR / edit body / abort
   - on confirm:
     - **`git.gh_enabled=true` and `git.auto_pr=true`**: stage uncommitted (none should remain), push branch, run `gh pr create --title <title> --body <body> --base <base_branch>`
     - **`git.gh_enabled=true` and `git.auto_pr=false`**: push branch, print PR-ready summary (title, body, compare URL); wait for user to open PR manually
     - **`git.gh_enabled=false`**: push branch, print PR-ready summary (title, body, compare URL hint)
   - on edit: re-compose with user feedback, loop
   - on abort: emit ABORT
6. **Sprint archival** — dispatch `asd-pm` via Task:
   - move folder `.asd/sprints/<NNN-slug>/` → `.asd/sprints/archived/<NNN-slug>/` (git mv)
   - commit move with message `chore: archive sprint <NNN-slug>` and push to sprint branch
   - update `state.json` (phase=done, archived_at)
   - append decisions-log entry ("sprint <NNN-slug> completed, archived, PR <url-or-summary>")
7. Emit phase COMPLETED with return contract

## Block-on-fail behaviour

Any DoD check failing → halt with structured message:

```
PR BLOCKED — reason: <which check>
Details: <specific failure>
Action: <suggested next step>
```

User decides: fix and retry, accept-debt (stubs only), or abort sprint.

## Artefacts produced
- Pushed git branch (and PR when `gh_enabled+auto_pr`)
- Archived sprint at `.asd/sprints/archived/<NNN-slug>/`
- Archive commit on sprint branch
- decisions-log final entry
- Updated `state.json` (phase=done, archived_at)

## Agents dispatched
- `asd-pm` (DoD verification, PR composition, archival, decisions-log)

## Skills dispatched
None.

## Return contract (single line)
```
PHASE: pr | SPRINT: <NNN-slug> | STATUS: <complete|blocked|aborted> | NEXT: done | PR: <url-or-summary-or-none>
```

## References
- `.asd/rules/sprint-lifecycle.md` (pr phase contract, sprint immutability)
- `.asd/rules/git-strategy.md` (PR self-review checklist, branch ops, stubs block rule)
- `.asd/rules/checkpoints.md` (final PR confirmation gate)
- `.asd/rules/artifact-layout.md` (sprint archival path)
- `.asd/rules/language-policy.md` (PR title English, body docs-lang)
- Templates: `t_pr-description.md`, `t_decisions-log.md`
