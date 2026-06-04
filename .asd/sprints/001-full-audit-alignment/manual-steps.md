---
responsibility:
  owns: per-sprint registry of manual operational actions a human must perform for the sprint plan to complete
  excludes: code todo stubs (stubs.md), manual QA verification of behaviour (reviews testing.md), plan tasks (plan.md)
  delegates_to: stubs.md (code stubs), plan.md (tasks + BLOCKED markers), reviews/ (manual verification of behaviour)
---

# Manual Steps

## Summary

| ID | Title | Blocks | Performed by | Status |
|---|---|---|---|---|
| MS-1 | Verify no NRE on game-less UI screens | Task 3 — AC-12 | user | pending |
| MS-2 | Verify passion score normalization across passion levels | Task 2 — AC-5 | user | pending |
| MS-3 | Verify valid hour-to-assignment mapping in WorkShift | Task 2 — AC-2 | user | pending |

## MS-1 — Verify no NRE on game-less UI screens

- **Blocks**: Task 3 — AC-12 in-game verification
- **Why**: AC-12 requires confirming that no `NullReferenceException` is thrown when UI patches and pawn column workers are invoked with no active game. An automated test requires a live RimWorld instance and is infeasible in the unit-test harness.
- **When**: After building and deploying the mod to a RimWorld 1.6 installation
- **Prerequisites**: Production build (`dotnet build Source/WorkManager.slnx -c Release`) completed and `1.6/Assemblies/LordKuper.WorkManager.dll` deployed to the mod folder
- **Performed by**: user (mod developer or tester with a RimWorld 1.6 installation)
- **Status**: pending

### Steps

1. Launch RimWorld with the WorkManager mod enabled.
2. At the main menu (no save loaded), open **Options → Mod Settings → WorkManager**. Navigate through all settings tabs. Confirm no error dialog and no exception in the RimWorld log.
3. At the main menu, open the **Work** tab if accessible from the main menu (some mods expose it). If not accessible without a game, skip to step 4.
4. Start or load a save. Once in-game, open and close the work tab, then save and return to the main menu without quitting. Immediately re-open any WorkManager-related UI from the main menu state. Confirm no exception in the RimWorld log.
5. Inspect `Player.log` (Linux/macOS) or the RimWorld log window for any `NullReferenceException` originating from `LordKuper.WorkManager`.

### Verification

No `NullReferenceException` with a stack trace containing `LordKuper.WorkManager` in the RimWorld log across all steps above. The mod settings screen opens and closes cleanly.

## MS-2 — Verify passion score normalization across passion levels

- **Blocks**: Task 2 — AC-5 (`PassionHelper.GetPassionScore` behavior coverage)
- **Why**: `PassionHelper.GetPassionScore` computes normalized passion scores from RimWorld's passion definitions (learn/forget rate factors). The normalization logic depends on the passion def database being fully loaded, which occurs only at runtime in a live RimWorld game. Unit testing without a loaded game is infeasible.
- **When**: After building and deploying the mod to a RimWorld 1.6 installation
- **Prerequisites**: Production build (`dotnet build Source/WorkManager.slnx -c Release`) completed and `1.6/Assemblies/LordKuper.WorkManager.dll` deployed
- **Performed by**: user (mod developer or tester with a RimWorld 1.6 installation)
- **Status**: pending

### Steps

1. Launch RimWorld with the WorkManager mod enabled.
2. Create a test save (or load an existing one) with at least one colonist with varied passion levels (None, Minor, Major) in different skills.
3. Open **Options → Mod Settings → WorkManager** and navigate to the **Pawns & Rules** tab (or whichever tab displays passion-based work assignments if reconfigured).
4. Verify that pawns with different passion levels are assigned correctly to work types, reflecting the normalized score differences. For example, pawns with Major passion should be prioritized higher than Minor/None for the same work type if passion affects assignment.
5. Inspect logs or debug output to verify that `PassionHelper.GetPassionScore` was called and returned normalized values in the 0–1 range for each passion level (None < Minor < Major expected relative ordering, based on passion def learn/forget rates).

### Verification

Passion-based work assignments execute without error. The mod correctly computes and applies normalized passion scores (expected range [0, 1]) when prioritizing pawns for work, with relative ordering reflecting passion levels (None, Minor, Major). No `NullReferenceException` or `KeyNotFoundException` from passion score lookups in the RimWorld log.

## MS-3 — Verify valid hour-to-assignment mapping in WorkShift

- **Blocks**: Task 2 — AC-2 (`WorkShift.GetTimeAssignment` valid hour mapping coverage)
- **Why**: `WorkShift.GetTimeAssignment(hour)` resolves hour values (0–23) to `TimeAssignmentDef` instances via `DefDatabase<TimeAssignmentDef>`. The def database is populated only at runtime when RimWorld is fully initialized. Unit testing without a loaded game is infeasible.
- **When**: After building and deploying the mod to a RimWorld 1.6 installation
- **Prerequisites**: Production build (`dotnet build Source/WorkManager.slnx -c Release`) completed and `1.6/Assemblies/LordKuper.WorkManager.dll` deployed
- **Performed by**: user (mod developer or tester with a RimWorld 1.6 installation)
- **Status**: pending

### Steps

1. Launch RimWorld with the WorkManager mod enabled.
2. Create or load a save with at least one work shift configured.
3. Open **Options → Mod Settings → WorkManager → Schedules** to view work shift configuration.
4. Verify that the mod correctly displays shift assignments for each hour (0–23). For example, navigate through hour sliders or schedule editors and confirm each hour is mapped to a valid `TimeAssignmentDef` (e.g., "Work", "Sleep", "Joy", "Anything").
5. Inspect the RimWorld log for any `ArgumentOutOfRangeException` or missing `TimeAssignmentDef` errors from `LordKuper.WorkManager.WorkShift.GetTimeAssignment`.

### Verification

Work shifts display and apply correctly across all 24 hours without error. The `GetTimeAssignment` method successfully resolves each hour value (0–23) to a corresponding `TimeAssignmentDef`. No `ArgumentOutOfRangeException` or def-not-found errors in the RimWorld log originating from `WorkShift.GetTimeAssignment`.
