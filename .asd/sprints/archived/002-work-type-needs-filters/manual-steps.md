---
responsibility:
  owns: per-sprint registry of manual operational actions a human must perform for the sprint plan to complete
  excludes: code todo stubs (stubs.md), manual QA verification of behaviour (reviews testing.md), plan tasks (plan.md)
  delegates_to: stubs.md (code stubs), plan.md (tasks + BLOCKED markers), reviews/ (manual verification of behaviour)
---

# Manual Steps

Per-sprint registry of manual operational actions and in-game manual verification steps for sprint `002-work-type-needs-filters`.

This document contains two types of steps:
1. **Build installation (preamble)** — how to install the dev build before running the in-game tests.
2. **In-game manual verification steps (MS-1 through MS-5)** — step-by-step manual checks that cannot be automated and must be run by the user in RimWorld 1.6.

## How to install the development build

> **Note:** Installed by the orchestrator at `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\WorkManager-dev`; settings file `Mod_WorkManager-dev_WorkManagerMod.xml`.

After Task 8 is complete, the built mod assembly is in `1.6/Assemblies/LordKuper.WorkManager.dll`, `LordKuper.WorkManager.pdb`, and `LordKuper.WorkManager.xml`.

**Option A: point RimWorld at the repo folder**

1. Open RimWorld's mod list screen.
2. Navigate to the RimWorld `Mods` folder: `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods` (Windows) or equivalent on your platform.
3. Create a symlink or copy-paste a folder named `LordKuper.WorkManager` pointing to the repo root (i.e., the folder containing `About/About.xml` and `1.6/`).
4. **Disable the Workshop version** of Work Manager in the mod list if installed (same `packageId` will cause conflicts).
5. Restart RimWorld and enable the dev build in the mod list.

