---
responsibility:
  owns: project-vetted reference for the RimWorld 1.6 game API (Assembly-CSharp + Unity modules) — consumed surface and conventions
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), adr/ (decisions), commands.yaml (commands)
---

# RimWorld game API @ 1.6 (Assembly-CSharp + Unity modules)

## Canonical source
- Official wiki (modding): https://rimworldwiki.com/wiki/Modding
- Decompiled `Assembly-CSharp` (game install): `$(RimWorldManagedDir)\Assembly-CSharp.dll` — the authoritative surface; there is no published API doc.
- Harmony (patches the game API): https://harmony.pardeike.net/ (see `Lib.Harmony-2.4.2.md`).
- Last verified: 2026-09-29 (Needs section: decompiled local RimWorld 1.6.4871 rev590 `Assembly-CSharp.dll` + Core `Data/Core/Defs/NeedDefs/Needs.xml` and `ThoughtDefs/Thoughts_Situation_Needs.xml`; only Core is installed, so DLC facts are marked [unverified]). Earlier sections: 2026-06-04.
- Verification note: the canonical wiki could not be fetched at verification time (HTTP 403). The surface below is grounded in **observed usage across `Source/WorkManager/`** (authoritative for what the mod binds to) plus established RimWorld modding knowledge. Signatures marked *(unverified)* are from modding-community convention, not a fetched primary source — re-verify against the decompiled `Assembly-CSharp.dll` when convenient.

## Versioning model (read first)
- RimWorld is **not a NuGet package**. `Assembly-CSharp` and the Unity modules are file references from the local install via `$(RimWorldManagedDir)` / `RIMWORLD_DIR` (the `Managed` folder), with `<Private>False</Private>` — compiled against, never shipped (per `stack.html` "compile against, never ship, the host").
- The version tracks the RimWorld major line (**1.6**). The mod's `About/About.xml` declares `supportedVersions` 1.1–1.6; the *compile-time* surface is whatever the 1.6 install exposes. A new major line (e.g. 1.7) means a new reference doc and re-validation.
- Runtime API is whatever the loaded game version provides; multi-version support is achieved via `About.xml` metadata + conditional dependencies, not per-version assemblies.

## Unity modules — consumed API surface
The authoritative list of *which* Unity modules are referenced and their version set is `stack.html` (SSoT). This section only details *how* the consumed types are used — it does not re-enumerate the reference/version set:
- **CoreModule** — `Rect`, `GUI`, `Color`, `Event`, `EventType`, `Mouse`, `Texture2D`. Used by every UI patch and pawn column.
- **IMGUIModule** — immediate-mode GUI backing RimWorld's `Widgets`/`Listing_Standard` used for mod settings.
- **TextRenderingModule** — text rendering for UI labels.

## API surface used in project
Grounded in actual usage across `Source/WorkManager/`. Namespaces consumed: `Verse`, `RimWorld`, `UnityEngine`.

### Mod entry & settings (`Verse`)
- `Mod` (base class): `WorkManagerMod : Mod`, ctor `WorkManagerMod(ModContentPack content) : base(content)`. RimWorld instantiates one `Mod` per active mod at startup. Override `DoSettingsWindowContents(Rect inRect)` and `SettingsCategory()` for the mod-config screen.
- `ModContentPack`: passed to the `Mod` ctor; identifies the mod's content.
- `ModSettings` (base class): `Settings : ModSettings`. Persisted via the parent `Mod.GetSettings<T>()` (`Settings = GetSettings<Settings>();`) and serialized through `ExposeData()`.
- `GetSettings<T>()` (on `Mod`): loads/creates the settings instance.
- `Dialog_ModSettings`: `Find.WindowStack.Add(new Dialog_ModSettings(Settings.Mod))` opens the mod-settings dialog programmatically.

