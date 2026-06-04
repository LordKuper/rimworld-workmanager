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
