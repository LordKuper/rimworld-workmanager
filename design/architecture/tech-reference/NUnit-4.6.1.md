---
responsibility:
  owns: project-vetted reference for NUnit 4.6.1 (attributes/APIs used, version specifics, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), adr/ (decisions), commands.yaml (commands)
---

# NUnit @ 4.6.1

## Canonical source
- Official docs: https://docs.nunit.org/
- NuGet: https://www.nuget.org/packages/NUnit/4.6.1
- Last verified: 2026-09-29 (Known issues: discovery-time type loading). Earlier sections: 2026-06-04.

## Package wiring
- Added to `Source/WorkManager.Tests/WorkManager.Tests.csproj` as `<PackageReference Include="NUnit" Version="4.6.1" />`.
- Test project targets `net472` (matches the mod). Pair with `NUnit3TestAdapter` (runner) and `Microsoft.NET.Test.Sdk` (host) for `dotnet test`.

## API surface used in project
NUnit is the **test framework only** (attributes + lifecycle). Assertions are NOT done with NUnit — see Project conventions.

- `[TestFixture]`: marks a test class.
- `[Test]`: a parameterless test.
- `[TestCase(...)]`: parameterized inline cases.
- `[SetUp]` / `[TearDown]`: per-test lifecycle — used for static-state snapshot/restore isolation.
- `[SetUpFixture]` + `[OneTimeSetUp]` / `[OneTimeTearDown]`: assembly/namespace-level one-time setup — reserved for the RimWorld `AppDomain.AssemblyResolve` registration in a namespace-less (global) fixture.
- `[NonParallelizable]`: applied to classes that touch global/cached/static state.
- `Assert.Pass(...)`: present only in the existing `WorkShiftTests.Placeholder` because no real assertions exist yet; new/rewritten tests use FluentAssertions, not `Assert`.

## Version-specific notes
- 4.6.1 is the current 4.6.x patch (released 2026-05-19). NUnit 4.x is the active major line for `dotnet test` against `NUnit3TestAdapter`.
- 4.x constraint-model and attribute surface used here are stable across the 4.x line; the 4.2 → 4.6 bump is patch/minor-level for this project's usage (attributes + lifecycle only).
- Classic `Assert.That` / constraint syntax exists in 4.x but is intentionally not used (FluentAssertions is the assertion library).

## Deprecations and breaking changes from prior version
- 4.2.2 → 4.6.1 is within the same major (4.x); no breaking changes affect the attributes/lifecycle this project uses. NUnit 3 → 4 (already adopted) had moved `Assert` overloads to the constraint model and split classic assertions, but the project does not rely on NUnit assertions, so that surface is moot here.

## Project conventions (from custom coding rules)
- Framework is **NUnit 4.x**; assertions are **FluentAssertions 7.x** (`.Should()`). Do NOT use `Assert.*` / `Assert.That` for new tests.
- Use a global `<Using Include="NUnit.Framework" />` in the test csproj instead of per-file `using NUnit.Framework;`.
- **Static-state isolation**: tests mutating global/cached/static state save/restore via per-test `[SetUp]` (snapshot) / `[TearDown]` (restore) on a shared base class. Use per-test `[SetUp]`/`[TearDown]`, NOT per-class `[OneTimeSetUp]`, so each test gets true isolation.
- NUnit runs non-parallel by default; mark static-touching classes `[NonParallelizable]` and never add `[assembly: Parallelizable]`.
- If RimWorld-typed test types are introduced, register the RimWorld `AppDomain.AssemblyResolve` handler in a namespace-less (global) `[SetUpFixture]` with `[OneTimeSetUp]`. That covers execution only; discovery-time loading needs `Assembly-CSharp.dll` in the test output (see Known issues below).
- Do not depend on test execution order.
- The placeholder `Assert.Pass` is legacy-only; first real test must add the FluentAssertions 7.x package and use `.Should()`.

## Known issues and workarounds
- **RimWorld types in tests: discovery vs execution (single home for this fact).** NUnit enumerates every type in the test assembly (`Assembly.GetTypes()`, attribute reads) during *discovery*, before any `[OneTimeSetUp]` runs, so the global `[SetUpFixture]` `AssemblyResolve` handler is not yet registered. Therefore:
  - A test-assembly type whose **signature** references a RimWorld type (base type, field/method signature, generic constraint — e.g. the test-only `FakeDefProvider : IDefProvider` with `where T : Def`) fails to load at discovery unless `Assembly-CSharp.dll` sits next to the test assembly; the whole assembly is then not runnable (zero tests discovered or mass failure).
  - Workaround in use: the test csproj's `CopyRimWorldTestDeps` target (`AfterTargets="Build"`) copies `$(RimWorldManagedDir)\Assembly-CSharp.dll` into the test output (`<Reference>` stays `Private=False`). The output `bin/` is gitignored; the DLL is never committed or shipped. Decision and scope: ADR-0002 amendment 2026-09-29 (`design/architecture/adr/adr-0002-test-isolation-infrastructure.html#amendment-2026-09-29`). LordKuper.Common's test project uses the same target (Common ADR-0006), with a wider file set.
  - The `AssemblyResolve` handler is still required: it supplies the Unity modules and other RimWorld-dir assemblies while tests *execute* (types referenced only inside method bodies load lazily and hit the resolver).
  - If a future discovery failure names another RimWorld-dir assembly (Unity module, `netstandard` 2.1), add it to `CopyRimWorldTestDeps` rather than restructuring tests.
- Parallelism + static state is a correctness hazard; the `[NonParallelizable]` + per-test SetUp/TearDown convention is the mitigation.
