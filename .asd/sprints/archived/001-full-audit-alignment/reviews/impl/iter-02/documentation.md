[REVIEW-impl-documentation]: CONCERNS

# Review — documentation

- **Phase**: impl-review
- **Iteration**: 2

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | design/architecture/adr/adr-0001-instance-null-handling-contract.html#decision (line 148) vs Source/WorkManager/WorkManagerGameComponent.cs:140 | Documentation-actuality divergence. The as-built contract is `IsInitialized => Current.Game != null && Instance is not null` (cs:140; XML-doc cs:129-140 correctly describes the two-condition check incl. the stale-Instance-after-quit case). ADR-0001's Decision still defines the helper as "Add one static helper, `WorkManagerGameComponent.IsInitialized` (returns `Instance is not null`)" — a single-condition contract. The ADR's Context/Alternatives further premise that "null genuinely means 'no game'" and never mention the stale-reference case (Instance non-null after quit-to-menu) that the implemented `Current.Game != null` guard now handles. The ADR — SSoT for this decision's rationale — is therefore stale against the code it documents. | Update ADR-0001 Decision to state `IsInitialized` returns `Current.Game != null && Instance is not null`, and add to Context/Consequences the stale-Instance-after-unload case (Instance retains its last value post-quit) that motivates the `Current.Game` check. Owned by the Architect in design-promote (reviewer must not edit persistent design/). |

## Verdict
CONCERNS: 1

## Next action
Architect to update persistent ADR-0001 (Decision + Context) so the documented contract matches the as-built `IsInitialized` two-condition guard, then re-run impl-review. All other persistent docs verified actual: README badges 1.1-1.6 + scheduling/Harmony/Common deps (AC-14/15) correct; stack.html matches WorkManager.csproj / WorkManager.Tests.csproj exactly (Harmony 2.4.2, NUnit 4.6.1, NUnit3TestAdapter 6.2.0, Test.Sdk 18.6.0, FluentAssertions 7.2.2, net472, LangVersion latest) (AC-16); concept.html accurate vs About.xml; RimWorld-1.6.md + dotnet-framework-4.7.2.md present and version-accurate, every stack tech has a tech-reference doc (AC-7/8); provenance flags correct (stack/concept reverse-engineered with source, ADRs original, badges suppressed for original); traceability AC-11/AC-12 ↔ ADR-0001 ↔ guarded UI entry points intact.

## Escalations (optional)
- none