### Game lifecycle components (`Verse`)
- `GameComponent` (base class): `WorkManagerGameComponent : GameComponent`, ctor takes `Game game`. RimWorld constructs game components when a `Game` is created (new game or load). Overrides used:
  - `ExposeData()` — save/load hook (called by `Scribe`).
  - `LoadedGame()` — invoked after a save is loaded.
  - `StartedNewGame()` — invoked after a new game starts.
  - *(unverified)* `GameComponentTick()` / `GameComponentUpdate()` exist but are not overridden here.
  - **Lifecycle invariant relied on by the code:** the ctor runs only when a `Game` exists, so the static `Instance` is null on game-less screens — see ADR-0001 for the null-handling contract.
- `MapComponent` (base class): `WorkPriorityUpdater(Map map) : MapComponent(map)` and `ScheduleUpdater(Map map) : MapComponent(map)`. One instance per `Map`. Override `MapComponentTick()` for per-tick work (the updaters tick-gate with `Find.TickManager.TicksGame & mask` and pause checks). Retrieved via `map.GetComponent<T>()`.
- `Game`: `Current.Game` (null when no game loaded — guarded before use in `ForceUpdateAssignments`/`ForceUpdateSchedules`). `Current.Game.playSettings.useWorkPriorities` toggles vanilla priority mode.

### Persistence — Scribe (`Verse`)
- `Scribe.mode` + `LoadSaveMode` (`Saving`/`LoadingVars`): branch in `ExposeData` (validate on save).
- `Scribe_Values.Look(ref field, "label", defaultValue)` — scalar persistence (`bool` flags, `int` thresholds).
- `Scribe_Collections.Look(ref collection, "label", LookMode.*, ...)` — collection persistence. `LookMode.Value`, `LookMode.Def`, `LookMode.Reference` used; the dictionary overload takes auxiliary `ref` key/value lists.
- Convention: reference-typed entries (`Pawn`) use `LookMode.Reference`; `Def`-typed use `LookMode.Def` **in save-game data only** (`GameComponent`); the component prunes destroyed pawns / missing defs in `Validate()` before saving.
- `Scribe_Collections.Look(..., LookMode.Deep)` sets the list to `null` when the node is missing (settings saved by older versions) — null-coalesce after load.
- **ModSettings pitfall:** mod settings load in the `Mod` constructor, before defs load. `Scribe_Defs` / `LookMode.Def` there resolve to null and log errors. Persist defs in `ModSettings` by name through `DefCache<T>` (see `LordKuper.Common-1.6.md`), and touch only `DefName` on the load path.

### Defs & data (`Verse` / `RimWorld`)
- `DefDatabase<T>` (static): `AllDefsListForReading` (enumerate, e.g. all `WorkTypeDef`), `GetNamed("name")`, `GetNamedSilentFail("name")` (null on miss). Used for `WorkTypeDef`, `TimeAssignmentDef`, `TraitDef`.
- `Def` (base) / `defName`, `label`: identity and display.
- `WorkTypeDef` (`RimWorld`): the unit of work assignment — the central domain type across the mod.
- `TimeAssignmentDef` (`RimWorld`) + `TimeAssignmentDefOf.Work` / def names `"Anything"`, `"NightOwl"`: timetable slot types for schedules.
- `TraitDef` + `"NightOwl"`: trait lookup for schedule grouping.
- `*DefOf` static caches (e.g. `TimeAssignmentDefOf`): populated by the game after defs load; reading them in tests requires the game's def-load to have run (see test conventions).

### Pawns & gameplay (`RimWorld` / `Verse`)
- `Pawn` (`Verse`): `Destroyed`, `Dead`, `Downed`, `InMentalState`, `InContainerEnclosed`, `timetable` (`Pawn_TimetableTracker` with `GetAssignment(hour)`/`SetAssignment`), `workSettings` (`Pawn_WorkSettings`, `EverWork`), `WorkTypeIsDisabled(WorkTypeDef)`.
- `Passion` (enum, `RimWorld`): skill passion levels (consumed via `LordKuper.Common` `PawnHelper`/`PassionHelper`).
- `PawnColumnWorker` (`RimWorld`, base class): `AutoWorkPriorities`/`AutoWorkSchedule : PawnColumnWorker`. Overrides `DoCell(Rect, Pawn, PawnTable)`, `GetMinWidth(PawnTable)`. `PawnTable` is the owning table. These are **UI entry points** subject to the ADR-0001 guard.
- `Find` (static service locator, `Verse`): `Find.Maps`, `Find.TickManager` (`TicksGame`, `CurTimeSpeed`, `TimeSpeed.Paused`), `Find.WindowStack`, `Find.PlaySettings.useWorkPriorities`.

