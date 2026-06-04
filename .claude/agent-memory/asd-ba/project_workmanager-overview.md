---
name: workmanager-overview
description: What the Work Manager RimWorld mod is and does — domain context for requirements/PRD/concept work
metadata:
  type: project
---

Work Manager is a RimWorld mod (packageId LordKuper.WorkManager, author LordKuper, CC BY-NC-SA 4.0) that automatically assigns pawn WORK PRIORITIES and WORK SCHEDULES.

Two managers in one codebase:
- WorkPriorityUpdater (MapComponent): recomputes work-tab priorities on a timer, driven by skill / passion / learning rate, plus dedicated-worker, common-work, leftover, and idle-pawn strategies.
- ScheduleUpdater (MapComponent): builds day/night work shifts with skill coverage, even distribution, couples kept together, and Night Owl trait handling.

Player control: per-pawn / per-work-type / per-schedule opt-outs, global PriorityManagementEnabled + ScheduleManagementEnabled toggles, tunable priorities/thresholds/score factors (Settings_WorkPriorities.cs).

Dependencies/compat: Harmony (hard dep); LordKuper.Common (required only on RW 1.6); compatibility shims for WorkTab (fluffy), MoreThanCapable, PriorityMaster, Vanilla Skills Expanded. Supported RW versions 1.1-1.6.

**Why:** This is the product under ASD audit/design; concept + PRD work decomposes this scope.
**How to apply:** Ground all requirements/concept claims in Source/WorkManager/ code and About/About.xml — no invented features (QODDA). Note README.md badges only go to 1.5 while About.xml supports 1.6 (stale README).
