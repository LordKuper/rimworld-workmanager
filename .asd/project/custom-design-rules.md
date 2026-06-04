---
responsibility:
  owns: project-owner custom rules read during design and design-review phases
  excludes: universal rules, code/test rules
  delegates_to: custom-common-rules.md (all phases), custom-coding-rules.md (impl/impl-review)
---

# Custom Design Rules

Inherited from the `LordKuper.Common` parent library and adapted to WorkManager.

## Modding & patchability

- Harmony-patchable: prefer small methods, stable public entry points, predictable side effects.
- Don't seal mod extension points without strong reason.
- No static constructors with heavy side effects.
- WorkManager patches RimWorld's work UI/scheduling — patch targets are an integration surface. Prefer narrow, stable patch points; document why each patch exists.

## Data-driven over hardcoded

- Stat / balance / tuning values come from RimWorld `Def`s or mod settings, never hardcoded literals in code. ADRs/PRDs introducing new tunables MUST specify the Def/settings surface (see `Source/WorkManager/Settings/`), not literal constants.

## Determinism

- Work-priority assignment, schedule generation, filtering, and caching logic: same inputs → same outputs.
- No time- or order-dependent behavior in core logic unless explicitly required.

## Compatibility

- WorkManager integrates with other mods (WorkTab, MoreThanCapable, PriorityMaster — see `Source/WorkManager/Compatibility/`). Design changes touching work-priority columns or scheduling MUST consider these compatibility shims and call out impact in the ADR.
