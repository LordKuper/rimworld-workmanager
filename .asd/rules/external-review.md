# External Review

External Review agent runs Codex CLI in parallel with internal reviewers during both `design-review` and `impl-review`, merging findings into the common issue pool.

## Enablement

Controlled by `review.external_review` in config (`enabled` | `disabled`). If `disabled`: agent does nothing, no log entry.

## OS-specific invocation

OS read from `system.os` in config (set by `/asd-init`).

Prompt is passed via a **temp file**, not inline: agent writes rendered prompt template + diff payload concatenated to `<in-file>` = `<sprint>/reviews/<design|impl>/iter-NN/codex-input.tmp`, pipes it to Codex stdin, then deletes it after the run. `<out-file>` = Codex final message (text verdict per prompt), parsed by agent.

| OS | Probe | Review command |
|---|---|---|
| windows | `codex.exe --version` (PowerShell) | `Get-Content <in-file> -Raw \| codex.exe exec -o <out-file> -` |
| linux | `codex --version` (bash) | `cat <in-file> \| codex exec -o <out-file> -` |
| macos | `codex --version` (bash) | `cat <in-file> \| codex exec -o <out-file> -` |

`codex exec -` reads prompt+diff from stdin; `-o <out-file>` writes Codex's final message to file. No `--json` — prompt yields text verdict, not event stream.

If `system.tools.codex_command` is set in config, it overrides the default command path (`codex`/`codex.exe`) for all OSes.

## Detection

At review phase start, the agent runs the probe. On failure (non-zero exit, command not found):

- Append to `decisions-log.md`: `Codex CLI unavailable, external review skipped for sprint <NNN-slug> iter <N>`
- Continue without external review, no user prompt

## Iteration-aware diff

| Phase | Iteration | Diff source |
|---|---|---|
| design-review | 1 | full content of `<sprint>/design/` files |
| design-review | 2+ | per-file diff since previous iteration snapshot |
| impl-review | 1 | `git diff <git.base_branch>...HEAD` |
| impl-review | 2+ | `git diff` (uncommitted) plus the last commit (`git show HEAD`) |

Iteration 1 covers all sprint work in that phase; later iterations cover only what changed since the last round. For design-review, each iteration persists a file snapshot to disk; the next iteration reads that snapshot to compute its diff.

The agent is dispatched fresh each iteration (`review-policy.md` clean-context). The incremental diff narrows the *input*, not context.

## Output mapping

Codex severity terms in `<out-file>` mapped to ASD severity:

| Codex | ASD |
|---|---|
| blocker, critical | critical |
| major | high |
| minor | medium |
| info, suggestion | low |

Findings rendered to the review output dir supplied by the dispatching phase skill (`reviews/design/iter-NN/external.md` or `reviews/impl/iter-NN/external.md`), using the verdict format from `review-policy.md`.

## Stalemate detection

The phase skill supplies the previous iteration's finding set as explicit payload input (from iteration 2). The agent compares against that supplied set only — it does not read prior `iter-*/` files.

If two consecutive iterations produce an identical issue set (same files, lines, messages), the agent emits `FAIL: stalemate after <N> iterations, identical findings` and escalates to the user with options: accept findings as-is, override, abort sprint.

## Aggregation

External Review verdict counts as one reviewer in the DoD check. APPROVE from External Review is required when `external_review: enabled`.
