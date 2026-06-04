---
responsibility:
  owns: external review aggregation report (kept/dropped accounting per iteration)
  excludes: codex raw prompt, internal reviewer output
  delegates_to: t_prompt-external-{design,impl}.md (prompts), t_review.md (internal reviewer output)
---

[REVIEW-{{PHASE}}-external]: {{APPROVE | CONCERNS | FAIL}}

# External Review Report

- **Phase**: {{design-review | impl-review}}
- **Iteration**: {{N}}
- **Severity floor (this iter)**: {{low | high | critical}}

## Kept findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | {{sev}} | {{location}} | {{description}} | {{fix}} |

## Dropped findings (below severity floor)

| # | Severity | Location | Description | Drop reason |
|---|---|---|---|---|
| 1 | {{sev}} | {{location}} | {{description}} | below floor on iter {{N}} |

## Dropped findings (nitpick)

| # | Location | Description | Drop reason |
|---|---|---|---|
| 1 | {{location}} | {{description}} | {{nitpick category from prompt}} |

## Verdict
{{APPROVE | CONCERNS: <count> | FAIL: <count>}}

## Next action
{{what creator/PM must do next}}
