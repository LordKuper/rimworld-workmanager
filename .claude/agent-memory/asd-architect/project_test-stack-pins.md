---
name: test-stack-pins
description: Pinned test-stack versions and the hard constraints behind them (FluentAssertions license, net472 support)
metadata:
  type: project
---

Test stack for Work Manager (verified 2026-06-04, all on net472):
- FluentAssertions **7.2.2** — pinned to 7.x, never 8.x.
- NUnit **4.6.1** (framework only; assertions via FluentAssertions, not `Assert`).
- NUnit3TestAdapter **6.2.0**.
- Microsoft.NET.Test.Sdk **18.6.0**.
- Lib.Harmony **2.4.2** (compile-time only, `ExcludeAssets=runtime`).
- LordKuper.Common **1.6** (reference-only DLL, no NuGet version, tracks RW major line).

**Why:**
- FluentAssertions 8.x switched Apache-2.0 → commercial license; 8.x is forbidden. 7.2.2 is the latest Apache-2.0 release.
- Both WorkManager and WorkManager.Tests target net472. NUnit3TestAdapter 6.x and Test.Sdk 18.x "min .NET 8" applies only to the modern-.NET track; both ship a .NET Framework 4.6.2 build that covers net472. net472 IS supported in these versions.

**How to apply:** Reject any dependency bump of FluentAssertions to 8.x (license, not functionality). When reviewing adapter/test-sdk upgrades, confirm the .NET Framework 4.6.2 build still ships before assuming net472 breaks. Tech-reference docs live in design/architecture/tech-reference/.
