# Review Policy

## Severity levels

| Level | Definition | Example |
|---|---|---|
| critical | breaks build, security hole, data loss, contract violation | unhandled secret leak, broken migration |
| high | wrong behavior, missing acceptance criterion, test gap on core path | requirement T3 not implemented |
| medium | bad pattern with concrete risk, weak edge-case coverage | shared mutable state, no test for empty input |
| low | style, minor clarity, micro-inefficiency | naming inconsistency |

## Iteration severity floor

Config sets `iterations_<severity>` (default: low=1, medium=1, high=2, critical=10). Each tier gets its own consecutive iteration budget in order low → medium → high → critical. On iteration N, the floor is the tier whose **cumulative budget** first covers N. Only findings at floor severity or higher count.

`N` is the **phase-local** counter — `reviews.design.iteration` or `reviews.impl.iteration` (see `sprint-lifecycle.md`). Each review phase computes its floor from its own counter.

Cumulative budgets with defaults:

- low: cum 1 → iter 1 → floor=low (all)
- medium: cum 2 → iter 2 → floor=medium (drop low)
- high: cum 4 → iters 3-4 → floor=high (drop low, medium)
- critical: cum 14 → iters 5-14 → floor=critical (drop low, medium, high)
- iter ≥ 15 → stop, escalate to user

User may override the cap. On override the counter keeps incrementing (not reset); floor stays pinned at `critical`, so extra rounds do not re-admit lower-severity findings.

## Clean-context review iteration

Every iteration dispatches each reviewer as a **fresh agent invocation** — new context, no carry-over from authoring or prior iterations. Isolates each verdict from creator reasoning and earlier rounds.

- The dispatching phase skill spawns every reviewer (and External Review) anew each iteration. No reviewer is reused or resumed.
- Reviewer payload carries only: the artifact/diff under review, rule references, severity floor, iteration number, context paths. Never authoring rationale or prior verdicts.
- Reviewers MUST NOT read prior `reviews/<phase>/iter-*/` files. Only the current `iter-NN/` directory.
- Incremental diff scoping (iter 2+ reviews only what changed — see `external-review.md`) narrows the *input*, not context. The agent is still fresh.
- Where a reviewer genuinely needs prior-iteration data (External Review stalemate detection), the phase skill supplies it as explicit payload input — scoped data, not context carry-over.

## Over-engineering checklist (critical, undroppable)

Simplification reviewer flags any of these as `critical`:

- Interface with exactly one implementer
- Generic with exactly one concrete type parameter
- Factory for fewer than three classes
- Plugin system with no plugin
- Abstraction with no second use case
- Premature config flag (no caller chooses non-default)
- Defensive code for impossible-by-contract case
- Helper that wraps one stdlib call without added value
- Inheritance depth ≥ 3 without polymorphic dispatch
- Framework wrapping a framework
- Mock of a mock in tests
- Comment that restates code
- Dead code left "in case we need it"

## Autofix vs escalation

Default: the responsible creator autofixes any reviewer issue without user prompt.

**Where the fix happens:**
- **design-review** — the creator (asd-ba / asd-ux-designer / asd-architect) autofixes within the loop; iteration advances.
- **impl-review** — fixes NOT applied inside the review phase. impl-review routes the sprint back to `impl` (fix mode); the responsible dev resolves findings; sprint re-enters impl-review.

**Escalation required** (ask user before fix), format = Complication Approval (`core.md`):

- Change to approved concept, PRD requirement, or API contract
- New abstraction, layer, interface, or dependency
- Scope expansion beyond `sprint.md`
- Complexity increase (any over-engineering check trips)

## Nitpick drop list (reviewers must NOT raise)

- Pure wording polish
- Opinion-only style
- Alternative naming with no concrete bug
- `you could also` without identifying a defect
- Speculative future-proofing

## Verdict format

Every reviewer ends with exactly one:

- `APPROVE` — no issues at or above floor severity
- `CONCERNS: <list>` — issues exist but the creator can autofix without escalation
- `FAIL: <list>` — issues require escalation or block DoD

Next action: APPROVE → reviewer done · CONCERNS → creator autofixes, next iteration · FAIL → escalate to user.

## Gate Verdict Format (machine-parseable first line)

Every reviewer output file MUST begin (after frontmatter) with a single-line verdict token:

```
[REVIEW-<phase>-<reviewer>]: <APPROVE | CONCERNS | FAIL>
```

- `<phase>` = `design` (design-review) or `impl` (impl-review)
- `<reviewer>` = `quality | implementation | testing | ui | simplification | documentation | performance | external`

Examples: `[REVIEW-impl-quality]: APPROVE` · `[REVIEW-design-documentation]: FAIL` · `[REVIEW-impl-external]: CONCERNS`

Never bury the verdict in prose. PM reads the first non-empty content line.

## DoD per review phase

| Phase | Required reviewers (all APPROVE same iteration) |
|---|---|
| design-review | Documentation, UI, Simplification, External Review (if enabled) |
| impl-review | Quality, Implementation, Testing, UI, Simplification, Documentation, Performance, External Review (if enabled) |

External Review counts as one reviewer when `review.external_review: enabled`. On DoD met, the phase advances.