### Needs (`RimWorld` / `Verse`)
Consumed by the work-type needs filter (ADR-0006). Source: sprint 002 audit, Background research R-1..R-8.

**API**
- `Need` (abstract): `NeedDef def`; `virtual float MaxLevel => 1f`; `virtual float CurLevel` (clamped to `[0, MaxLevel]`); **`float CurLevelPercentage => CurLevel / MaxLevel`**; `virtual float CurInstantLevel => -1f` (seekers override it with the momentary target); `protected List<float> threshPercents` (per instance, not on `NeedDef`); `bool IsFrozen` (suspended, asleep with `freezeWhileSleeping`, mental state with `freezeInMentalState`, dormant, unspawned/caravan/cryptosleep).
- `Pawn_NeedsTracker` (`pawn.needs`): `List<Need> AllNeeds`; `Need TryGetNeed(NeedDef)` returns `null` when the pawn lacks the need (linear scan, ~10 entries); `TryGetNeed<T>()`; typed shortcut fields (`mood`, `food`, `rest`, `joy`, `beauty`, `roomsize`, `outdoors`, `indoors`, `drugsDesire`, `comfort`, `learning`, `play`), each null when absent. `NeedsTrackerTick` runs `NeedInterval` every 150 ticks (1 h = 2500 ticks, 1 day = 60000).
- `NeedDef` applicability fields: `needClass`, `minIntelligence`, `colonistsOnly`, `colonistAndPrisonersOnly`, `playerMechsOnly`, `slavesOnly`, `neverOnPrisoner`, `neverOnSlave`, `onlyIfCausedByHediff/Gene/Trait/Ideo`, `titleRequiredAny`, `hediffRequiredAny`, `nullifyingPrecepts`, `requiredComps`, `developmentalStageFilter`. Display fields: `showOnNeedList` (default true), `listPriority`, `major`, `baseLevel` (0.5), `fallPerDay`, `seekerRisePerHour`, `seekerFallPerHour`, `freezeWhileSleeping`, `freezeInMentalState`, `description`.
- `Pawn_NeedsTracker.ShouldHaveNeed` gates on intelligence, developmental stage, colonist/prisoner/slave/mech flags, hediff/gene/trait/ideo disables and `onlyIfCausedBy*`, mutant whitelists (Anomaly), titles, nullifying precepts, `requiredComps`, and a hard-coded `defName == "Authority"` → false. Food requires `EatsFood`; Rest requires `needsRest`.
- UI enumeration: `DefDatabase<NeedDef>.AllDefsListForReading` (via `DefProvider.Current` in this project). Do not filter on `showOnNeedList` (it hides Mood and RoomSize); exclude `playerMechsOnly` and `Authority`.
- Pitfalls: `pawn.needs` is null after death → always `pawn.needs?.TryGetNeed(def)`. Read `CurLevelPercentage`, not `CurLevel`: Food and MechEnergy have `MaxLevel != 1`. For seekers (Beauty, Comfort, RoomSize) `CurLevel` is the smoothed value thought workers read; `CurInstantLevel` is the target.

**Core needs** (`Needs.xml`; Beauty/Outdoors verified locally)

