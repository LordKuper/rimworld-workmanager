# Sprint Lifecycle

## Phases (all mandatory)

```
scope → audit → design → design-review → design-promote → plan → impl ⇄ impl-review → pr
```

`impl` and `impl-review` form a cycle: when impl-review finds issues it does NOT fix them — it routes the sprint back to `impl` (fix mode), then impl returns to `impl-review`. Repeats until impl-review reaches DoD (all reviewers APPROVE) or the iteration cap is hit. Phase routing follows the `NEXT:` token in each phase skill's return contract, not a fixed linear chain.

## Review iteration counters

Two independent counters in `state.json`, one per review phase:

- `reviews.design.iteration` — design-review iterations
- `reviews.impl.iteration` — impl-review iterations

Each review phase reads, increments, and reports only its own counter. They never share a value; the severity-floor budget (`review-policy.md`) is computed per counter.

**Lifecycle:**

- Both created at `0` when the sprint is initialised in `scope` (per `t_state.json`).
- Each is incremented at the **start of every entry** of its phase (`1` on first entry). `design-review` is entered once and loops internally. `impl-review` is re-entered each `impl⇄impl-review` cycle; the intervening `impl` fix-mode phase does not touch the counter — it accumulates across the whole cycle.
- **Rollback reset.** When `state.json.phase` is set strictly earlier in the chain than a review's input-producing phase, that counter resets to `0`, its severity floor resets, and its `verdicts` clear. Input-producing phases: `design` for design-review, `impl` for impl-review.

  | Counter | Resets when phase set to |
  |---|---|
  | `reviews.design.iteration` | `scope`, `audit` |
  | `reviews.impl.iteration` | `scope`, `audit`, `design`, `design-review`, `design-promote`, `plan` |

  Setting phase to `impl` is not earlier than `impl` — the normal `impl⇄impl-review` re-entry never resets. Reset fires only on a genuine rollback (the `asd-sprint` resume menu's *re-run earlier phase*). Rationale: once the artifact under review is re-created from an earlier phase, prior review rounds are void.
- On iteration-cap override, the counter keeps incrementing — not reset. Severity floor stays pinned at `critical`.

Verdict files: design-review → `<sprint>/reviews/design/iter-NN/`, impl-review → `<sprint>/reviews/impl/iter-NN/`, `NN` = that phase's own counter.

## Phase table

| Phase | Owner | Input | Output | Exit criteria |
|---|---|---|---|---|
| scope | PM | user request | `sprint.md`, sprint id, branch | scope approved, branch created |
| audit | Architect + BA | `sprint.md`, codebase, `design/`, existing docs any format/location | `audit.md`; optional reverse-engineered/migrated drafts in `<sprint>/design/` | audit approved |
| design | BA → UX Designer → Architect | `audit.md` | drafts in `<sprint>/design/` | drafts complete |
| design-review | Documentation + UI + Simplification + External Review | `<sprint>/design/` | `reviews/design/iter-NN/<reviewer>.md` | DoD met |
| design-promote | PM + Architect + BA + UX Designer | approved drafts | persistent docs in `design/` | drafts merged, decisions-log entry |
| plan | PM | promoted persistent docs | `plan.md` | plan approved |
| impl | Backend Dev + Frontend Dev + Test Engineer | `plan.md` (initial) or `reviews/impl/iter-NN/` findings (fix mode) | code + tests, `manual-steps.md` | all tasks/findings done; build + tests pass (completion gate) |
| impl-review | Quality + Implementation + Testing + UI + Simplification + Documentation + Performance + External Review | code + tests | `reviews/impl/iter-NN/<reviewer>.md` | DoD met → `pr`; else route to `impl` fix mode |
| pr | PM | everything | sprint archive + PR | PR opened (or push summary if `gh_enabled=false`) |

## Audit phase

Scans: existing source in touched areas; existing docs in **any format/location** (MD, RST, Confluence/Notion exports, HTML, Wiki, text-extractable PDF, READMEs outside ASD layout); persistent docs in `design/`.

Output `audit.md` — findings (touched areas, existing docs/code, gaps, risks) plus **Documentation migration plan** listing found external docs to promote into ASD format. Where sprint scope directly overlaps found content, the agent may pre-formulate reverse-engineered/migrated drafts in `<sprint>/design/` (prd/ux-spec/adr.html) with `provenance` + `source` frontmatter; these flow through design and design-review like any draft. Migration items not covered by drafts wait for design-promote.

## Design phase

Agents produce a unified draft set for the whole sprint scope in `<sprint>/design/`:

- `prd.html` — requirements + acceptance criteria
- `ux-spec.html` — flows + accessibility notes
- `adr.html` — architecture decisions
- `design-md-delta.yaml` — proposed DESIGN.md token changes, produced inline during UX-spec authoring (only on token gap; each entry user-approved)
- `c4-full/` — full LikeC4 schema for sprint scope (`model/*.c4`, `views.c4`, `dist/`)

Order: PRD blocks design-system gate. Design-system gate (existence check on `design/ux/DESIGN.md`, `design-system.html`, `accessibility.html`; dispatches `/asd-design-system` when any missing) blocks UX-spec. UX-spec blocks ADR. ADR blocks c4-full. If `subsystem_decomposition: disabled`, `c4-full/` omitted.

## Design-promote phase

PM orchestrates; three domain creators promote (Documentation reviewer NOT involved):

1. PM proposes per-subsystem decomposition (only when `subsystem_decomposition: enabled`); user approves split.
2. PM proposes new subsystems inferred from drafts; user approves each (name, parent container, description). On approve: Architect patches C4 registry, creates folders, runs `likec4 build` if applicable.
3. PM distributes `audit.md` migration items to the matching domain (architecture/product/ux/api).
4. Parallel promotion:
   - `asd-ba` → per-subsystem (or flat) `design/product/requirements/<subsystem>.html` from prd draft; product migration items.
   - `asd-architect` → `design/architecture/adr/<subsystem>/adr-NNNN-<slug>.html`; updates `api/<subsystem>.html`, `stack.html`, `tech-reference/`; applies c4 delta to persistent `design/architecture/c4/`; regenerates `dist/` (likec4) or `architecture.html` (mermaid); architecture migration items.
   - `asd-ux-designer` → `design/ux/<subsystem>.html` from ux-spec draft; patches `DESIGN.md` from `design-md-delta.yaml`; regenerates `design-system.html`; ux migration items.
   - Each creator AskUserQuestion before each persistent write.
5. PM final user confirmation before persistent mutation (confirm / rollback / partial rollback).
6. PM appends decisions-log entries, finalises `state.json`.

If `subsystem_decomposition: disabled`: drafts merge into flat project-level docs (`requirements.html`, `adr/adr-NNNN-<slug>.html`, `api.html`, `ux-spec.html`). No subsystem folders, no c4 model.

## Impl phase

Devs implement plan tasks. When a subtask needs a human-only operational action (secret, cloud resource, hand-run migration, env var, third-party account), the dev registers an `MS-N` entry in `<sprint>/manual-steps.md`, marks the subtask `BLOCKED: MS-N` in `plan.md`, emits `BLOCKED_MANUAL`, continues all unblocked work. PM validates each `MS-N` for necessity (`artifact-layout.md`); autonomously-doable entries are rejected and returned to the dev. Once all unblocked work is COMPLETED and validated `pending` entries remain, the phase halts: PM presents `manual-steps.md`, waits for a continue command. On resume the dev verifies each entry per its `Verification` field, flips it to `done`, finishes the blocked subtasks.

**Modes** — detected from `state.json.review_fixes_pending`:

- **Initial** (`review_fixes_pending` null/absent) — implement `plan.md` tasks. Ends with the user-facing impl assessment gate, then `NEXT: impl-review`.
- **Fix** (`review_fixes_pending` = `iter-NN`) — entered when impl-review routed back. Devs read findings in `<sprint>/reviews/impl/iter-NN/`, resolve every CONCERNS finding plus every user-approved FAIL finding, return `NEXT: impl-review`. Skips the impl assessment gate; blockers escalate as in initial mode. Clears `review_fixes_pending` on completion.

**Completion gate** (both modes) — impl MUST NOT emit `COMPLETED` until, verified via `commands.yaml`: `build` ran with no errors/warnings, `test` ran, every test passed. On failure: devs fix and re-run; unrecoverable failure escalates as `FAILED`. Automatic verification, not a user pause.

## Signal vocabulary

- `COMPLETED` — phase work done, ready for next
- `FAILED` — cannot proceed, reason in body
- `REVIEW_DONE` — reviewer finished, verdict in body
- `QUESTION` — needs user input, body has options
- `PLAN_DRAFT` — plan written, not approved
- `PLAN_READY` — plan approved
- `BLOCKED_MANUAL` — task needs a human-performed manual action; entry registered in `manual-steps.md`

## Plan file format

See `t_plan.md` for canonical structure.

## Sprint immutability

A closed sprint folder under `.asd/sprints/archived/<NNN-slug>/` is read-only. Follow-up work creates a new sprint.

## State recovery

`state.json` is the single recovery point. Updated on every phase transition, task status change, review verdict. Session-start hook reads it and prints a summary into context.
