---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-impl-simplification]: CONCERNS

# Review — simplification

- **Phase**: impl-review
- **Iteration**: 1

Scope: the needs-filter change set under `Source/`. That is `NeedLimit.cs`, `WorkTypeAssignmentRule.cs`, `Settings_WorkTypes.cs` (`GetAddableNeeds`, `DoRuleNeeds`), `Resources.cs` (8 strings, `GetFilterNeedsTooltip`), `WorkPriorityUpdater.cs` (fail-safe condition), `FakeDefProvider.cs`, `NeedsFilterTests.cs` and `WorkManager.Tests.csproj` (`CopyRimWorldTestDeps`). I checked it against the over-engineering checklist in `review-policy.md`, `design-principles.md` §2/§10, `code-style.md` and `custom-coding-rules.md`, and against the approved Complication Approval items CA-1…CA-5 in ADR-0006.

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | critical | `Source/WorkManager/Settings/Settings_WorkTypes.cs:292`; `Source/WorkManager.Tests/NeedsFilterTests.cs:628-637` | Checklist: **defensive code for impossible-by-contract case**. `GetAddableNeeds` guards each element with `limit?.DefName`, but `existing` is typed `IReadOnlyCollection<NeedLimit>` (non-nullable elements). The only caller passes `rule.NeedLimits`. That list has no null entries: `ValidateNeedsFilter` drops them on load and before save, new rules start with `[]`, `Combine` copies an already-validated list, and the UI only adds `new NeedLimit(def)`. The test `GetAddableNeeds_NullExistingEntry_Ignored` has to force the case with `null!`, so it only pins the guard in place. Category: **simplify**. Does not contradict CA-4: CA-4 approves the method, not the null-element guard. | Change to `if (limit.DefName != null) present.Add(limit.DefName);`. The `DefName` null check stays, because `DefName` is `string?` and the build treats warnings as errors. Delete `GetAddableNeeds_NullExistingEntry_Ignored`. |
| 2 | critical | `Source/WorkManager/Settings/Settings_WorkTypes.cs:355` | Checklist: **defensive code for impossible-by-contract case**. `removeIndex < limits.Count` can never be false. `removeIndex` is only set to a loop index `i < limits.Count` in the same draw pass, and nothing shrinks `limits` between the loop and the check. The FloatMenu add callback runs later and can only grow the list. Category: **simplify**. Not covered by any CA item. | Change to `if (removeIndex >= 0) limits.RemoveAt(removeIndex);`. |
| 3 | low | `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs:418` | Complexity vs value, not a checklist item. The summary line `limit.Def != null ? limit.Label : limit.DefName` repeats a fallback that `DefCache.Label` already has: it returns `Def?.GetLabel() ?? DefName` (tech-reference, LordKuper.Common `DefCache`). Both branches return the same text. (The UI row at `Settings_WorkTypes.cs:345-347` is different and needs its branch, because unresolved entries use `NeedUnavailableLabel` there.) Category: **simplify**. | Use `$"{limit.Label}: {limit.Threshold.ToStringPercent()}"`. |

## Assessed and kept as-is (no finding)

- **CA-1 `NeedLimit`**: minimal, mirrors `PawnCapacityLimit`. `DefaultThreshold` is used both as the field initialiser and as the Scribe default. The `(string defName)` constructor has no production caller; only tests use it. It is a one-line pass-through to the base constructor, listed in the approved plan. Tests could use `new NeedLimit(new NeedDef { defName = … })` instead, but that is wordier and removes no complexity. **keep-as-is**.
- **CA-2 fields `FilterNeeds` / `NeedLimits`**: same shape as the sibling tri-state fields. The nullable list matches Scribe's behaviour with older settings. **keep-as-is**.
- **CA-3 static `IsNeedBlocked` seam + instance wrapper**: the `Func` lookup has two consumers (the production `pawn.needs` lambda and tests), and the wrapper has two call sites (`IsAllowedWorker` and the fail-safe). The `limits.Count == 0` early return duplicates what the empty loop already does, but it is harmless and reads as the documented contract. **keep-as-is**.
- **CA-4 `ValidateNeedsFilter`**: the null-entry, empty-defName and duplicate handling is real boundary validation of persisted data, not defensive code. **keep-as-is**.
- **CA-5 `FakeDefProvider`**: implements `IDefProvider` exactly. `WorkTypeDefsInPriorityOrder` returning `[]` and the null/empty guard in `GetNamedSilentFail` are required by the interface contract. It is a single fake with no mock-of-mock. **keep-as-is**.
- **`GetFilterNeedsTooltip`**: the fourth copy of the existing `Get*Tooltip(bool triState)` pattern in `Resources.cs`. It matches the surrounding code. Pulling out a shared helper now would be refactoring adjacent code outside the task's scope. **keep-as-is**.
- **Default-rule checkbox bridging** (`var value = rule.FilterNeeds == true; … rule.FilterNeeds = value;`): needed because the default rule is two-state over a `bool?` field. **keep-as-is**.
- **Fail-safe change and comment** (`WorkPriorityUpdater.cs:284-287`): one extra condition. The comment explains *why*, not *what*. **keep-as-is**.
- **Test csproj `CopyRimWorldTestDeps` target**: **keep-as-is**. The problem is real: NUnit loads test types during discovery, before the global `[SetUpFixture]` resolver runs. Types whose signatures reference `Verse` types need `Assembly-CSharp` next to the test assembly. That covers `FakeDefProvider` and `NeedsFilterTests.RegisterNeed`, which returns `NeedDef`. The only simpler option is removing `Private=False` from the `Assembly-CSharp` reference. That would also let MSBuild resolve and copy `Assembly-CSharp`'s dependencies (the Unity modules) from `$(RimWorldManagedDir)` into the output, which is a wider and less controlled change than the current explicit single-file `Copy`. The target is 5 lines, adds no dependency or abstraction, and its comment gives the reason. It is not a checklist item.

## Verdict
CONCERNS: 3 (2 critical checklist hits, 1 low), all fixable by the creator with no escalation.

## Next action
Route to `impl` fix mode. The frontend dev applies findings 1–3 and the test engineer deletes the obsolete test in finding 1. All three fixes remove code and add none, so no Complication Approval is needed. None of the findings contradicts CA-1…CA-5. Cross-reviewer guard: if another reviewer's fix in this iteration proposes a new abstraction, interface, helper layer, config flag or dependency (for example a shared tooltip helper, a needs-provider interface, or a wrapper around `pawn.needs`), escalate it to the user in Complication Approval format. Do not autofix it.

## Escalations (optional)
- none
