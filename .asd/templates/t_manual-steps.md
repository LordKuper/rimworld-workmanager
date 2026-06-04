---
responsibility:
  owns: per-sprint registry of manual operational actions a human must perform for the sprint plan to complete
  excludes: code todo stubs (stubs.md), manual QA verification of behaviour (reviews testing.md), plan tasks (plan.md)
  delegates_to: stubs.md (code stubs), plan.md (tasks + BLOCKED markers), reviews/ (manual verification of behaviour)
---

# Manual Steps

Per-sprint registry of manual operational actions a human MUST perform for the sprint plan to complete — provisioning a secret or API key, creating a cloud resource, running a migration by hand, setting an env var, registering a third-party account.

Created lazily: the file exists only when at least one manual action arose. Devs append entries (append-only; never edit-in-place an existing entry's identity). Entry content is `language.docs`.

Not the same as:

- `stubs.md` — open code TODO stubs.
- manual verification spec (reviews `testing.md`) — manual QA of *behaviour*.

Manual steps = operational *setup* actions the agent cannot perform autonomously.

PM validates every new entry for necessity before the impl phase halts. An entry is kept only when the action genuinely cannot be done autonomously (needs access, a secret, an external account, or an authority the agent lacks). If PM judges it autonomously doable, the entry is rejected and the task returns to the dev to implement directly.

Status is `pending` → `done`. The dev that registered an entry flips it to `done` only after running its `Verification`. File is sprint-scoped and archived with the sprint.

## Summary

| ID | Title | Blocks | Performed by | Status |
|---|---|---|---|---|
| {{MS-N}} | {{title}} | {{Task N / subtask(s)}} | {{role}} | {{pending\|done}} |

<!-- when no manual action arose, this file is not created -->

## MS-{{N}} — {{title}}

- **Blocks**: {{Task N — subtask(s)}}
- **Why**: {{AC-N / reason the plan needs this}}
- **When**: {{before first run | before deploy | …}}
- **Prerequisites**: {{what must exist first}}
- **Performed by**: {{role — typically user}}
- **Status**: {{pending | done}}

### Steps

1. {{step}}
2. {{step}}

### Verification

{{how the workflow confirms completion — a commands.yaml check command, observable state to inspect, or explicit user confirmation when no automated check exists}}
