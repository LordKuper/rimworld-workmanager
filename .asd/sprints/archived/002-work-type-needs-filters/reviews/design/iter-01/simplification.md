[REVIEW-design-simplification]: CONCERNS

# Review — simplification

- **Phase**: design-review
- **Iteration**: 1
- **Severity floor**: low (all findings count)
- **Inputs**: `design/prd.html`, `design/adr.html` (ADR-0006, ADR-0007, ADR-0008, Complication Approval CA-1…CA-5); context `sprint.md`, `design-principles.md`, `review-policy.md`, `custom-design-rules.md`, `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs`, `Source/WorkManager/Settings/Settings_WorkTypes.cs`, `Source/WorkManager/Cache/PawnCache.cs`, `Source/WorkManager/WorkPriorityUpdater.cs`; Common `DefCache.cs`, `PawnCapacityLimit.cs` (read-only reference).

## Findings

| # | Severity | Location | Description | Suggested fix |
|---|---|---|---|---|
| 1 | medium | adr.html, ADR-0006 Decision item 7 "Evaluation" (instance `IsNeedBlocked(Pawn)` and static `IsNeedBlocked(bool?, …)`) | **Category: simplify.** Not a checklist hit; this is a complexity-vs-value issue. The design checks the same needs-state gate twice. The instance method returns `false` when `FilterNeeds != true`, before it calls the static seam. The static seam then checks `filterNeeds` again ("Returns false when the state is not On"). In production, the static seam only ever gets `filterNeeds == true`, so its Off/Inherit branch never runs. The AC-19 "Off never blocks" unit test (required by AC-48) would therefore cover a branch production never takes, while the gate production actually uses (the instance early return) is not unit-tested. The two copies of the predicate have to stay in sync by hand. The only reason given for the early return is "the common path allocates no closure". That saving is negligible: the verdict is memoised per pawn and work type per update (`PawnCache.IsAllowedWorker`), so it amounts to at most a few hundred small closures per update, and the fail-safe caller runs rarely. | Keep one gate, in the static seam. Make the instance method a one-line call, `IsNeedBlocked(FilterNeeds, NeedLimits, def => pawn.needs?.TryGetNeed(def)?.CurLevelPercentage)`, with no early return, and remove the "allocates no closure" rationale from the ADR. The CA-3 signatures stay as approved, so this does not contradict a user-approved item and needs no escalation. |

### Checked against the over-engineering checklist, no change needed (keep-as-is)

These are recorded for traceability. They are not findings and do not count toward the verdict.

- **CA-1 `NeedLimit : DefCache<NeedDef>, IExposable`** (user-approved). It is a real persisted entity, not an abstraction. It mirrors Common's `PawnCapacityLimit`, and the approved alternatives (a dictionary, or reusing `PawnCapacityLimit`) do not work before defs load or use the wrong def type. Keep-as-is.
- **CA-2 `bool? FilterNeeds` + `List<NeedLimit>? NeedLimits` on the rule** (user-approved). There is no sub-object layer, and the ADR rejects the extra `NeedsFilter` layer. The nullable list type leads to `?? []` in `Combine` and a nullable parameter on the static seam, even though `ValidateNeedsFilter` already normalises the list on `LoadingVars`. That redundancy is small and matches the sibling convention (`AllowedWorkers?` / `DedicatedWorkerSettings?` with `!`), so I do not raise it as "defensive code for impossible-by-contract case". Changing it would contradict the approved CA-2 type for no material gain. Keep-as-is.
- **CA-3 `Func<NeedDef, float?>` delegate seam** (user-approved). It has one production implementation (the `pawn.needs?` lambda), but the unit tests are a genuine second consumer (AC-13…AC-19, and `Pawn` cannot be built in net472), and ADR-0007 reuses the instance method. It is not "abstraction with no second use case". Keep-as-is (see finding 1 for the gate duplication only).
- **CA-4 `internal ValidateNeedsFilter()` / `internal static GetAddableNeeds(...)`** (user-approved). Both contain real logic: normalising nulls, removing duplicates, clamping and the default-rule Off normalisation for the first; filtering and a two-key sort for the second. The load-time call is required by design-principles §6 and AC-10. Keep-as-is.
- **CA-5 test-only `FakeDefProvider : IDefProvider`** (user-approved). It is a test double of Common's public seam, not a mock of a mock and not a new production interface. Keep-as-is.
- **ADR-0007 fail-safe change.** It is a single added condition with no new helper, no memoisation and no extraction. The alternatives that would add state or a seam were correctly rejected. Checked `WorkPriorityUpdater.cs:821–825`: the schedule-aware fail-safe draws from `allowedWorkers`, which already includes the needs check, so the one-site change is sufficient. Keep-as-is.
- **ADR-0008 UI.** It uses only Common's public helpers and vanilla widgets, with no local copy of `DoToggleableSectionHeader` and no reflection. Unresolved entries reuse the same row layout. `_needsSectionContentHeight` follows the existing `_*SectionContentHeight` pattern (`Settings_WorkTypes.cs:106–116`). Keep-as-is.
- **PRD.** Every AC traces to a user decision (D1–D9) or to a sprint.md acceptance line. There are no speculative requirements, and the non-goals explicitly exclude hysteresis, per-pawn exemption, range thresholds and slider hints. Keep-as-is.

## Verdict
CONCERNS: 1 (medium: 1)

## Next action
The Architect autofixes finding 1 in `adr.html` ADR-0006 item 7: remove the instance-level early return and its closure-allocation rationale, and make the static seam the only needs-state gate. Consequence bullet 3 ("only while the state is On") should be reworded to match. No escalation is needed.

## Escalations (optional)
None for this iteration.

**Cross-reviewer guard.** Each of the following fixes, if another reviewer proposes it, would add complexity and requires Complication Approval (What / Why / Justification / Alternatives) before it is applied:
- Making `NeedLimit.DefaultThreshold` (50%) or the 0.01 slider step a mod setting or Def field, for example by citing the "data-driven over hardcoded" rule in `custom-design-rules.md`. That would be a premature config flag: no caller chooses a non-default value, and D7 and D8 fix both values as UI defaults, not balance tunables.
- Memoising the need-blocked verdict on `PawnCache` for the fail-safe, or extracting fail-safe candidate selection into a static helper. Both were rejected in ADR-0007 under the Simplicity Default.
- Adding a `NeedsFilter` sub-object, a local copy of Common's internal UI helpers, or reflection into Common.
- Adding hysteresis or latching state. This is a non-goal by user decision.
