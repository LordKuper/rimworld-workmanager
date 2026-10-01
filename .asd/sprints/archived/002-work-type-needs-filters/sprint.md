---
responsibility:
  owns: sprint scope, goal, top-level acceptance criteria
  excludes: task breakdown, design decisions, code, audit findings
  delegates_to: plan.md (tasks), design/ docs (decisions), audit.md (audit)
---

# Sprint 002-work-type-needs-filters

## Goal
Add a WorkManager-local needs filter to per-work-type assignment rules (`WorkTypeAssignmentRule`), alongside the existing `AllowedWorkers` pawn filter. The user can add any `NeedDef` to any work type's rule, each with its own 0–100% threshold. When one of a pawn's configured needs is below its threshold, that work type is disabled (priority 0) for the pawn. The filter is re-checked on every WorkManager work-priority update, so assignments follow changes in the pawn's needs. It can be edited in the work-type rules settings UI, is saved and loaded with the other rule settings, and merges with the default rule in the same way as the other rule fields.

Motivating use case: the user configures the Mining work type with Outdoors and Beauty thresholds. A pawn whose Outdoors or Beauty need is unfulfilled is then not assigned Mining, which avoids the mood penalties of being underground or indoors for long periods (cabin fever, "entombed underground").

Before designing, research RimWorld 1.6 needs mechanics in depth: the `Need` and `NeedDef` model, how need levels and thresholds work, the Outdoors and Beauty needs and their thoughts and mood effects (including the underground and cabin-fever effects), and how existing mods handle need-aware work assignment. Write up the findings as design background.

### Scope decisions
1. **Location**: WorkManager-local. The filter is part of `WorkTypeAssignmentRule`. `LordKuper.Common`'s `PawnFilter` is not extended.
2. **"Unfulfilled"**: need level below a threshold the user sets per need, with a 0–100% slider.
3. **Generality**: generic. Any `NeedDef` can be configured on any work type's rule. No needs rules ship by default; the user configures Mining with Outdoors and Beauty themselves.
4. **Effect**: a pawn that fails the needs filter has the work type disabled (priority 0).
5. **Coverage**: the needs block is absolute. `EnsureWorkerAssigned` and `MinWorkerNumber` must not override it.

## Acceptance
- Research findings on RimWorld 1.6 needs mechanics (Need/NeedDef, Outdoors and Beauty levels and thoughts, underground and cabin-fever effects) and related mods are documented as design background.
- `WorkTypeAssignmentRule` has a needs filter: a list of `NeedDef` entries, each with a 0–100% threshold.
- The needs filter is saved and loaded with the other rule settings and merges with the default rule in the same way as the other rule fields.
- The needs filter can be configured (add/remove need, set threshold) in the work-type rules settings UI.
- On every WorkManager work-priority update, a pawn whose level for any configured need is below its threshold gets the work type disabled (priority 0).
- The needs block is absolute: `EnsureWorkerAssigned` and `MinWorkerNumber` never assign a need-blocked pawn to the work type.
- No needs rules ship by default; existing default rules behave exactly as before.
- Production build (`Source/WorkManager.slnx`) compiles clean with no new warnings, and the test suite passes, with tests covering the needs-filter logic.

## Out of scope
- Editing the `LordKuper.Common` upstream library (including adding a needs dimension to `PawnFilter`).
- Shipped default needs rules for Mining or any other work type.
- Lowering priority (instead of disabling) for need-blocked pawns.
- Modifying ASD workflow infrastructure (`.asd/rules/`, `.asd/templates/`, `.claude/`, `CLAUDE.md`).