**Option B: copy the mod folder into Mods/**

1. Copy the entire repo folder to `C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\LordKuper.WorkManager`.
2. **Disable the Workshop version** of Work Manager in the mod list if installed.
3. Restart RimWorld and enable the mod in the mod list.

**Prerequisites for all manual steps**

- RimWorld 1.6 running with the dev build of Work Manager enabled.
- Harmony and LordKuper.Common mods enabled (dependencies of Work Manager).
- An active game with at least 3 colonists, or dev-mode access to spawn pawns and modify needs.
- Dev mode recommended (F11): enables +/− buttons on each need bar in the pawn's Needs tab to adjust levels by ±10%.

**Settings location and file naming**

RimWorld names ModSettings files as `Mod_<mod folder name>_<Mod class name>.xml`. The dev build's mod folder name and file name depend on your setup:

- **If the mod folder is named `rimworld-workmanager`** (repo root):
  ```
  %USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\Mod_rimworld-workmanager_WorkManagerMod.xml
  ```

- **If the mod folder has a different name**, substitute `rimworld-workmanager` in the path above.

- **Workshop version** (if installed) saves to: `Mod_2029596262_WorkManagerMod.xml` (Workshop ID differs).

**Important: Settings do NOT carry over between Workshop and dev builds.** If you have pre-sprint settings in the Workshop version and want to test them with the dev build (MS-1 step "Existing pre-sprint settings load with no errors"):
1. Locate the Workshop settings file: `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\Mod_2029596262_WorkManagerMod.xml`
2. **Make a backup** of both files.
3. Copy the Workshop file and rename it to match your dev build's folder name (e.g., `Mod_rimworld-workmanager_WorkManagerMod.xml`).
4. Proceed with MS-1 testing. The dev build will load the copied settings.

On Windows, substitute `%USERPROFILE%` with your actual user folder (e.g., `C:\Users\<username>`). On Mac/Linux, use the equivalent config path for RimWorld.

---

## Summary

| ID | Title | Blocks | Performed by | Status |
|---|---|---|---|---|
| MS-1 | Persistence and load-before-defs | Task 7 (setup) | user | done (verified by user 2026-09-29) |
| MS-2 | Live evaluation and recovery | Task 7 (setup) | user | done (verified by user 2026-09-29) |
| MS-3 | Fail-safe absolute block | Task 7 (setup) | user | done (verified by user 2026-09-29) |
| MS-4 | Settings UI and summary | Task 7 (setup) | user | done (verified by user 2026-09-29) |
| MS-5 | Russian and ChineseSimplified display | Task 7 (setup) | user | done (verified by user 2026-09-29) |

---

## MS-1 — Persistence and load-before-defs

- **Blocks**: Task 7 — Persistence, load before defs, unresolved entry verification
- **Why**: AC-9 (load path reads only defName/threshold), AC-10 (ValidateNeedsFilter on LoadingVars), AC-11 (defName persisted not `.Def`), AC-12 (null list and unresolved entries handled safely)
- **When**: After Task 5 is built; before playing a full session
- **Performed by**: user (manual in-game verification)
- **Status**: done (verified by user 2026-09-29)

### Scenario: Mining + Outdoors/Beauty

This scenario tests persistence across a save/load cycle and that the filter resolves needs only after defs are loaded (not on the load path itself).

### Steps

1. **Create a new game** with at least 2 colonists. Name one "Miner" for easy identification.
2. **Open Work Manager settings** (icon in the mod list or via mod settings).
3. **Navigate to Work Types tab** and select "Mining" rule.
4. **In the Needs section**, toggle "Filter by needs" **On** (for the Mining rule; this is Inherit → On).
5. **Click Add** to add a need. A menu appears with needs sorted by list priority and label.
6. **Select "Outdoors"** from the menu. A new entry appears with a slider at 50% (default).
7. **Change the Outdoors threshold to 60%** (drag the slider or type 0.6 if clickable).
8. **Click Add** again and select **"Beauty"** from the menu.
9. **Change the Beauty threshold to 35%** (drag to 0.35).
10. **Close the settings window** (changes are persisted to the dev build's settings file, e.g., `Mod_rimworld-workmanager_WorkManagerMod.xml`, depending on your mod folder name).
11. **Wait for one work priority update** (configurable frequency, default every 1 in-game hour). Closing the settings window should trigger an update automatically.
12. **Check in the work assignments:** open the Work tab (vanilla) and verify Miner is not assigned Mining (priority 0) if their Outdoors < 60% or Beauty < 35%.
13. **Save the game** (Ctrl+S or menu) and **quit to desktop**.
14. **Restart RimWorld** (or load the save in a new session).
15. **Open the Work Manager settings again** and go to the Mining rule.
16. **Verify the Needs section shows:**
    - "Filter by needs" is **On**
    - **Outdoors: 60%** (unchanged)
    - **Beauty: 35%** (unchanged)
17. **Close settings** and verify the assignments **still apply** (Miner still blocked if needs are below thresholds) **without needing a second restart** — this confirms defs were resolved correctly on load, not on the load path.

### Test an unresolved defName entry (hand-edit the settings file)

18. **Close RimWorld** (to avoid overwriting the file).
19. **Navigate to the dev build's settings file** in the config folder:
    ```
    %USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\Mod_<folder name>_WorkManagerMod.xml
    ```
    (Replace `<folder name>` with your actual mod folder name, e.g., `rimworld-workmanager`.)
20. **Open the settings file in a text editor** (Notepad, VS Code, etc.).
21. **Find the Mining rule's NeedLimits section.** It should look like:
    ```xml
    <NeedLimits>
      <li>
        <DefName>Outdoors</DefName>
        <Threshold>0.6</Threshold>
      </li>
      <li>
        <DefName>Beauty</DefName>
        <Threshold>0.35</Threshold>
      </li>
    </NeedLimits>
    ```
22. **Add a nonexistent need manually.** Insert a new `<li>` with a fake defName:
    ```xml
    <NeedLimits>
      <li>
        <DefName>Outdoors</DefName>
        <Threshold>0.6</Threshold>
      </li>
      <li>
        <DefName>Beauty</DefName>
        <Threshold>0.35</Threshold>
      </li>
      <li>
        <DefName>FakeNeedThatDoesNotExist</DefName>
        <Threshold>0.5</Threshold>
      </li>
    </NeedLimits>
    ```
23. **Save the file** and close the editor.
24. **Restart RimWorld** and load the same save.
25. **Open Work Manager settings** and go to the Mining rule.
26. **Verify the Needs section shows:**
    - **Outdoors: 60%** (resolved, label shown)
    - **Beauty: 35%** (resolved, label shown)
    - **FakeNeedThatDoesNotExist (unavailable)** with a tooltip explaining it's not available (the mod could not resolve the defName after defs loaded).
27. **Click the delete button** on the unresolved entry to remove it.
28. **Close settings and save the game** again.
29. **Exit and re-open RimWorld** (load the same save again).
30. **Verify the unresolved entry is gone** and only Outdoors and Beauty remain.

### Verification

- [ ] **Persistence**: Settings survive a save/load cycle; defNames and thresholds unchanged.
- [ ] **Load-before-defs**: Filter blocks correctly in the loaded game without a second restart (defs resolved after load, on the first update).
- [ ] **Unresolved entry**: Hand-edited fake need shows as "(unavailable)" with tooltip; is removable; and does not cause log errors.
- [ ] **No errors**: The RimWorld log shows no `NeedDef` errors or missing-def warnings related to the Needs filter during load or gameplay.

---

## MS-2 — Live evaluation and recovery

- **Blocks**: Task 7 — Live evaluation and recovery
- **Why**: AC-13 (seam is evaluated every update), AC-16 (missing need passes), AC-17 (below threshold blocks), AC-22 (priority 0 is default), AC-23 (no errors when pawn lacks configured need)
- **When**: After Task 5 is built; during an active game session
- **Performed by**: user (manual gameplay observation)
- **Status**: done (verified by user 2026-09-29)

### Scenario: Outdoors threshold blocks Mining on demand

This scenario tests that the needs filter evaluates live (re-checked every update cycle) and recovers when the need recovers, and that pawns without a listed need are unaffected.

### Steps

1. **Continue from MS-1 or start a fresh game** with the Mining + Outdoors (60%) filter active.
2. **Select a colonist miner** (ideally "Miner" from MS-1). Open their Needs tab to view their Outdoors need level.
3. **Ensure the miner has Outdoors need** (most humanlike colonists do; you should see the Outdoors need bar in their Needs tab). If the miner is an Undergrounder, skip to step 10 (pawns without a listed need should be unaffected).
4. **Force the Outdoors need below 60%:**
   - **Option A (dev mode, recommended):** Press F11 to enable dev mode. Select the miner and open their Needs tab. When dev mode is on, small +/− buttons appear on each need bar. Click the − button on the Outdoors bar multiple times until the bar shows less than 60% (roughly one-third full).
   - **Option B (gameplay):** Build an isolated underground room with a roof. Move the miner underground and wait ~1–2 in-game hours until their Outdoors drops below 60% (watch the Needs tab as they move).
5. **Trigger a work priority update:** Close the Work Manager settings window (if open), or wait for the next scheduled update (default every 1 in-game hour). Closing the settings window should force an immediate update.
6. **Check the miner's work assignments:** Open the Work tab (vanilla) and verify **Mining is priority 0** (or not shown if priority 0 is hidden) for the miner.
7. **Move the miner back outdoors** (or use dev mode to restore Outdoors to ≥ 60%):
   - **Dev mode:** Click the + button on the Outdoors need bar several times until it shows above 60%.
   - **Gameplay:** Move them outside or to a room with open roof; wait ~10–20 minutes of in-game time for Outdoors to recover naturally.
8. **Trigger another work priority update** (close settings, or wait 1 hour).
9. **Verify Mining assignment is restored:** Open the Work tab and confirm **Mining is no longer blocked** (priority is ≥ 1).

### Scenario: Pawn without a listed need is unaffected

10. **Spawn or identify a pawn that lacks the Outdoors need:**
    - **Undergrounder trait:** Find a colonist with this trait. You can spawn one using dev-mode debug actions (look for "Spawn pawn" in the debug menu).
    - **Verify the pawn lacks Outdoors:** Open their Needs tab and confirm **no Outdoors need bar is shown** (Undergrounders disable Outdoors).
11. **Assign the pawn to Mining** (manually in the Work tab, or let the mod auto-assign if they're capable).
12. **With the Mining + Outdoors (60%) filter active, the pawn should still be assignable** (no block) because they lack the Outdoors need.
13. **Verify in the Work tab:** The pawn can have Mining priority ≥ 1, regardless of their "Outdoors" level (which is nil).

### Verification

- [ ] **Live evaluation**: Outdoors below 60% → Mining priority becomes 0 (blocked). Outdoors ≥ 60% → Mining priority restored (≥ 1).
- [ ] **Recovery is fast**: Changes apply within one update cycle (~1 in-game hour or immediately after closing settings).
- [ ] **Missing need unaffected**: Pawns lacking the Outdoors need are not blocked by the Outdoors threshold.
- [ ] **No log errors**: RimWorld log shows no errors or exceptions when the filter re-evaluates (especially for missing needs).

---

## MS-3 — Fail-safe absolute block

- **Blocks**: Task 7 — Fail-safe absolute block
- **Why**: AC-24 (fail-safe excludes need-blocked only), AC-25 (EnsureWorkerAssigned + MinWorkerNumber never assign need-blocked), AC-26 (compatible with pre-sprint; identical candidates/priorities when needs filter is Off/empty)
- **When**: After Task 5 is built; during an active game session with dedicated workers enabled
- **Performed by**: user (manual gameplay verification)
- **Status**: done (verified by user 2026-09-29)

### Scenario: Mining dedicated worker with EnsureWorkerAssigned, all candidates need-blocked

This scenario tests that the fail-safe (in `AssignDedicatedWorkersForDay`) respects the needs filter absolutely: even if "Ensure worker assigned" is On or "Minimum workers" is ≥ 1, no need-blocked pawn is assigned the dedicated priority.

### Setup

1. **Start a new game or continue one** with Work Manager settings open.
2. **Ensure "Assign dedicated workers" is enabled** in the Work Priorities tab.
3. **Go to Mining rule** and:
   - Toggle "Filter by needs" to **On**.
   - Add **Outdoors threshold 60%**.
   - Toggle "Allow dedicated workers" to **On** (if not already; use Inherit on work-type rules to follow default).
   - Set Mode to "Constant" with **Target number of workers = 1** (or higher, e.g., 2).
4. **In Assignment settings:**
   - Toggle "Ensure workers assigned" to **On**.
   - Set "Minimum number of workers" to **1** (or 2, same as dedicated target).
5. **Spawn or add 3 colonists**, all capable of Mining. Name them "Miner1", "Miner2", "Miner3".
6. **Block all 3 from Mining via needs:** Move them all underground (or use dev mode +/− buttons in their Needs tab to set Outdoors to 0.5 / 50%, below the 60% threshold). Verify they all have Outdoors need.
7. **Trigger a work priority update:** Close the settings window or wait ~1 in-game hour for the scheduled update.

### Test

8. **Open the Work tab (vanilla)** and check Mining assignments for all 3 miners.
9. **Verify: NONE of them are assigned Mining dedicated priority** (check against the configured `DedicatedWorkerPriority`, typically 1 or similar). They should all have priority 0 or unassigned for Mining.
10. **Explanation (from code):** The fail-safe loops through capable pawns, finds "good workers" (allowed and not bad/dangerous work). When good-worker candidates are exhausted, it falls back to the "available workers" pool: capable pawns not in goodWorkers AND not blocked by needs. Since all are need-blocked, the available pool is empty, so no one is assigned.
11. **Restore one miner's Outdoors (e.g., to 0.8 / 80%)** using dev mode +/− buttons. Close settings or wait for the next update. Verify that one miner is now assigned Mining dedicated priority (≤ 1). The other two remain blocked at priority 0.

### Scenario: Pre-sprint behavior (needs filter Off or empty)

12. **Return to Mining rule and toggle "Filter by needs" to **Off** (or remove all need entries).**
13. **Keep "Ensure workers assigned" On and "Minimum workers" = 1.**
14. **Move all 3 miners back underground** (Outdoors at 0.5) or use dev mode +/− buttons.
15. **Close settings or wait ~1 hour** to trigger an update.
16. **Verify:** Unlike the test above, **at least one miner is now assigned Mining dedicated priority** (the fail-safe can assign them because the needs filter is off). This confirms that **before the needs filter was active**, the fail-safe would have assigned someone.

### Verification

- [ ] **Needs block overrides EnsureWorkerAssigned**: Even with EnsureWorkerAssigned=On and MinWorkerNumber=1, no need-blocked pawn receives the dedicated priority.
- [ ] **Needs block overrides MinWorkerNumber**: Same as above; the minimum is never met if it requires assigning a need-blocked pawn.
- [ ] **Pre-sprint compatibility**: With needs filter Off, fail-safe behavior is identical to the pre-sprint version (same candidates are assigned).
- [ ] **No log errors**: Log shows no errors during fail-safe evaluation.

---

## MS-4 — Settings UI and summary

- **Blocks**: Task 7 — Settings UI and summary
- **Why**: AC-27..41 (Needs section placement, state control, add button, entry rows, empty state, summary, tooltip changes)
- **When**: After Task 5 is built; during an active game session
- **Performed by**: user (manual UI inspection and interaction)
- **Status**: done (verified by user 2026-09-29)

### UI presence and placement

1. **Open Work Manager settings** (Work Types tab).
2. **Select the default rule** (top of list, "* Default *").
3. **Verify Needs section is present** below "Allowed workers" and shows:
   - A section header "Needs" (key `LordKuper.WorkManager.Settings.WorkTypes.NeedsLabel`).
   - A checkbox "Filter by needs" with Inherit/Off/On options (for work-type rules; Off/On for default).
   - A tooltip on "Filter by needs" explaining the three states.
4. **Select a work-type rule** (e.g., Mining).
5. **Verify Needs section appears in the same position** (below Allowed workers, before or after Dedicated workers depending on layout).

### State control and tooltips

6. **On the default rule:**
   - The "Filter by needs" checkbox shows **Off / On** (two states, not Inherit).
   - Click the checkbox: toggle it and verify the label changes between "Off" and "On".
7. **On a work-type rule (e.g., Mining):**
   - The "Filter by needs" checkbox shows **Inherit / Off / On** (three states, a tri-state control).
   - Hover over the checkbox label or button: a tooltip appears with `GetFilterNeedsTooltip(true)` text (e.g., "The setting's value from the default rule will be used" for Inherit, "Pawns with a listed need below its threshold are not assigned to the work type" for On, "Needs are ignored" for Off).

### Add button and need menu

8. **Set "Filter by needs" to On** for any rule.
9. **Below the state control, if no needs are added yet, a label appears: "No needs added. Nothing is filtered."**
10. **Verify an "Add" button is shown** (label "Add", from Common's Actions.Add string).
11. **Click the "Add" button.** A float menu appears listing all addable needs.
12. **Verify the menu includes:**
    - **Mood** (even though `showOnNeedList = false`; this is intentional per spec).
    - **RoomSize** (even though `showOnNeedList = false`).
    - **Outdoors, Beauty, Comfort, Food, Rest, Joy, DrugDesire**.
    - **Authority is NOT listed** (hard-excluded per `IsNeedBlocked` seam logic).
    - **Needs already added to this rule are NOT listed** (e.g., if Outdoors is already present, it's not in the menu again).
    - Sorted by `listPriority` descending (Mood 1000 first, then Food 800, Rest 700, Joy 500, Beauty 300, Comfort 200, etc.), then by label (CurrentCulture sort).
13. **Hover over each need in the menu:** a tooltip appears showing `def.description` (e.g., "Comfort: Comfort provides a positive boost to a pawn's mood." or similar).

### Entry rows (added needs)

14. **Select one need from the menu** (e.g., Outdoors). A new entry row appears.
15. **Verify the entry row shows:**
    - The need label on the left (if resolved, e.g., "Outdoors"; if unresolved from hand-edit, e.g., "FakeNeed (unavailable)").
    - The need description as a tooltip when hovering over the label (e.g., "Forces pawns to spend time outside" or the unavailable tooltip).
    - A **percentage slider** in the middle, ranging 0–100% (or 0–1 internally), with 1% steps (or fine-grained).
    - The current value displayed (e.g., "50%" by default).
    - A **delete button** on the right (X icon or "Delete" label).
16. **Drag the slider or click to set the value to 60%** (0.6). Verify the display updates immediately.
17. **Add a second need** (e.g., Beauty at 35%). Verify a second entry row appears below the first.
18. **Add a third need** (e.g., Food at 0.5). Verify layout adjusts to show all three.
19. **Click the delete button on the Beauty entry.** The entry is removed (deferred deletion, so it applies on the next layout event).
20. **Close and reopen the settings** (or trigger a layout refresh). Verify Beauty is gone and Outdoors/Food remain.

### Empty state

21. **Remove all need entries** (delete each one until none remain and "No needs added" text reappears).
22. **Set "Filter by needs" to Off.** The Needs section collapses; entries and add button are hidden.
23. **Set "Filter by needs" back to On.** The section expands; "No needs added" label is shown; no entries; Add button is visible.

### Rule summary (in the header tooltip)

24. **Still on the same work-type rule, look at the top of the rule details area.**
25. **You should see a header like "Work type assignment rule for 'Mining'"** with a tooltip.
26. **Hover over the header** or click a "Rule summary" section if present.
27. **In the tooltip (or summary panel), you should see:**
    - **Assignment settings section** (with EnsureWorkerAssigned, MinWorkerNumber, AssignEveryone details).
    - **Dedicated worker settings section** (if UseDedicatedWorkers is enabled).
    - **Allowed workers section** (summary of the filter).
    - **Needs section** with:
      - If FilterNeeds is Inherit: "The settings from the default rule will be used."
      - If FilterNeeds is Off: "No" (or "Needs are ignored").
      - If FilterNeeds is On with entries: One line per need, e.g., "Outdoors: 60%", "Beauty: 35%", "Food: 50%". Unresolved (fake) needs show just the defName, e.g., "FakeeNeed: 50%".
      - If FilterNeeds is On with no entries: "No needs added. Nothing is filtered."

### Reworded tooltips

28. **Go back to the Assignment settings section.**
29. **Hover over "Ensure workers assigned"** (when On): The tooltip should include text like "Pawns blocked by the needs filter are never assigned" (added per AC-40).
30. **Hover over "Minimum number of workers"** (when EnsureWorkerAssigned is On): The tooltip should include "Pawns blocked by the needs filter are never assigned" (per AC-41).
31. **Go to the bottom of the rule, to the "Available pawns" section.**
32. **Hover over "Available pawns"** (the label or a tooltip icon): The tooltip should say "List of pawns that satisfy the allowed workers filter. The needs filter is not applied to this preview." (per AC-38).

### Apply without restart

33. **Make a change to the needs filter** (add a need, remove a need, toggle On/Off, adjust a threshold).
34. **Close the settings window** (click X or OK).
35. **Open the Work tab (vanilla) and observe the work assignments.** They should reflect the changes immediately (the mod's `WriteSettings` callback triggers an update).
36. **No restart required** (this was already true for other settings, and the needs filter follows the same path).

### Verification

- [ ] **Needs section present** below Allowed workers.
- [ ] **State control correct**: two-state (Off/On) on default, tri-state (Inherit/Off/On) on work-type rules.
- [ ] **Tooltips accurate** and describe the filter behavior (blocking when below threshold, overriding fail-safe, etc.).
- [ ] **Add button and menu work**: menu lists all needs except Authority, sorted by priority+label, includes Mood and RoomSize, excludes already-added needs, each with description tooltip.
- [ ] **Entry rows display correctly**: label, slider (0–100% in 1% steps), delete button.
- [ ] **Empty state shows** when no needs are added and filter is On.
- [ ] **Rule summary includes** Needs part with the three states (Inherit/Off/On/empty).
- [ ] **"Available pawns" and fail-safe tooltips reworded** to mention needs block.
- [ ] **Changes apply without restart** (WriteSettings → UpdateSettingsCache → ForceUpdateAssignments).

---

## MS-5 — Russian and ChineseSimplified display

- **Blocks**: Task 7 — Locale display
- **Why**: AC-43 (all 1.6 locales have same 55 Settings.WorkTypes keys), AC-49 (Russian and ChineseSimplified strings are real translations, not English)
- **When**: After Task 5 is built and all 8 new + 3 reworded keys are in all 1.6 locale files
- **Performed by**: user (manual language switching and inspection)
- **Status**: done (verified by user 2026-09-29)

### Russian locale test

1. **Open RimWorld settings or pause menu** and switch language to **Russian** (Русский).
2. **Restart RimWorld** (or reload the game; some builds apply language changes on restart).
3. **Open Work Manager settings** (icon or menu).
4. **In the Work Types tab**, select any rule and navigate to the Needs section.
5. **Verify all Needs-section strings are in Russian**, not English:
   - Section header: should be a Russian translation of "Needs" (not "Needs" in English).
   - "Filter by needs" label and checkbox: should be in Russian.
   - Tooltips (hover over labels): should be in Russian (e.g., the Needs tooltip starting with "The work type is disabled for a pawn while any need listed here is below its threshold...").
   - "Add" button: should be in Russian (from Common library).
   - "Delete" button: should be in Russian.
   - Need list entries: slider labels, delete buttons all in Russian.
   - "No needs added" text: should be in Russian.
   - Unresolved need text (if manually added): "(unavailable)" portion should be in Russian.
6. **Check the rule summary (header tooltip):**
   - "Needs" section header: in Russian.
   - State descriptions ("On", "Off", "Inherit"): in Russian.
   - Threshold displays (e.g., "60%"): numeric, same in all languages.
   - Descriptions should be in Russian if they come from locale keys; game-supplied `NeedDef.description` is in Russian if the game is in Russian.
7. **Check reworded Assignment tooltips:**
   - "Ensure workers assigned" On tooltip: should end with Russian text about needs blocking (added in AC-40).
   - "Minimum workers" tooltip: should include Russian text about needs blocking (added in AC-41).
   - "Available pawns" tooltip: should say in Russian that needs filter is not applied (added in AC-38).
8. **Verify no raw keys are shown** (e.g., no `LordKuper.WorkManager.Settings.WorkTypes.NeedsLabel` string appearing in place of the translated text).

### ChineseSimplified locale test

9. **Switch language to ChineseSimplified** (简体中文 or similar; path depends on RimWorld's language menu).
10. **Restart RimWorld** if needed.
11. **Open Work Manager settings** and repeat steps 4–8 for Chinese: all Needs-section strings, tooltips, buttons, and reworded fail-safe tooltips should be in ChineseSimplified (simplified Chinese characters), not English or raw keys.

### Key count verification

12. **Still in Chinese (or Russian, does not matter for key count):**
13. **Open each 1.6 language file:**
    - `1.6/Languages/English/Keyed/WorkManager_Keyed.xml`
    - `1.6/Languages/Russian/Keyed/WorkManager_Keyed.xml`
    - `1.6/Languages/ChineseSimplified/Keyed/WorkManager_Keyed.xml`
14. **Count the number of `<LordKuper.WorkManager.Settings.WorkTypes.*>` keys** in each file (or search for the prefix).
15. **Verify all three files have the same number of keys** (should be 55 total per the spec; at minimum, the 8 new Needs keys + 3 reworded should be present in all three).
16. **Spot-check a few new keys:**
    - `NeedsLabel` (section header)
    - `NeedsTooltip` (detailed tooltip)
    - `FilterNeedsLabel` (checkbox label)
    - `NeedsEmptyLabel` (empty state)
    - `NeedUnavailableLabel` (unresolved need, with `{0}` placeholder for defName)
    - `NeedUnavailableTooltip` (unresolved need explanation)
17. **Spot-check the 3 reworded keys:**
    - `EnsureWorkerAssignedOnTooltip` — should mention needs block.
    - `MinWorkerNumberTooltip` — should mention needs block.
    - `AvailablePawnsTooltip` — should mention needs filter not applied.
18. **Verify values are real translations** (not copy-paste of English or obviously auto-translated gibberish). For Russian and Chinese, a native speaker or community reviewer should validate linguistic quality (this is best-effort per the spec; the gate is "no raw keys shown" and "all keys present", not linguistic correctness).

### Verification

- [ ] **Russian UI**: All Needs-section strings and reworded tooltips displayed in Russian; no English keys; no raw key IDs.
- [ ] **ChineseSimplified UI**: All Needs-section strings and reworded tooltips displayed in ChineseSimplified; no English keys; no raw key IDs.
- [ ] **Key count**: All three 1.6 locale files have the same number of Settings.WorkTypes keys (55).
- [ ] **All new and reworded keys present**: 8 new (NeedsLabel, NeedsTooltip, FilterNeedsLabel, FilterNeedsOnTooltip, FilterNeedsOffTooltip, NeedsEmptyLabel, NeedUnavailableLabel, NeedUnavailableTooltip) + 3 reworded (EnsureWorkerAssignedOnTooltip, MinWorkerNumberTooltip, AvailablePawnsTooltip) in English, Russian, and ChineseSimplified.
- [ ] **No 1.1–1.5 changes**: Run `git diff master -- 1.1 1.2 1.3 1.4 1.5` and confirm no language file changes in those folders.

---

## User completion checklist

Once all five manual steps have been run and verified, mark them complete:

- [ ] MS-1 verified: persistence and load-before-defs working; unresolved entries handled.
- [ ] MS-2 verified: live evaluation and recovery working; missing needs unaffected.
- [ ] MS-3 verified: fail-safe blocks need-blocked pawns absolutely; pre-sprint behavior preserved.
- [ ] MS-4 verified: settings UI complete, summary correct, tooltips reworded, no restart needed.
- [ ] MS-5 verified: Russian and ChineseSimplified translated; no raw keys; 55 keys in all locales.