| defName | class | applicability | listPriority | showOnNeedList |
|---|---|---|---|---|
| Mood | Need_Mood | Humanlike; Baby/Child/Adult | 1000 (major) | false |
| Food | Need_Food | EatsFood | 800 | true |
| Rest | Need_Rest | needsRest | 700 | true |
| Joy | Need_Joy | Humanlike, colonistsOnly, neverOnPrisoner, neverOnSlave, Adult | 500 | true |
| Beauty | Need_Beauty (seeker) | Humanlike, colonistAndPrisonersOnly | 300 | true |
| Comfort | Need_Comfort (seeker) | Humanlike, colonistAndPrisonersOnly; nullified by Comfort_Ignored | 200 | true |
| DrugDesire | Need_Chemical_Any | Humanlike, colonistAndPrisonersOnly | 150 | true |
| Outdoors | Need_Outdoors | Humanlike, colonistAndPrisonersOnly, freezeWhileSleeping | 100 | dynamic |
| Indoors | Need_Indoors | only if caused by ideo/hediff/trait/gene | 100 | dynamic |
| RoomSize | Need_RoomSize (seeker) | Humanlike, colonistAndPrisonersOnly | 100 | false |

Other subclasses: `Need_Chemical` (per drug); DLC `Need_Deathrest`, `Need_KillThirst`, `Need_Learning`, `Need_Play`, `Need_MechEnergy`, `Need_Suppression`, `Need_Sadism` [defNames unverified]. `Need_Authority` is always excluded. Hemogen is a gene resource, not a `Need`.

**Applicability by pawn category.** WorkManager manages only spawned, player-faction, non-mechanoid pawns with `workSettings.EverWork` and `skills`, so mech needs are out of scope. Slaves: no Joy; Suppression [unverified defName]. Guests/lodgers in the player faction: Beauty/Comfort/Outdoors/RoomSize apply, Joy does not. Biotech children: no Joy; Learning (babies: Play) [unverified]. Anomaly mutants: whitelisted needs only. Undergrounder trait disables Outdoors and enables Indoors; Ideology "Indoors: preferred" and the Biotech indoor-dweller gene disable or neutralise Outdoors [unverified]; `Need_Outdoors.Disabled` pins `CurLevel = 1`. A configured need is often absent for a given pawn; vanilla convention (ThoughtWorkers skip absent needs) is that an absent need imposes nothing.

**Outdoors** (`Need_Outdoors`). Initial level 1.0; each 150-tick interval adds `delta × 0.0025`. "Outdoors" = unspawned or `Position.UsesOutdoorTemperature(map)` (room touches the map edge, or ≥ 25% open-roof cells). Frozen while asleep; in bed, negative deltas ×0.2. A source never pushes the level below its floor.

| situation | delta | per hour | floor |
|---|---|---|---|
| outdoors, no roof | +8 | +33.3% | — |
| indoors, no roof | +5 | +20.8% | — |
| outdoors, thin roof | +1 | +4.2% | — |
| indoors, thin roof | −0.32 | −1.33% | 0.2 |
| outdoors, thick roof | −0.4 | −1.67% | 0.2 |
| indoors, thick roof | −0.45 | −1.875% | 0.0 |

| CurLevel | category | thought | mood |
|---|---|---|---|
| > 0.8 | Free | — | 0 |
| > 0.6 | NeedFreshAir | stuck indoors | −1 |
| > 0.4 | CabinFeverLight | trapped indoors | −3 |
| ≥ 0.2 | CabinFeverSevere | cabin fever | −5 |
| > 0.05 | Trapped | trapped underground | −7 |
| ≤ 0.05 | Entombed | entombed underground | −9 |

Only enclosed rooms under overhead mountain go below 20%; mining tunnels connected to the outside usually bottom out at cabin fever (−5). In 1.6 "cabin fever" is a thought stage, not a mental state. `ThoughtWorker_NeedOutdoors` is inactive when `HostFaction != null`. Indoors mirrors Outdoors (same thresholds, moods −1/−3/−5/−7/−8, no floor). Reference: https://rimworldwiki.com/wiki/Outdoors.

**Beauty** (`Need_Beauty : Need_Seeker`). `CurInstantLevel = Clamp01(0.4 + avgBeauty × 0.1)` with `avgBeauty = BeautyUtility.AverageBeautyPerceptible(PositionHeld, MapHeld)`; 0.5 when unspawned or blind. `CurLevel` moves toward the target at +0.32/h up, −0.08/h down; frozen while asleep.

