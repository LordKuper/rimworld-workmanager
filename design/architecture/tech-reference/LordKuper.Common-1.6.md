---
responsibility:
  owns: project-vetted reference for the LordKuper.Common shared library (RW 1.6 line) — consumed API surface and conventions
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), adr/ (decisions), commands.yaml (commands)
---

# LordKuper.Common @ 1.6 (RimWorld 1.6 line)

## Canonical source
- Source / repo: https://github.com/LordKuper/RimWorld-Common
- Last verified: 2026-06-04

## Versioning model (read first)
- LordKuper.Common is a **reference-only DLL**, not a NuGet package. There is no NuGet version.
- It is consumed as a file reference: `<Reference Include="LordKuper.Common"><HintPath>$(LordKuperCommonAssembliesDir)\LordKuper.Common.dll</HintPath><Private>False</Private></Reference>` in `Source/WorkManager/WorkManager.csproj`.
- `$(LordKuperCommonAssembliesDir)` resolves under the `1.6` assemblies tree (the per-game-version layout `...\1.6\Assemblies`). The version tracks the RimWorld major line (1.6), not a semver of the library.
- `<Private>False</Private>`: the DLL is provided by the parent mod's environment and is NOT copied into WorkManager's output `Assemblies/` folder. Compile-time contract only.

## API surface used in project
Grounded in actual `using`/usages across `Source/WorkManager/`. Namespaces consumed: `LordKuper.Common`, `LordKuper.Common.Cache`, `LordKuper.Common.Helpers`, `LordKuper.Common.Filters`, `LordKuper.Common.UI`, `LordKuper.Common.UI.Widgets`, `LordKuper.Common.Compatibility`, `LordKuper.Common.Resources`.

### `LordKuper.Common` (root)
- `RimWorldTime`: game-time value type. Used as the parameter type of `TimedCache.Update(RimWorldTime)` overrides; constant `RimWorldTime.HoursInDay` seeds cache lifetimes (`PawnWorkCache`).

### `LordKuper.Common.Cache`
- `TimedCache` (base class): time-invalidated cache base. `PawnSkillCache` and `PawnWorkCache` inherit it, pass a lifetime to `base(...)`, and override `bool Update(RimWorldTime time)` calling `base.Update(time)` to decide whether to refresh.
- `DefCache<T>` where `T : Def`: base for def-keyed config objects. `WorkTypeAssignmentRule : DefCache<WorkTypeDef>` uses `DefName`, `Label`, the `(string?)` ctor, and `ExposeData()`/`base.ExposeData()`.

### `LordKuper.Common.Helpers`
- `PawnHelper.GetWorkPassion(Pawn, WorkTypeDef)`: passion lookup (`PawnWorkCache`).
- `PassionHelper.Passions`: collection of passion descriptors with `LearnRateFactor`, `ForgetRateFactor`, `Passion` (consumed by `PassionHelper` to compute normalized scores).
- `MathHelper.NormalizeValue(value, FloatRange)`: range normalization (`PassionHelper`).

### `LordKuper.Common.Filters`
- `PawnFilter`: rich pawn predicate with flags `TriStateMode`, `FilterPawnTypes`, `AllowedPawnTypes`, `ForbiddenPawnTypes`, `FilterPawnHealthStates`, `AllowedPawnHealthStates`, `ForbiddenPawnHealthStates`, `FilterWorkPassions`, `FilterPawnCapacities`, `FilterPawnSkills`, `FilterPawnStats`, `FilterPawnTraits`, `FilterWorkCapacities`, `FilterPawnPrimaryWeaponTypes`, `AllowedPawnPrimaryWeaponTypes`. Methods: `Validate()`, `SatisfiesFilter(Pawn, Def)`, `GetSummary(int indent)`, `GetFilteredPawns(IEnumerable<Map>, ...)`, static `Combine(main, fallback)`.
- Enums/types: `PawnType` (`Colonist`, `Guest`, `Slave`, `Prisoner`, `Animal`, `Undefined`), `PawnHealthState` (`[Flags]`: `None`, `Healthy`, `Resting`, `NeedsTending`, `Downed`, `Mental`, `Dead`), `PawnPrimaryWeaponType` (`Ranged`). `PawnHealthState` is aliased locally (`using PawnHealthState = LordKuper.Common.Filters.PawnHealthState;`) to disambiguate.

### `LordKuper.Common.UI` / `LordKuper.Common.UI.Widgets`
- `Layout`: layout helpers and constants — `ElementGap`, `ElementGapTiny`, `ElementGapSmall`, `GetRightColumnRect(...)`, `GetLeftColumnRect(...)`.
- `Buttons`: `IconButtonSize`, `DoIconButton(rect, IconButton)`, `DoIconButtonToggle(rect, getter, setter, ...)`.
- `IconButton`: button descriptor `(texture, action, tooltip)`.
- Widgets namespace consumed by `Settings_WorkTypes` for settings UI.

### `LordKuper.Common.Compatibility`
- Compatibility helpers consumed by `Settings_WorkPriorities` for inter-mod integration.

### `LordKuper.Common.Resources`
- `Resources.Strings`: shared string table, imported statically (`using static LordKuper.Common.Resources.Strings;`) in `Resources.cs`. Also string-builder/colorize extensions used in `WorkTypeAssignmentRule.Description` (`AsTipTitle()`, `AppendLineIndented`, `AppendIndented`, `Colorize`, `ColoredText.*` colors) originate from the shared resources/extension surface.

## Version-specific notes
- The contract tracks RimWorld 1.6. Any RW major bump (e.g. 1.7) means a new `1.7\Assemblies\LordKuper.Common.dll` and a separate tech-reference doc; the API surface above is validated against the 1.6 DLL only.
- Because it is reference-only with `Private=False`, mismatches surface at game load (missing-member `TypeLoadException`/`MissingMethodException`), not at compile time against a different deployed DLL. Keep the build-time DLL in lockstep with the DLL the parent mod ships for 1.6.

## Deprecations and breaking changes from prior version
- No NuGet semver to diff against. Treat the 1.6 DLL as the baseline contract for this RW line. If members consumed above disappear or change signature in a refreshed 1.6 DLL, that is a breaking change requiring code updates here.

## Project conventions
- Consumed strictly as a compile-time reference (`Private=False`); never bundle `LordKuper.Common.dll` in WorkManager's `Assemblies/` output.
- Prefer `DefCache<T>` / `TimedCache` base classes for new def-keyed and time-bounded caches rather than hand-rolling cache invalidation.
- Pawn eligibility is expressed through `PawnFilter` (flags + `Combine` fallback merging), not ad-hoc predicates.
- UI is composed from `Layout` + `Buttons` + `IconButton` rather than raw `Widgets`/IMGUI where a Common helper exists.
- Shared strings come from `LordKuper.Common.Resources.Strings` (static import); mod-specific strings live in WorkManager's own `Resources.Strings`.

## Known issues and workarounds
- No compile-time guard against deploying a different 1.6 DLL than the one referenced. Workaround: pin `$(LordKuperCommonAssembliesDir)` to the same 1.6 assemblies tree the game loads, and validate at game start (the mod logs its version on init).
- Test code that exercises types touching `LordKuper.Common` indirectly pulls in RimWorld types — those require the RimWorld `AppDomain.AssemblyResolve` handler registered in a global `[SetUpFixture]` before load (see test conventions).
