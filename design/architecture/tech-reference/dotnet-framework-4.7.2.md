---
responsibility:
  owns: project-vetted reference for the .NET Framework 4.7.2 target (TFM facts, BCL/runtime constraints, language-version interplay, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), adr/ (decisions), commands.yaml (commands)
---

# .NET Framework @ 4.7.2 (`net472`)

## Canonical source
- TFM / lifecycle: https://learn.microsoft.com/dotnet/framework/migration-guide/versions-and-dependencies
- C# language versioning: https://learn.microsoft.com/dotnet/csharp/language-reference/configure-language-version
- Nullable reference types support: https://learn.microsoft.com/dotnet/csharp/nullable-references
- Last verified: 2026-06-04

## Why net472 (read first)
- `net472` is **platform-pinned by RimWorld's Mono/Unity runtime** — the game loads mod assemblies into its own runtime, whose BCL surface matches .NET Framework 4.7.x. **Do not upgrade the TFM** (a `stack.html` hard constraint); a different TFM would not load or would bind against members the game runtime lacks.
- Both `WorkManager.csproj` and `WorkManager.Tests.csproj` target `net472`.

## Build SDK vs target framework (the key interplay)
- The build uses **.NET SDK 10.0.300** (build-only toolchain) with `LangVersion=latest`, which resolves the C# language version from the Roslyn compiler in that SDK — currently **C# 14**. The TFM (`net472`) does **not** cap the *language* version.
- Consequence: modern C# *syntax* compiles (collection expressions `[]`, primary constructors, `record`, pattern matching, target-typed `new`, file-scoped namespaces — all used in this codebase), but anything needing **newer BCL/runtime support** is unavailable on net472 unless polyfilled.
  - Language features that need only the compiler: available (e.g. `[]`, primary ctors, switch/property patterns, `nameof`, ranges where the compiler lowers them).
  - Language features that need runtime/BCL types absent on net472: **not** available without shims — e.g. `init`/`record` need `IsExternalInit` (compiler-synthesized on modern SDKs, usually fine), `required` members need `RequiredMemberAttribute`, `System.Index`/`System.Range` for some forms, `Span<T>`-heavy APIs, `async` streams' newer surfaces. Verify any such feature compiles AND has the runtime type before relying on it.

## BCL / runtime constraints on net472
- BCL is the .NET Framework 4.7.2 surface — **not** .NET (Core) 5+/8+. Newer BCL APIs (e.g. much of `System.Text.Json`, newer `Span`/`Memory` overloads, `DateOnly`/`TimeOnly`, many `static abstract` interface members) are absent.
- No `<Nullable>` runtime enforcement: nullable reference types are a **compile-time** analysis only (see below).
- Runtime is the game's Mono/Unity, not desktop CLR — avoid relying on desktop-CLR-only behaviour; keep code to portable BCL the game runtime exposes.

## Nullable reference types support on net472
- `<Nullable>enable</Nullable>` is set in both projects and is a **must-keep** constraint (`stack.html`). NRT is a Roslyn compile-time feature and works fully on net472 — the nullable annotations and warnings are produced by the compiler, independent of the target runtime.
- The nullable *attributes* (`System.Diagnostics.CodeAnalysis.*`, `NotNullWhen`, `MaybeNull`, etc.) are emitted/recognized by the modern SDK even on net472; the compiler provides them when the target framework does not.
- NRT does **not** add runtime null checks — it is static analysis. Argument null-validation at boundaries is still done explicitly (the codebase uses `ArgumentNullException` guard clauses), and the `Instance` null-handling contract (ADR-0001) is a runtime concern NRT cannot enforce because the field is typed non-null (`null!`).

## API surface used in project
This is a *runtime/framework* target, not a library with a consumed API; the "surface" is the net472 BCL the mod compiles against (`System.*`, `System.Collections.Generic`, `System.Linq`, `System.Reflection`, `System.Diagnostics.CodeAnalysis`). All consumed BCL members are within the net472 surface.

## Version-specific notes
- 4.7.2 is in-place-updated by Windows and is highly compatible across the 4.x line; the relevant fact for this project is purely "matches the game runtime," not specific 4.7.2 features.
- `WarningLevel 9999` + `TreatWarningsAsErrors=true` apply on net472 the same as any TFM — a warning (including a nullable warning) fails the build.

## Deprecations and breaking changes from prior version
- Not applicable as an upgrade path: the TFM is pinned and must not change. There is no "prior version" migration to track because moving off net472 is forbidden by the runtime constraint, not a routine choice.

## Project conventions
- Never change the TFM; treat any tooling suggestion to multi-target or upgrade as a violation of the platform pin.
- Keep `Nullable=enable` and the zero-warning build; rely on NRT for compile-time null analysis but keep explicit boundary guards for runtime safety.
- Prefer modern C# *syntax* (it lowers to net472-compatible IL) but verify any feature that needs a runtime/BCL type is actually available before use.
- `GenerateDocumentationFile=true` is on; XML docs are required on the public surface.

## Known issues and workarounds
- A C# feature may compile under `LangVersion=latest` yet fail at game load if it depends on a BCL/runtime type net472 lacks. Workaround: confirm the required runtime type exists on net472 (or that the compiler synthesizes it) before adopting the feature; prefer features already used elsewhere in the codebase.
- Newer-BCL APIs are tempting from SDK 10 IntelliSense but are not present at runtime. Workaround: target only the net472 BCL surface; let `TreatWarningsAsErrors` and game-load testing catch overreach.
