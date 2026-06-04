---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

## Scope reference
[sprint.md](./sprint.md)

## Touched areas
- {{path or area}}: {{what scope touches here}}

## Existing docs found
- [{{title}}]({{path}}): {{quote or summary of relevant part}}

## Existing implementation found
- {{path}}: {{what scope already covered by current code}}

## Gaps
- {{missing piece needed by scope}}

## Risks
- {{risk}}: impact={{impact}}, mitigation={{mitigation}}

## Subsystems map (optional, decomposition enabled)
- {{subsystem id from c4 model}}: {{relation to scope}}

## Dependencies (optional)
- {{external dep}}: {{usage}}

## Migration notes (optional)
- {{what migrates}}: {{from → to}}

## Related open stubs (optional)

Open stubs from `.asd/project/stubs.md` that touch files or subsystems in this sprint's scope. Surfaced for user decision in plan phase: include resolution in this sprint, defer, or migrate.

| Sprint of origin | File:Line | Reason | Owner |
|---|---|---|---|
| {{NNN-slug}} | {{path:N}} | {{why}} | {{agent}} |

<!-- when none match scope: -->
<!-- | — | — | no related open stubs | — | -->

## Documentation migration plan

Items found outside ASD format/location that should become persistent docs in `design/`.
Items addressed by sprint design drafts are NOT listed here (they flow through design → design-promote).
Items NOT covered by sprint scope but worth promoting wait for design-promote handling.

| # | Source (path/URL) | Format | Proposed target in `design/` | Type | Notes |
|---|---|---|---|---|---|
| 1 | {{path}} | {{md/rst/html/wiki/...}} | {{design/.../*.html}} | {{migrated / reverse-engineered}} | {{notes}} |

<!-- when no migrations needed: -->
<!-- | — | — | — | — | — | no migrations | -->

