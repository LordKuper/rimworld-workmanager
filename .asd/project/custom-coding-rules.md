---
responsibility:
  owns: project-owner custom rules read during impl and impl-review phases
  excludes: universal rules, design-only rules
  delegates_to: custom-common-rules.md (all phases), custom-design-rules.md (design/design-review)
---

# Custom Coding Rules

Inherited from the `LordKuper.Common` parent library and adapted to WorkManager's actual setup.

## Nullability

- Source uses `<Nullable>enable</Nullable>`. Keep nullable reference annotations (`string?`, `T?`, `[return:]`) consistent with this setting.
- Do NOT disable the nullable context.

## Zero warnings

- Source builds with `TreatWarningsAsErrors=true` and `WarningLevel 9999` in both Debug and Release. Code MUST compile warning-clean. A warning fails the build.

## Build / lint flow

- Before `build`: run `jb-cleanup` (applies the solution code-cleanup profile).
- After `lint`: run `jb-inspect`, then verify `TestResults/jb-inspect.sarif` has no `error` or `warning` severity entries.
- Commands defined in `.asd/project/commands.yaml` (`jb-cleanup`, `jb-inspect`).

## Analyzer / linter suppressions

- Suppress findings only as a last resort — fix the real issue first.
- Prefer attribute-based suppression (`[SuppressMessage]`, `[UsedImplicitly]`, `[Pure]`) over comment pragmas (`#pragma warning disable`, `// ReSharper disable`). Use comments only when no attribute applies.
- Every suppression MUST carry a real reason saying *why*. "false positive" / "by design" alone is not enough.

## Self-contained code — no design-doc references

- The codebase (Source AND Tests) MUST be self-sufficient without the ASD design docs. Code and comments MUST NOT reference or quote ASD artifacts: ADR, PRD, acceptance criteria (`AC-N`), improvement items (`IMP-N`), `Task N`, sprint ids, or rule-doc filenames (`custom-*-rules.md`).
- Explain the *why* directly in the comment instead of citing a doc.
- The only exception: forward-looking `TODO`/`FIXME` comments MAY reference a sprint/issue for future work.

## Logging

- Use the project `Logger` (`Source/WorkManager/Logger.cs`). Actionable, gated, no spam.

## Testing (NUnit + FluentAssertions)

- Test framework is **NUnit 4.x** (`[Test]`, `[TestCase]`, `[TestFixture]`, `[SetUp]`/`[TearDown]`, `[SetUpFixture]`/`[OneTimeSetUp]`); runner is `NUnit3TestAdapter`; host is `Microsoft.NET.Test.Sdk`. Assertions are **FluentAssertions 7.x** (`.Should()`) — do NOT use NUnit `Assert.*` / `Assert.That`. FluentAssertions is pinned to 7.x (Apache-2.0); never float to 8.x (commercial license).
- Use global `<Using Include="NUnit.Framework" />` and `<Using Include="FluentAssertions" />` rather than per-file `using` directives.
- The existing `WorkShiftTests.Placeholder` uses `Assert.Pass` only because no real assertions exist yet; new and rewritten tests MUST use FluentAssertions, and the FluentAssertions 7.x package MUST be added to `WorkManager.Tests.csproj` on first real test.
- **Static state isolation** — tests mutating global/cached/static state MUST save/restore via per-test `[SetUp]` (snapshot) / `[TearDown]` (restore) on a shared base class. Use per-test `[SetUp]`/`[TearDown]` (not per-class `[OneTimeSetUp]`) so each test gets true isolation. NUnit runs non-parallel by default; mark static-touching classes `[NonParallelizable]` and never add `[assembly: Parallelizable]`.
- If RimWorld-typed test types are introduced, the RimWorld `AppDomain.AssemblyResolve` handler MUST be registered before any such type loads — house it in a namespace-less (global) `[SetUpFixture]` with `[OneTimeSetUp]`.
- Do not depend on test execution order.
- RimWorld APIs requiring live game context must be abstracted or guarded; don't call them directly in unit tests without isolation.
