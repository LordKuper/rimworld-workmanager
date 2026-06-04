---
name: asd-phase-design-review
description: "Runs the ASD design-review phase iteratively until DoD met: dispatches asd-reviewer-documentation, asd-reviewer-ui, asd-reviewer-simplification (and asd-external-review when enabled) in parallel against the sprint design drafts, aggregates verdicts, and routes CONCERNS to creator autofix or FAIL to user escalation. Use when asd-sprint dispatches design-review, or when the user explicitly asks to run or re-run design-review for the active sprint."
metadata:
  asd-role: phase
  asd-order: "4"
  version: "0.1"
allowed-tools: "Read AskUserQuestion Task"
---

# ASD Phase: Design Review

## Preconditions
- Active sprint at `.asd/sprints/<NNN-slug>/`
- Required design drafts present in `<sprint>/design/`: prd.html, ux-spec.html, adr.html (per checkpoints precondition chain)
- Optional drafts honored: design-md-delta.yaml, c4-full/
- `state.json.phase` advanced from `design`

## Tool policy
- Read — `.asd/project/config.yaml`, `state.json`, drafts in `<sprint>/design/`, review files
- AskUserQuestion — escalation on FAIL or iteration cap reached
- Task — parallel reviewer dispatch; sequential creator dispatch for autofix; PM for state + decisions-log

## Workflow

1. Read `.asd/project/config.yaml` (`review.external_review`, `review.iterations_low/medium/high/critical`, `language.chat`, `language.docs`)
2. Read `<sprint>/state.json` → set `phase=design-review`, increment `reviews.design.iteration` (it is `0` at sprint creation, so `1` on first entry; see `sprint-lifecycle.md` "Review iteration counters" for increment and rollback-reset rules). `NN` = the resulting value, zero-padded.
3. Compute severity floor for current iteration per `review-policy.md` cumulative-budget algorithm (uses `reviews.design.iteration`)
4. Create folder `<sprint>/reviews/design/iter-NN/` if absent
5. **Parallel dispatch** via Task — every reviewer is spawned as a **fresh agent** each iteration (clean-context dispatch per `review-policy.md`); no reviewer is reused across iterations:
   - `asd-reviewer-documentation` — SSoT, template adherence, traceability across drafts
   - `asd-reviewer-ui` — ux-spec compliance with DESIGN.md + accessibility.html
   - `asd-reviewer-simplification` — over-engineering smells + design-principles.md adherence
   - if `review.external_review=enabled` → `asd-external-review` with phase=`design-review`
   - payload to each: drafts paths, iteration N, review output dir `<sprint>/reviews/design/iter-NN/`, severity floor, `language.chat`, `language.docs`. The payload carries no authoring rationale and no prior-iteration verdicts. For `asd-external-review` on iter ≥ 2, also pass the previous iteration's finding set (for stalemate detection)
   - each reviewer writes `<sprint>/reviews/design/iter-NN/<reviewer>.md` per `t_review.md` (or `t_review-report.md` for external) with first-line verdict token `[REVIEW-design-<reviewer>]: ...`
6. Wait all REVIEW_DONE signals
7. Parse first-line tokens from all reviewer files; record the per-reviewer verdicts under `state.json` `reviews.design.verdicts["iter-NN"]`; aggregate:
   - **All APPROVE** → DoD met; PM appends decisions-log entry "design-review iter NN: APPROVE"; emit phase COMPLETED
   - **Any FAIL** → escalation:
     - parse FAIL findings; group by escalation cause (concept change / new abstraction / scope expansion / contract change)
     - AskUserQuestion in `language.chat`: present each FAIL using Complication Approval format from `core.md`; collect decisions
     - on user override → mark resolved, continue
     - on user accept → dispatch corresponding creator (BA / UX Designer / Architect) via Task to apply approved changes; on creator COMPLETED → loop step 2 (increment iteration)
   - **Only CONCERNS** (no FAIL) → autofix loop:
     - dispatch responsible creator(s) via Task with finding list; each creator autofixes per `review-policy.md` (no escalation needed)
     - on all creator COMPLETED → loop step 2
8. Iteration cap reached (no severity tier has remaining budget for next iter):
   - AskUserQuestion: override cap and continue / accept current findings / abort sprint
   - on override → loop step 2 (`reviews.design.iteration` keeps incrementing — not reset; severity floor stays pinned at `critical`)
   - on accept → COMPLETED with note "iteration cap reached, user accepted"
   - on abort → emit ABORT
9. Any reviewer QUESTION / FAILED / ABORT → relay, halt

## Iteration severity floor (reference)
See `.asd/rules/review-policy.md` for the cumulative-budget algorithm. Phase skill computes floor and passes to reviewer payload so reviewers drop findings below floor.

## Artefacts produced
- `<sprint>/reviews/design/iter-NN/documentation.md`
- `<sprint>/reviews/design/iter-NN/ui.md`
- `<sprint>/reviews/design/iter-NN/simplification.md`
- `<sprint>/reviews/design/iter-NN/external.md` (when `external_review=enabled`)
- Updated `<sprint>/design/` artifacts after autofix or escalation-approved fixes
- Updated `state.json` (phase, `reviews.design.iteration`, `reviews.design.verdicts`)
- decisions-log entry on DoD met or override

## Agents dispatched
- 3 internal reviewers (Documentation, UI, Simplification) — parallel
- External Review — parallel (when enabled)
- Creators (BA, UX Designer, Architect) — sequential, only when autofix or escalation requires
- PM — state updates + decisions-log

## Skills dispatched
None.

## Return contract (single line)
```
PHASE: design-review | SPRINT: <NNN-slug> | ITER: <N> | STATUS: <complete|blocked|aborted> | NEXT: design-promote
```

## References
- `.asd/rules/sprint-lifecycle.md` (design-review phase contract)
- `.asd/rules/review-policy.md` (severity floor, autofix, escalation, gate verdict format, DoD per phase)
- `.asd/rules/design-principles.md`
- `.asd/rules/checkpoints.md`
- `.asd/rules/language-policy.md`
- Templates: `t_review.md`, `t_review-report.md`
