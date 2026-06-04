---
responsibility:
  owns: project-vetted reference for one technology version (apis used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), adr/ (decisions), commands.yaml (commands)
---

# {{TECH_NAME}} @ {{VERSION}}

## Canonical source
- Official docs: {{URL}}
- Last verified: {{ISO8601_DATE}}

## API surface used in project
- {{api or feature}}: {{purpose in project}}; example: {{snippet}}

## Version-specific notes
- {{behaviour or limitation specific to this version}}

## Deprecations and breaking changes from prior version
- {{what changed}}: {{migration path}}

## Project conventions
- {{how project uses this tech: patterns, wrappers, error handling, naming}}

## Known issues and workarounds
- {{issue}}: {{workaround}}
