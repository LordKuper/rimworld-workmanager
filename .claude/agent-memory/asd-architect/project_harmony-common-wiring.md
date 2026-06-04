---
name: harmony-common-wiring
description: How WorkManager references Harmony and LordKuper.Common (do-not-bundle constraints)
metadata:
  type: project
---

WorkManager (Source/WorkManager/WorkManager.csproj) dependency wiring:
- Lib.Harmony: PackageReference with `ExcludeAssets=runtime`, `PrivateAssets=all`. Compile against NuGet contract; the runtime `0Harmony.dll` is provided by the game (brrainz.harmony mod). Never ship a second 0Harmony.dll.
- LordKuper.Common: file `<Reference>` via `$(LordKuperCommonAssembliesDir)\LordKuper.Common.dll` with `<Private>False</Private>`. Reference-only; not copied to output Assemblies/. Provided by the parent mod environment for the RW 1.6 line.

**Why:** Both are shared/host-provided assemblies. Bundling Harmony breaks the game's shared instance (type-identity conflicts); bundling/mismatching Common surfaces as load-time MissingMethod/TypeLoad rather than compile errors.

**How to apply:** Do not change Harmony asset flags or Common's Private=False without Complication Approval. Harmony patches use declarative attributes + manual AccessTools.TypeByName for optional mods (e.g. WorkTab); optional-mod patches must tolerate absent types.
