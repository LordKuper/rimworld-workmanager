---
responsibility:
  owns: sprint scope, goal, top-level acceptance criteria
  excludes: task breakdown, design decisions, code, audit findings
  delegates_to: plan.md (tasks), design/ docs (decisions), audit.md (audit)
---

# Sprint 001-full-audit-alignment

## Goal
Conduct a full audit of the WorkManager mod — both code (`Source/`: production code, tests, build/config) and documentation (`design/`, `.asd`, README). Bring the project into compliance with all four reference standards: the `.asd` rules (`rules/*` plus `custom-*-rules.md`), the defined technology stack (`design/architecture/stack.html`), the `LordKuper.Common` upstream contract, and the code conventions (code-style, nullable annotations, XML documentation). During the audit, fix all detected errors and issues, and identify deficiencies and opportunities for optimization or simplification. Implement all findings within this sprint.

### Scope decisions
1. **Audit breadth** — Code AND documentation. In scope: `Source/` (production code, tests, build/config) and documentation (`design/`, `.asd`, README).
2. **Compliance targets** — All four reference standards must be reconciled:
   - `.asd` rules: `rules/*` plus `custom-*-rules.md`.
   - Technology stack: `design/architecture/stack.html`.
   - `LordKuper.Common` upstream contract.
   - Code conventions: code-style.md, nullable annotations, XML documentation.
3. **Implementation boundary** — Everything in this sprint. All findings are implemented now; nothing is deferred to a later sprint. The sprint may be large.

## Acceptance
- Production build (`Source/WorkManager.slnx`) compiles clean: no errors, no new warnings.
- Test suite (`Source/WorkManager.Tests`) builds and passes green.
- `.asd` rules compliance reconciled: every divergence from `rules/*` and `custom-*-rules.md` is either fixed or recorded as an approved, documented exception.
- Technology-stack compliance reconciled: code and project config align with `design/architecture/stack.html`; mismatches resolved.
- `LordKuper.Common` upstream-contract compliance reconciled: only the public surface is consumed, no fork/reimplementation of provided functionality, compile-only reference settings correct.
- Code-conventions compliance reconciled: code-style adhered to, nullable annotations correct across the codebase, XML documentation present and accurate on the public surface.
- Documentation (`design/`, `.asd`, README) brought into alignment with the audited code and the four standards.
- Every audit finding (error, deficiency, optimization, or simplification opportunity) is implemented in this sprint — none deferred (per decision 3).

## Out of scope
- Editing the `LordKuper.Common` upstream library itself (consumed as an integration contract, not editable from this repo).
- Modifying ASD workflow infrastructure (`.asd/rules/`, `.asd/templates/`, `.claude/`, `CLAUDE.md`).
- New feature work unrelated to audit findings.
