---
responsibility:
  owns: project-owner custom rules read by all agents in all phases
  excludes: phase-specific rules (design-only, coding-only)
  delegates_to: custom-design-rules.md (design/design-review), custom-coding-rules.md (impl/impl-review), .asd/rules/ (workflow rules)
---

# Custom Common Rules

## What this project is

WorkManager is a RimWorld mod that automatically assigns pawn work priorities and work schedules. It is a downstream consumer of the shared `LordKuper.Common` library (`d:\Projects\rimworld-common`). Rules here are inherited from that parent library and adapted to WorkManager's actual setup; where WorkManager diverges, the WorkManager wording wins.

## Project layout

- All source lives under `Source/`: the solution `Source/WorkManager.slnx`, the shared `Source/Directory.Build.props`, and one folder per project.
- **Production**: `Source/WorkManager/` (`WorkManager.csproj`). Target framework `net472`. `Nullable enable`, `GenerateDocumentationFile`. References RimWorld `Assembly-CSharp` + Unity modules (via `$(RimWorldManagedDir)`), `Lib.Harmony` 2.4.2 (compile-only: `PrivateAssets=all`, `ExcludeAssets=runtime`), and `LordKuper.Common.dll` (compile-only reference, `Private=False`, resolved via `$(LordKuperCommonAssembliesDir)`). Build output goes to `1.6/Assemblies/`.
- **Tests**: `Source/WorkManager.Tests/` (`WorkManager.Tests.csproj`). NUnit 4.x + NUnit3TestAdapter + Microsoft.NET.Test.Sdk + **FluentAssertions 7.x**, `net472`, `Nullable enable`. (FluentAssertions is the assertion standard inherited from the parent; the package is added on first real test — see custom-coding-rules.md.)

## Upstream dependency

- `LordKuper.Common` is an upstream integration contract, not editable from here. Consume its public surface; do not fork or reimplement what it already provides. Common is resolved at compile time from `$(LordKuperCommonDir)\1.6\Assemblies` (defaults to `..\..\rimworld-common`, overridable via `LORDKUPER_COMMON_DIR`).
- RimWorld build requires `RimWorldManagedDir` / `RIMWORLD_DIR` pointing at RimWorld's `Managed` dir.
