---
responsibility:
  owns: project-vetted reference for Microsoft.NET.Test.Sdk 18.6.0 (test host role, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), adr/ (decisions), commands.yaml (commands)
---

# Microsoft.NET.Test.Sdk @ 18.6.0

## Canonical source
- Repo / release notes: https://github.com/microsoft/vstest/releases
- NuGet: https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.6.0
- Last verified: 2026-06-04

## Package wiring
- Added to `Source/WorkManager.Tests/WorkManager.Tests.csproj` as `<PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.6.0" />`.
- Role: the test host / MSBuild integration that makes the project runnable by `dotnet test` and IDE runners. Required as the host for `NUnit3TestAdapter`. No compile-time API.
- Test project targets `net472`.

## API surface used in project
- None (no compile-time API). Provides `dotnet test` host wiring and MSBuild targets.

## Version-specific notes
- 18.6.0 is the current release (2026-05-26).
- **net472 is supported**: VSTest/Test SDK continues to target **.NET Framework 4.6.2** for the .NET Framework track, which covers net472. Only end-of-life modern-.NET TFMs (e.g. net6.0) were dropped from the Core track.
- Remains backwards compatible with prior Microsoft.NET.Test.Sdk versions; no mandatory migration from VSTest to Microsoft Testing Platform (MTP) within 18.x. The default `dotnet test` flow stays VSTest.

## Deprecations and breaking changes from prior version (17.11.1 → 18.x)
Relevant to a net472 NUnit project using `dotnet test`:
- **Dropped end-of-life modern TFMs** (effective 17.14.0 and carried into 18.x): projects targeting `net6.0` (or other EOL .NET TFMs) must move to `net8`+ or pin Test SDK to 17.13.0. **No impact on net472** — the .NET Framework path is retained.
- The test platform itself moved to **net8/net9** minimum for its own runtime ("drop unsupported frameworks"), but it still produces a .NET Framework 4.6.2 host, so **net472 test projects remain supported by 18.x**.
- **VSTest vs MTP**: 18.x references MTP in infrastructure but does NOT force a migration; VSTest mode (default `dotnet test`) is preserved and backwards compatible. No config change required for this project.
- Minimum .NET **SDK** to build/run: a current SDK that includes the net8/net9 runtime for the test platform host. Building a net472 test project still works on a modern .NET SDK; no separate Framework-only toolchain is needed.

## Project conventions
- Pinned together with NUnit 4.6.1 (framework) and NUnit3TestAdapter 6.2.0 (adapter); upgrade the trio in lockstep.
- Run via `dotnet test` in default VSTest mode; do not enable MTP mode without an explicit stack decision.
- Keep the test project on net472 to match the mod's runtime; do not retarget the test project to net8 (it must load the same RimWorld/net472 assemblies under test).

## Known issues and workarounds
- If a future contributor adds a `net6.0` TFM to a test project, restore will conflict with the EOL-TFM drop — keep test projects on net472 (or net8+ only if ever decoupled from RimWorld assemblies).
