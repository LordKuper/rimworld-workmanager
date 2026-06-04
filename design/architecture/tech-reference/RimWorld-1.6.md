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
- Last verified: 2026-06-04
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
- Convention: reference-typed entries (`Pawn`) use `LookMode.Reference`; `Def`-typed use `LookMode.Def`; the component prunes destroyed pawns / missing defs in `Validate()` before saving.

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

### Localization (`Verse`)
- `string.Translate()` (extension): keyed-text lookup against `Languages/<locale>/Keyed/*.xml`. Parameterized form `"Key".Translate(arg0, …)` substitutes positional placeholders.
- Keyed XML lives in `1.6/Languages/{English,Russian,ChineseSimplified}/Keyed/WorkManager_Keyed.xml` (English-only for legacy 1.1–1.5).
- **Project key-naming convention** (observed in `Resources.cs` + `WorkManager_Keyed.xml`): `WorkManager.<Name>` and `WorkManager.Settings_<Area>_<Name>` (e.g. `WorkManager.PawnEnableTooltip`, `WorkManager.Settings_Schedule_AddWorkShift`); enum-derived keys use `{ModId}.{EnumType}.{Value}.Label`. New keys follow this scheme (see ADR-0005).

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

## Known issues and workarounds
- `Current.Game` / `Find.*` are null outside an active game; always guard on game-less paths (the source already does for `ForceUpdate*`).
- Testing any RimWorld-typed unit requires the `AppDomain.AssemblyResolve` handler registered in a global `[SetUpFixture]` resolving from `$(RimWorldManagedDir)` before the type loads (see ADR-0002 and `LordKuper.Common-1.6.md`); `*DefOf` caches are only populated after the game's def-load, so prefer pure-logic units that do not require populated defs.
- No primary API reference is fetchable; keep this doc grounded in decompiled `Assembly-CSharp` and observed usage, and re-verify *(unverified)* signatures against the install when touched.
