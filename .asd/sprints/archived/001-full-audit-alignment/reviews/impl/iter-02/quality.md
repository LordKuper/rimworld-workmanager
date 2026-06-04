---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-quality]: APPROVE

# Review — quality

- **Phase**: impl-review
- **Iteration**: 2

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| — | — | — | no findings at or above medium floor | — |

## Verdict
APPROVE

## Next action
Reviewer done. No quality findings at or above the iteration-2 floor (medium). Other DoD reviewers must also APPROVE this iteration for the phase to advance.

## Notes (informational, below floor — not blocking)

The following were verified and found correct; recorded only to document the scan, not as findings:

- **IsInitialized contract correctness / behavior preservation** — `Current.Game != null && Instance is not null` (WorkManagerGameComponent.cs:140). When a game is loaded both operands are non-null, so the property returns `true` exactly as the prior `Instance is not null`-only contract did — behavior preserved on the live-game path. The added `Current.Game != null` term only flips the post-quit stale-reference case from `true` to `false`, which is the intended fix. XML-doc contract (lines 115-139) matches the implementation.
- **Game-scoped direct-deref invariant holds** — PawnCache.cs, WorkPriorityUpdater.cs, ScheduleUpdater.cs dereference `Instance` directly without the guard. All run only on a live `Map` (MapComponent ticks / cache updates); a `Map` cannot exist without a `Game`, and a `Game` cannot exist without this component, so `Instance` is non-null on those paths. Invariant consistent with the documented contract.
- **Harmony early-outs preserve vanilla UI** — every UI-scoped prefix/postfix (PawnColumnWorkerWorkPriorityPatch, MainTabWindowWorkPatch, WidgetsWorkPatch, WorkTabPatch, AutoWorkPriorities, AutoWorkSchedule) guards with `if (!IsInitialized) return;` before any `Instance` use. The guards only skip the mod's added drawing/interaction; they never suppress or mutate vanilla output. `WorkTabPatch.DoHeaderPrefix` adjusts `rect` only after the guard passes, so a game-less path leaves vanilla layout untouched.
- **Localization arg/placeholder correctness** — English and Russian keyed XML have matching key sets and matching `{0}` placeholders (`WorkTypeRuleHeader`, `WorkTypeRuleHeaderTooltip`, `Settings_Schedule_WorkShiftLabel`). `WorkShiftLabel` uses a single `{0}` and the two call sites in Settings_Schedules.cs (lines 151, 209) pass exactly one argument (`i + 1`). No arity mismatch.
- **Test reflection robustness** — StateIsolationTestBase uses `typeof(...)` for type resolution (not string-based `Type.GetType`) and fail-loud `?? throw new InvalidOperationException(...)` for the `Instance` property, its setter, and static fields. Snapshot/restore is per-test `[SetUp]`/`[TearDown]` on a `[NonParallelizable]` base, matching the isolation rule.
- **AssemblyResolver attribute lookup** — `RimWorldAssemblyResolverFixture.GetRimWorldManagedDir` uses `GetCustomAttribute<AssemblyMetadataAttribute>()` (singular). The generated AssemblyInfo emits exactly one `[AssemblyMetadata]` (RimWorldManagedDir), so no `AmbiguousMatchException` risk for the current build.
- **WidgetsWorkPatch.DrawWorkBoxForPostfix** (WidgetsWorkPatch.cs:27-43) does not null-guard `p`/`wType` before `GetPawnWorkTypeEnabled` (which throws on null), unlike the prefix. In practice RimWorld always supplies non-null arguments to `DrawWorkBoxFor`, so this is defensive-code-for-impossible-case territory — below the medium floor; not raised as a finding.
