---
responsibility:
  owns: project-vetted reference for NUnit3TestAdapter 6.2.0 (runner role, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), adr/ (decisions), commands.yaml (commands)
---

# NUnit3TestAdapter @ 6.2.0

## Canonical source
- Official docs: https://docs.nunit.org/articles/vs-test-adapter/Index.html
- Release notes: https://docs.nunit.org/articles/vs-test-adapter/AdapterV4-Release-Notes.html
- NuGet: https://www.nuget.org/packages/NUnit3TestAdapter/6.2.0
- Last verified: 2026-06-04

## Package wiring
- Added to `Source/WorkManager.Tests/WorkManager.Tests.csproj` as `<PackageReference Include="NUnit3TestAdapter" Version="6.2.0" />`.
- Role: the VSTest/MTP adapter that discovers and runs NUnit tests under `dotnet test` and IDE test runners. No code API; it is build/runtime infrastructure.
- Test project targets `net472`. The adapter's `.NET Framework 4.6.2` build covers net472 (see notes).

## API surface used in project
- None (no compile-time API). Configured via project reference and, if needed, `.runsettings` / `RunConfiguration`.

## Version-specific notes
- 6.2.0 is the current release (2026-03-21).
- **Framework support**: 6.2.0 ships two builds — `.NET 8.0` and `.NET Framework 4.6.2`. The "minimum .NET 8.0" requirement applies ONLY to the modern .NET (Core) track; the net462 build still supports .NET Framework, so **net472 is supported**.
- 6.x dependencies pull in Microsoft Testing Platform packages: `Microsoft.Testing.Extensions.VSTestBridge (>= 2.1.0)` and `Microsoft.Testing.Platform.MSBuild (>= 2.1.0)` on both builds (net8 additionally pulls `Microsoft.Extensions.DependencyModel >= 8.0.2`). This means 6.x co-exists with MTP but does not force MTP mode for VSTest `dotnet test`.

## Deprecations and breaking changes from prior version (4.6.0 → 6.x)
Relevant to a net472 NUnit project using `dotnet test`:
- **6.0.0** (2025-12-06): minimum modern-.NET raised to **.NET 8.0** (netcore 3.1 dropped). Requires **Microsoft Testing Platform 2.0.0**. Introduces `AssemblyLoadContext` for net8+. **No impact on net472** — the .NET Framework build path is unaffected by the netcoreapp3.1 drop.
- **6.1.0** (2026-01-07): `UseDefaultAssemblyLoadContext` now defaults to enabled, which can break ReSharper/Rider versions prior to 2025.3.1. Affects IDE test runs on modern .NET; mitigate by updating the IDE or setting `UseDefaultAssemblyLoadContext=false` in `.runsettings` if needed.
- **6.2.0** (2026-03-21): no breaking changes.
- **5.0.0** (2025-02-07): added Microsoft Testing Platform support; no direct breaking changes when MTP is not enabled.
- There were no major-version 5/6 changes that drop .NET Framework or that alter `.runsettings` semantics for this project's net472 + VSTest `dotnet test` flow.

## Project conventions
- Pinned alongside NUnit 4.6.1 (framework) and Microsoft.NET.Test.Sdk 18.6.0 (host) — the three move together.
- Run tests via `dotnet test` (VSTest mode). Do not opt into MTP mode unless explicitly decided (would be a stack change requiring approval).
- No `.runsettings` is required for the default net472 flow; add one only to set `UseDefaultAssemblyLoadContext` or parallelism options if an IDE incompatibility surfaces.

## Known issues and workarounds
- IDE (ReSharper/Rider < 2025.3.1) discovery failures after 6.1.0 due to the `UseDefaultAssemblyLoadContext` default. Workaround: update the IDE, or set `UseDefaultAssemblyLoadContext=false` in `.runsettings`.
