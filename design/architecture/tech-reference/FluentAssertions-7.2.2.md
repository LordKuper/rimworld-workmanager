---
responsibility:
  owns: project-vetted reference for FluentAssertions 7.2.2 (assertion APIs used, license pin rationale, project conventions)
  excludes: adr rationale, code, full stack overview, build commands
  delegates_to: stack.html (overview), adr/ (decisions), commands.yaml (commands)
---

# FluentAssertions @ 7.2.2

## Canonical source
- Official docs: https://fluentassertions.com/introduction
- NuGet: https://www.nuget.org/packages/FluentAssertions/7.2.2
- Last verified: 2026-06-04

## Package wiring
- Added to `Source/WorkManager.Tests/WorkManager.Tests.csproj` as `<PackageReference Include="FluentAssertions" Version="7.2.2" />` on the first real test.
- **Pin to 7.x — never float to 8.x.** Use an exact/locked pin; do not use a floating range that could resolve 8.x.

## License pin rationale (critical)
- FluentAssertions **7.x is Apache-2.0** (free for any use, including commercial).
- FluentAssertions **8.x switched to a commercial license** (paid for commercial use). It is **forbidden** for this project.
- Therefore the project pins **7.2.2** (latest 7.x as of verification, released 2026-03-16). The current overall latest is 8.10.0 (commercial) — do NOT adopt it. Any dependency-update tooling that bumps to 8.x must be rejected.

## API surface used in project
FluentAssertions is the **assertion library** (replaces NUnit `Assert`). Entry point is the `.Should()` extension.
- `value.Should().Be(...)`, `.NotBe(...)`, `.BeTrue()`, `.BeFalse()`, `.BeNull()`, `.NotBeNull()` — scalar assertions.
- `collection.Should().BeEmpty()`, `.NotBeEmpty()`, `.HaveCount(n)`, `.Contain(...)`, `.BeEquivalentTo(...)` — collection assertions.
- `action.Should().Throw<TException>()` / `.NotThrow()` — exception assertions (replacing `Assert.Throws`).
- `because`/reason message arguments on assertions for failure context.

(Exact assertions in use grow with the test suite; the placeholder `WorkShiftTests.Placeholder` predates the first real assertion and still uses `Assert.Pass`.)

## Version-specific notes
- 7.2.2 is the latest 7.x patch and the highest version compatible with the Apache-2.0 license requirement.
- 7.x targets and APIs are compatible with NUnit 4.x + net472. The `.Should()` fluent surface used here is stable across 7.x.

## Deprecations and breaking changes from prior version
- Within 7.x: no breaking changes affecting this project.
- 7.x → 8.x: **license change (Apache-2.0 → commercial)** is the blocking "breaking change" for this project; there is no migration path because 8.x is forbidden on license grounds. Stay on 7.x.

## Project conventions (from custom coding rules)
- Assertions MUST use FluentAssertions `.Should()`; do NOT use NUnit `Assert.*` / `Assert.That` in new or rewritten tests.
- Use a global `<Using Include="FluentAssertions" />` in the test csproj instead of per-file `using FluentAssertions;`.
- The FluentAssertions 7.x package is added to `WorkManager.Tests.csproj` on the first real test (the existing placeholder's `Assert.Pass` is legacy-only).
- Keep the 7.x pin permanent; treat an 8.x bump as a license violation, not a routine update.

## Known issues and workarounds
- Automated dependency bots will propose 8.x (the overall latest). Workaround: pin exactly to 7.2.2 and reject 8.x bumps in review; the rationale is license, not functionality.
