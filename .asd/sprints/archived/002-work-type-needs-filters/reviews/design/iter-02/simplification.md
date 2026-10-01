---
responsibility:
  owns: single reviewer verdict for one iteration
  excludes: other reviewers, other iterations, fixes
  delegates_to: creator agent (fixes), sibling review files (other reviewers)
---

[REVIEW-design-simplification]: APPROVE

# Review — simplification

- **Phase**: design-review
- **Iteration**: 2

## Findings

| # | Severity | Category | Location | Description | Suggested fix |
|---|---|---|---|---|---|
| — | — | — | — | no findings | — |

## Verdict
APPROVE

## Next action
Reviewer done. No creator action is required from the simplification lane.

I checked every item on the over-engineering checklist (`review-policy.md`) against `prd.html` and `adr.html` (ADR-0006, ADR-0007, ADR-0008), and verified the claims against the source:

- **New types.** The only new production type is `NeedLimit` (CA-1). It uses the existing `DefCache<T>` base in the same way as Common's `PawnCapacityLimit`, and it drops the range, buffer and step machinery that the scope does not need. It adds no interface, generic, factory, plugin point or inheritance level, and no `NeedsFilter` sub-object; the ADR explicitly rejects that sub-object.
- **Evaluation seam.** The static `IsNeedBlocked` plus its one-line instance adapter (CA-3) has two production callers: `IsAllowedWorker` and the day-level fail-safe in `WorkPriorityUpdater.cs:281-285`. It also has the test suite as a consumer, so it is not an abstraction without a second use.
- **Fail-safe change.** ADR-0007 changes one condition. It correctly leaves `TryAssignDedicatedWorkersBySchedule` (`WorkPriorityUpdater.cs:821-825`) alone, because that fallback already draws from gated `allowedWorkers`. The ADR rejects both the memoisation and the candidate-selection extraction on simplicity grounds.
- **UI (ADR-0008).** The Needs section uses only public Common helpers and vanilla widgets. It adds no local copy of internal Common helpers and no reflection. `_needsSectionContentHeight` and `GetFilterNeedsTooltip(bool)` follow existing patterns (`Settings_WorkTypes.cs:106-116`, `Resources.cs:565`).
- **Config.** No new config flag. `FilterNeeds` is a user-facing rule setting, and the user chooses its non-default values.
- **Validation.** `ValidateNeedsFilter` runs on load and on save. It is backed by real inputs: `Scribe_Collections` sets the list to null for older settings, and hand-edited XML can contain duplicates or out-of-range values. This is not defensive code for a case the contract rules out.
- **Test double.** `FakeDefProvider` (CA-5) is a hand-written fake of Common's existing `IDefProvider` seam. It adds no new interface and involves no mock of a mock.
- **Contingencies.** The `FloatMenuOption.tooltip` fallback and the `NeedDef` construction fallback are alternatives that implementation will settle. They are not dead code or speculative layers.

## Escalations (optional)
- None. No finding contradicts the approved Complication Approval items CA-1…CA-5. Each approved item is minimal for the problem it solves, and I found no simpler alternative that would hold up.
