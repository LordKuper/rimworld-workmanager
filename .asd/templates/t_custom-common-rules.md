---
responsibility:
  owns: project-owner custom rules read by all agents in all phases
  excludes: phase-specific rules (design-only, coding-only)
  delegates_to: custom-design-rules.md (design/design-review), custom-coding-rules.md (impl/impl-review), .asd/rules/ (workflow rules)
---

# Custom Common Rules

Universal project rules read by every ASD agent in every phase. Put here: domain glossary, naming conventions, compliance requirements, project-wide vocabulary, anything that must hold across design AND code.

Phase-scoped rules go elsewhere — design constraints to `custom-design-rules.md`, code/test constraints to `custom-coding-rules.md`. ASD never overwrites this file.
