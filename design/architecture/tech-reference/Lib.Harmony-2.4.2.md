---
responsibility:
  owns: project-vetted reference for Lib.Harmony 2.4.2 (patching APIs used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), adr/ (decisions), commands.yaml (commands)
---

# Lib.Harmony @ 2.4.2

## Canonical source
- Official docs: https://harmony.pardeike.net/
- API docs: https://harmony.pardeike.net/api/HarmonyLib.html
- NuGet: https://www.nuget.org/packages/Lib.Harmony/2.4.2
- Last verified: 2026-06-04

## Package wiring (how it is consumed)
- Referenced as `<PackageReference Include="Lib.Harmony" Version="2.4.2" />` in `Source/WorkManager/WorkManager.csproj` with `PrivateAssets=all`, `ExcludeAssets=runtime`, `IncludeAssets=compile; build; native; contentfiles; analyzers; buildtransitive`.
- Rationale for `ExcludeAssets=runtime`: RimWorld ships its own Harmony (the `brrainz.harmony` mod / `0Harmony.dll` loaded by the game). The mod compiles against the NuGet contract but MUST NOT ship its own `0Harmony.dll`; the runtime instance is provided by the game environment. Do not change these asset flags without Complication Approval — bundling a second `0Harmony.dll` causes load-order and type-identity conflicts.

## API surface used in project
All usages live under `Source/WorkManager/`. Namespace `HarmonyLib`.

- `Harmony` (ctor + `PatchAll`): bootstrap in `WorkManagerMod` ctor. `new Harmony(ModId)` then `harmony.PatchAll(Assembly.GetExecutingAssembly())` applies all attribute-declared patches in the mod assembly. Guarded by a static `_isPatched` flag so patching runs once.
- `harmony.Patch(original, prefix:, postfix:)` (manual patching): `WorkTabPatch.Apply(Harmony)` patches types from the optional WorkTab mod that are not statically referenceable. Uses `HarmonyMethod` wrappers, e.g. `harmony.Patch(method, postfix: new HarmonyMethod(typeof(WorkTabPatch), nameof(DoWindowContentsPostfix)))`.
- `[HarmonyPatch(typeof(T), nameof(T.Method))]` (declarative target): used on `DefGeneratorPatch`, `MainTabWindowWorkPatch`, `WidgetsWorkPatch`, `PawnColumnWorkerWorkPriorityPatch`.
- `[HarmonyPatch(typeof(T))]` class-level target + per-method `[HarmonyPatch(nameof(...))]` + `[HarmonyPostfix]` / `[HarmonyPrefix]`: used in `PawnColumnWorkerWorkPriorityPatch` and `WidgetsWorkPatch` to patch multiple methods of one type.
- `[HarmonyBefore("fluffy.worktab")]`: ordering hint on `DefGeneratorPatch` so columns are injected before the WorkTab mod's own def generation runs.
- `[HarmonyPostfix]` / `[HarmonyPrefix]` method roles.
- Injected parameters: `__instance` (patched instance), `ref __result` (return-value rewrite, e.g. `GetMinHeaderHeightPostfix` adds to header height), `ref` on game args (e.g. `ref Rect rect` in `WorkTabPatch.DoHeaderPrefix`). Parameter names match the original method signature for argument capture (`x`, `y`, `p`, `wType`, etc.).
- `AccessTools.TypeByName(string)`: resolve types from the optional WorkTab assembly by full name without a compile-time reference.
- `AccessTools.Method(Type, string)`: resolve a `MethodInfo` for manual patching.
- `HarmonyMethod(Type, string)`: wrap a patch method for the manual `Patch` call.

## Version-specific notes
- 2.4.x is the current 2.x line and is API-compatible with the 2.x patch model (attributes, injected params, `AccessTools`) used here. No 2.x source changes are required for this project.
- `ExcludeAssets=runtime` means the 2.4.2 `0Harmony.dll` is never copied to the output `Assemblies/` folder; the effective runtime Harmony version is whatever RimWorld 1.6 / the Harmony mod provides. Treat 2.4.2 as the compile-time contract only.

## Deprecations and breaking changes from prior version
- No breaking changes affect this project versus prior 2.x usage. The declarative/manual patch APIs, injected-parameter conventions, and `AccessTools` helpers are stable across 2.x.

## Project conventions
- Patch classes are `public static`, named `<Target>Patch`, marked `[UsedImplicitly]`, and live in `Source/WorkManager/Patches/`.
- Patch methods that touch the game are kept side-effect-minimal and null-guard `WorkManagerGameComponent.Instance` before use.
- ReSharper naming for Harmony magic params (`__instance`, `__result`) is suppressed with attribute-based `[SuppressMessage("ReSharper", "InconsistentNaming")]`, never pragmas (per custom coding rules).
- Optional-mod patches (WorkTab) are applied manually via `AccessTools.TypeByName` so the mod compiles and loads even when the optional mod is absent; manual `Apply` is invoked from the bootstrap only after `PatchAll`.
- Patching is idempotent via the static `_isPatched` guard.

## Known issues and workarounds
- Shipping a second `0Harmony.dll` breaks the game's shared Harmony instance. Workaround: the `ExcludeAssets=runtime` flag already prevents this — do not remove it.
- `AccessTools.TypeByName` returns `null` when the optional mod is not loaded; manual patch wiring must tolerate absent types (WorkTab integration is gated by its own `Initialize`/active check before `Apply`).