| CurLevel | category | thought | mood |
|---|---|---|---|
| > 0.99 | Beautiful | gorgeous | +15 |
| > 0.85 | VeryPretty | beautiful | +10 |
| > 0.65 | Pretty | pretty | +5 |
| > 0.35 | Neutral | — | 0 |
| > 0.15 | Ugly | unsightly | −5 |
| > 0.01 | VeryUgly | ugly | −10 |
| ≤ 0.01 | Hideous | hideous | −15 |

Mining context: `Filth_RubbleRock` Beauty −15 indoors / −4 outdoors; rough stone −1, smooth stone +2, natural soil −3 indoors. An average of −1 gives a 0.3 target (unsightly).

**Other needs.** Mood: hidden from the need list but meaningful as a filter. Rest: tired < 0.28, very tired < 0.14. Food: hungry at `FoodLevelPercentageWantEat × 0.8`, urgent at `× 0.4`; churns constantly. Joy: thresholds 0.15/0.3/0.7/0.85. Comfort: noisy (instant level 0 unless a comfort item was used in the last 15 ticks). RoomSize: < 0.01 confined −10, < 0.3 cramped −5, ≥ 0.7 spacious +5. DrugDesire/Chemical: conditional on addiction.

**Precedents** (need-aware work assignment)
- Free Will (https://github.com/paul-freeman/rimworld-freewill, `Priority.cs`): soft additive scoring from beauty category and thoughts; no Outdoors, no hysteresis.
- Brrr and Phew (Continued) (https://steamcommunity.com/sharedfiles/filedetails/?id=2195938471): keeps a cabin-fevered pawn outside until Outdoors reaches 75% (latch); job control, not a priority filter.
- SmarterScheduling (https://github.com/maarxx/SmarterScheduling): latch-based schedule state machine (behaviour mod).
- Vanilla `DrugPolicyEntry.onlyIfMoodBelow` / `onlyIfJoyBelow`: plain percent threshold, no latch — closest vanilla analogue.
- Work Tab, Priority Master, AutoPriorities, Better Pawn Control, Colony Manager: no known need-gated work types [unverified].
- LordKuper.Common `PawnFilter` limits: stateless threshold/range checks on every update. No comparable filter uses hysteresis; the WorkManager needs filter is stateless too (ADR-0006).

### UI — menus, tooltips, formatting (`Verse`)
Verified 2026-09-29: member names present in the local 1.6 `Assembly-CSharp.dll`; signatures confirmed by the project compiling against it with 0 warnings.
- `FloatMenu` (`Window`): `new FloatMenu(List<FloatMenuOption>)`, opened with `Find.WindowStack.Add(...)`. Used for the schedule / work-type pickers (`Settings_Schedules.cs`, `Settings_WorkTypes.cs`) and the Needs "Add" menu (ADR-0008).
- `FloatMenuOption`: ctor `(string label, Action action)`; public field `TipSignal? tooltip` — set via object initializer `tooltip = new TipSignal(text)` to show a per-option hover tooltip (ADR-0008, AC-31). The field type is `TipSignal`, not `string`.
- `TipSignal` (struct): tooltip payload; constructed from a `string`. `TooltipHandler.TipRegion(Rect, string)` remains the rect-based route used elsewhere (`Settings_Schedules.cs`).
- `TexUI.DismissTex` (`static Texture2D`): stock "dismiss / remove" icon; passed to Common's `IconButton` for the Needs entry remove button (ADR-0008).
- `GenText.ToStringPercent(this float)` (extension): formats `0..1` as a percentage string (e.g. `0.3f` → `"30%"`); used in the rule summary's Needs part (`WorkTypeAssignmentRule.cs`).

### Localization (`Verse`)
- `string.Translate()` (extension): keyed-text lookup against `Languages/<locale>/Keyed/*.xml`. Parameterized form `"Key".Translate(arg0, …)` substitutes positional placeholders.
- Keyed XML lives in `1.6/Languages/{English,Russian,ChineseSimplified}/Keyed/WorkManager_Keyed.xml` (English-only for legacy 1.1–1.5).
- **Project key-naming conventions** (observed in `Resources.cs` + `WorkManager_Keyed.xml`); two forms coexist, scoped by settings area:
  - `LordKuper.WorkManager.Settings.WorkTypes.<Name>` — the whole Work Types (work-type assignment rule) settings area, built from nested `nameof` in `Resources.Strings.Settings.WorkTypes` (e.g. `LordKuper.WorkManager.Settings.WorkTypes.NeedsLabel`). New keys in that area use this form (ADR-0008).
  - `WorkManager.<Name>` and `WorkManager.Settings_<Area>_<Name>` — older form used elsewhere (e.g. `WorkManager.PawnEnableTooltip`, `WorkManager.Settings_Schedule_AddWorkShift`, see ADR-0005). New keys in those areas follow their siblings.
  - Enum-derived keys use `{ModId}.{EnumType}.{Value}.Label`.

### Startup hooks (`Verse` / `UnityEngine`)
- `[StaticConstructorOnStartup]` (`Verse`): marks a type whose static ctor RimWorld runs once after defs load — used by `Resources.Textures` to load `Texture2D` assets via `ContentFinder<Texture2D>.Get(...)` *(unverified exact signature)*.
- Harmony bootstrap (`HarmonyLib`, see `Lib.Harmony-2.4.2.md`): `new Harmony(ModId).PatchAll(...)` in the `Mod` ctor, guarded by a static `_isPatched` flag.

## Version-specific notes
- 1.6 requires `LordKuper.Common` as a mod dependency (`About.xml` `modDependenciesByVersion`); earlier lines do not.
- The TFM is `net472`, fixed by RimWorld's Mono/Unity runtime (see `dotnet-framework-4.7.2.md`); the game's BCL is the net472 surface regardless of the build SDK's C# language version.
- API members can be added/changed across major lines without deprecation notices; the game ships no compatibility contract. Mismatches surface at game load (`MissingMethodException`/`TypeLoadException`), not at compile time against a different install.

## Deprecations and breaking changes from prior version
- No published changelog for the API surface. Treat the 1.6 install as the baseline. Cross-line breaks (1.5→1.6) that affect consumed members must be found by recompiling against the new install and by runtime testing; `About.xml` `supportedVersions` gates which lines are claimed.

## Project conventions
- Compile against, never ship: `Assembly-CSharp` + Unity modules are `Private=False`; the game provides them at runtime.
- Patch, don't fork: behaviour is injected via Harmony, not by replacing game code.
- UI entry points (`PawnColumnWorker` overrides, Harmony patch methods) guard `WorkManagerGameComponent.Instance` per ADR-0001 because RimWorld can invoke them with no active game; game-scoped components (`MapComponent` ticks) rely on the `Map`⇒`Game` lifecycle.
- All user-facing text goes through `.Translate()` keyed lookups; def names (`"Anything"`, `"NightOwl"`) are not user-facing text and stay as literals.
- Def lookups that may miss use `GetNamedSilentFail` + a fallback, never an unguarded `GetNamed` for optional defs.
- Never `Scribe_Defs` / `LookMode.Def` in `ModSettings`; persist by defName (see Persistence — Scribe).
- Need levels are read as `pawn.needs?.TryGetNeed(def)?.CurLevelPercentage`; a missing need or null tracker means "no constraint" (ADR-0006).

## Known issues and workarounds
- `Current.Game` / `Find.*` are null outside an active game; always guard on game-less paths (the source already does for `ForceUpdate*`).
- Testing any RimWorld-typed unit needs `Assembly-CSharp` resolvable at both NUnit discovery and execution; the mechanism (build-time `CopyRimWorldTestDeps` + global `[SetUpFixture]` `AssemblyResolve` handler) is documented once in [NUnit-4.6.1 — Known issues](NUnit-4.6.1.md#known-issues-and-workarounds) (decision: ADR-0002 amendment 2026-09-29); `*DefOf` caches are only populated after the game's def-load, so prefer pure-logic units that do not require populated defs.
- No primary API reference is fetchable; keep this doc grounded in decompiled `Assembly-CSharp` and observed usage, and re-verify *(unverified)* signatures against the install when touched.
