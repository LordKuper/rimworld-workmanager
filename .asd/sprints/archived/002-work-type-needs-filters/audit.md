---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

## Scope reference
[sprint.md](./sprint.md)

## Touched areas

Docs side (by asd-ba) first, then code side (by asd-architect).

- `design/product/concept.html`: pillars "Configurable & overridable" and target segment "Players who tune colony behavior in depth" name per-work-type rules and allowed-worker filters. The needs filter falls under both. The concept stays valid as written; an optional one-phrase mention could be added at design-promote.
- `design/product/` (no PRD exists): WorkManager has no persistent requirements doc for work-type assignment rules. The sprint PRD (`design/prd.html`) will be the first requirements doc for this area.
- `design/architecture/tech-reference/RimWorld-1.6.md`: documents Scribe persistence, `.Translate()`, the keyed-XML locations and a key-naming convention. It does not cover `Need` / `NeedDef` / `Pawn.needs`. The research required by the sprint (Acceptance bullet 1) targets this gap.
- `design/architecture/tech-reference/LordKuper.Common-1.6.md`: documents `PawnFilter` (flags, `SatisfiesFilter`, `Combine`) and the project convention "Pawn eligibility is expressed through `PawnFilter` ... not ad-hoc predicates". The sprint's WorkManager-local needs filter conflicts with this convention as written (see Existing docs found, D-3).
- `1.6/Languages/{English,Russian,ChineseSimplified}/Keyed/WorkManager_Keyed.xml`: work-type rule settings UI strings (`LordKuper.WorkManager.Settings.WorkTypes.*`). The needs filter UI (section label/tooltip, add-need, remove-need, threshold slider label/tooltip, empty-state text, rule-summary line) needs new keys in all three 1.6 locales.
- `1.1`–`1.5/Languages/English/Keyed/WorkManager_Keyed.xml`: legacy locales. They contain no `Settings.WorkTypes.*` keys (the work-type rules UI is 1.6-only), so the new keys do not go there. This differs from ADR-0005, which added its key to the legacy folders too.
- In-game rule summary tooltip (`WorkTypeAssignmentRule.Description`, rendered under key `...Settings.WorkTypes.WorkTypeRuleSummary`): user-facing runtime text that describes each rule. The PRD should decide whether the needs filter appears in it.
- `1.6/Assemblies/LordKuper.WorkManager.xml` (generated XML doc from `GenerateDocumentationFile`): describes `WorkTypeAssignmentRule` members (`AllowedWorkers`, `EnsureWorkerAssigned`, `MinWorkerNumber`, ...). It is regenerated at build time. New public members need `<summary>` doc comments (build warnings otherwise); there is no manual doc work.

Code side (by asd-architect):

- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs`: the needs filter data lives here. Touches the fields block (after `AllowedWorkers`, l.53), `DefaultRules` (l.96-271, which must stay without needs entries), `Description` (l.276-387, rule summary tooltip), `ExposeData` (l.397-408), `Combine` (l.429-448), `CreateRule` (l.455-470), `IsAllowedWorker` (l.551-555, evaluation hook) and `Validate` (l.560-589, clamp and default-rule normalisation).
- New WorkManager-local type for one need entry (a `DefCache<NeedDef>` + threshold, following Common's `PawnCapacityLimit` pattern). Proposed location `Source/WorkManager/Settings/`. The name and shape are a design decision.
- `Source/WorkManager/Cache/PawnCache.cs`: `IsAllowedWorker(WorkTypeDef)` (l.143-156) memoises the per-work-type verdict in `_allowedWorkTypes`, which `Update()` clears on every refresh (l.244-250). It is the single choke point that almost all assignment paths go through. No change is needed if evaluation is added in `WorkTypeAssignmentRule.IsAllowedWorker`.
- `Source/WorkManager/WorkPriorityUpdater.cs`: the assignment paths. `AssignDedicatedWorkersForDay` (l.241-290) has a fallback that bypasses `IsAllowedWorker` (see Gaps G-3 and Risks). `TryAssignDedicatedWorkersBySchedule` (l.771-838) and the others are already gated.
- `Source/WorkManager/Settings/Settings_WorkTypes.cs`: the rule editor. `DoWorkTypeRule` (l.383-400) composes the sections. A new needs section goes next to `DoRuleAllowedWorkers` (l.192-206). It needs a new cached section height field (pattern l.103-116) and new input-id constants (pattern l.23-47). The "Available pawns" preview `UpdateAllowedWorkers` (l.574-581) uses only `rule.AllowedWorkers.GetFilteredPawns` on the uncombined rule.
- `Source/WorkManager/Resources.cs`: `Strings.Settings.WorkTypes` nested class (l.347-595). New keys use the `$"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(X)}".Translate()` form. The tri-state tooltip helpers `Get*Tooltip(bool triState)` + `AppendUndefinedSettingTooltip` (l.544-590) are the pattern for an inheritable toggle.
- `1.6/Languages/{English,Russian,ChineseSimplified}/Keyed/WorkManager_Keyed.xml`: 47 `Settings.WorkTypes.*` keys in each locale today (parity confirmed).
- `Source/WorkManager.Tests/`: new fixture(s) for the needs filter. The existing `WorkTypeAssignmentRuleComparerTests.cs` already covers `WorkTypeAssignmentRule.Combine` (null-arg and merge cases) and is the extension point. `StateIsolationTestBase.cs` snapshots `WorkTypeAssignmentRule._defaultRule` / `_defaultRulesByName` and is needed for tests that touch `Validate`/`GetDefaultRule`.
- Not touched: `WorkManagerGameComponent.UpdateCombinedRules` (l.426-460) already rebuilds combined rules through `WorkTypeAssignmentRule.Combine` for every `WorkTypeDef`. `Settings_WorkTypes.ExposeWorkTypesData` (l.546-550) persists rules via `Scribe_Collections LookMode.Deep`, so the nested needs entries go through the rule's `ExposeData`. `Settings_Version.cs` (`CurrentVersion = 1`) does not need a bump for an additive optional node (`backward_compat: none` in config).

## Existing docs found

- [concept.html](../../../design/product/concept.html): "Configurable & overridable — Per-work-type rules, allowed-worker filters, priority/threshold sliders and per-pawn opt-out toggles keep the player in control." Target users: "want fine-grained control via per-work-type rules, allowed-worker filters, dedicated-worker modes ...". No mention of needs or mood. Status approved, provenance reverse-engineered.
- [RimWorld-1.6.md](../../../design/architecture/tech-reference/RimWorld-1.6.md):
  - D-1 (persistence): "`Scribe_Collections.Look(ref collection, "label", LookMode.*, ...)` — ... `LookMode.Value`, `LookMode.Def`, `LookMode.Reference` used; the dictionary overload takes auxiliary `ref` key/value lists." This is directly relevant to persisting a `NeedDef → threshold` collection.
  - D-2 (localization convention drift): line 71 states the project key convention is "`WorkManager.<Name>` and `WorkManager.Settings_<Area>_<Name>`" with enum keys `{ModId}.{EnumType}.{Value}.Label`. This is **incomplete**. The whole Work Types settings area (the area this sprint touches) actually uses `LordKuper.WorkManager.Settings.WorkTypes.<Name>`, built from nested `nameof` in `Source/WorkManager/Resources.cs` (e.g. `Resources.cs:525-526`). ADR-0005 repeats only the `WorkManager.Settings_Schedule_*` form. New needs-filter keys should follow the `LordKuper.WorkManager.Settings.WorkTypes.*` form used by their siblings. The tech-reference should be corrected to document both conventions and their scope.
  - No coverage of `Need`, `NeedDef`, `Pawn_NeedsTracker`, `CurLevelPercentage`, Outdoors/Beauty needs, or related thoughts (cabin fever, underground). This is a gap to be closed by the sprint's research deliverable.
- [LordKuper.Common-1.6.md](../../../design/architecture/tech-reference/LordKuper.Common-1.6.md):
  - `PawnFilter` API surface (flags incl. `FilterPawnCapacities`, `FilterPawnSkills`, `FilterPawnStats`, `FilterPawnTraits`; methods `SatisfiesFilter(Pawn, Def)`, `GetSummary`, `GetFilteredPawns`, static `Combine(main, fallback)`). There is no needs dimension, which matches scope decision 1.
  - D-3 (convention conflict): "Pawn eligibility is expressed through `PawnFilter` (flags + `Combine` fallback merging), not ad-hoc predicates." The approved scope makes the needs filter WorkManager-local, outside `PawnFilter`. Design (ADR) must record a scoped exception and amend this convention line at design-promote so the persistent doc does not contradict the as-built code.
  - "UI is composed from `Layout` + `Buttons` + `IconButton` rather than raw `Widgets`/IMGUI where a Common helper exists." This constrains the needs-filter settings UI.
  - "Shared strings come from `LordKuper.Common.Resources.Strings` ...; mod-specific strings live in WorkManager's own `Resources.Strings`." New needs-filter strings are mod-specific and belong in WorkManager.
- [ADR-0005 localize work-shift labels](../../../design/architecture/adr/adr-0005-localize-work-shift-labels.html): the precedent for adding keys: "Add the key to all three 1.6 locales"; ru/zh-Hans "ship as the English string until a translator localizes them"; "A missing key at runtime would surface the raw key text — mitigated by adding it to every shipped locale." This carries over to the needs-filter keys.
- [custom-design-rules.md](../../project/custom-design-rules.md):
  - "Stat / balance / tuning values come from RimWorld `Def`s or mod settings, never hardcoded literals ... ADRs/PRDs introducing new tunables MUST specify the Def/settings surface (see `Source/WorkManager/Settings/`)." The per-need threshold is a new tunable, so the PRD must name the settings surface (`WorkTypeAssignmentRule` in mod settings).
  - "Determinism ... same inputs → same outputs." This applies to the need-block evaluation.
  - "Design changes touching work-priority columns or scheduling MUST consider these compatibility shims (WorkTab, MoreThanCapable, PriorityMaster) and call out impact in the ADR." Disabling a work type (priority 0) goes through the priority-write path used by the shims.
- [WorkManager_Keyed.xml (English, 1.6)](../../../1.6/Languages/English/Keyed/WorkManager_Keyed.xml): existing user-facing wording for the adjacent rule fields that the PRD must stay consistent with:
  - `AllowedWorkersTooltip`: "The settings in this section determine which pawns are considered eligible for assignment to a work type."
  - D-4 (wording conflict): `EnsureWorkerAssignedOnTooltip`: "At least one worker will be assigned even if no pawn satisfies the allowed workers filter." `MinWorkerNumberTooltip`: "At least the specified number of workers will be assigned." Scope decision 5 (needs block is absolute) means these tooltips are no longer unconditionally true once a needs filter exists. The PRD should decide whether to reword them (e.g. "...except pawns blocked by the needs filter") in all three locales.
  - `WorkTypeRuleUndefinedSettingTooltip` / `WorkTypeRuleUndefinedSectionTooltip`: "The setting's value from the default rule will be used." These describe the existing default-rule merge UX. The needs filter "merges with the default rule in the same way", so it will need the same undefined/inherited-state affordance and wording.
  - `AvailablePawnsTooltip`: "List of pawns that satisfy the allowed workers filter." Open point: should this preview also reflect need-blocked pawns? (PRD question.)
- [WorkManager_Keyed.xml (Russian / ChineseSimplified, 1.6)](../../../1.6/Languages/Russian/Keyed/WorkManager_Keyed.xml): key parity with English for the Work Types area (9/9 matched keys for AllowedWorkers/EnsureWorkerAssigned/MinWorkerNumber/AvailablePawns in each locale). The new keys must keep that parity.
- [README.md](../../../README.md) / [About/About.xml](../../../About/About.xml): README says "For a full feature overview see About/About.xml", but About.xml `<description>` is only "Automatic work priority management mod". There is no user-facing feature documentation of work-type rules anywhere. This is not blocking for this sprint; see migration plan note.
- [decisions-log.md](../../project/decisions-log.md), entry 2026-09-29: the approved scope Q&A (Q1–Q5, Q7). It is the authoritative record of scope decisions 1–5 and is consistent with sprint.md.
- Archived sprint 001 ([prd.html](../archived/001-full-audit-alignment/design/prd.html), [audit.md](../archived/001-full-audit-alignment/audit.md)): `WorkTypeAssignmentRule.Combine` is unit-tested "for its merge result (including deterministic tie-breaks)" (PRD line 234). Settings are "Versioned schema (`CurrentVersion = 1`), clamped validation, default rule sets" (audit line 76). These are the precedents for the needs filter's merge, validation (0–100% clamp) and save/load tests.

## Existing implementation found
<!-- architect: code side -->
- No needs-related code exists anywhere in `Source/`: no references to `NeedDef`, `Need`, `pawn.needs` or `TryGetNeed`. `LordKuper.Common`'s `PawnFilter` (workshop build 1.6.4.2) has no needs dimension either. This matches scope decision 1.
- `Source/WorkManager/Settings/WorkTypeAssignmentRule.cs`: the rule is `DefCache<WorkTypeDef>, IExposable`. Its inheritable fields use the tri-state convention: `bool?` / nullable = "undefined, inherit from default rule". `Combine(main, fallback)` (l.429-448) picks `main.X ?? fallback.X`. Dependent values follow their controlling flag (`MinWorkerNumber` follows `EnsureWorkerAssigned.HasValue`, `AssignEveryonePriority` follows `AssignEveryone.HasValue`). Sub-objects delegate to `DedicatedWorkerSettings.Combine` / `PawnFilter.Combine`. `ExposeData` uses `public new void ExposeData()` (hides `DefCache.ExposeData`) and calls `Validate()` only when `Scribe.mode == Saving`. `IsAllowedWorker(pawn)` = `AllowedWorkers!.SatisfiesFilter(pawn, Def)`.
- Common's closest precedent (decompiled workshop `LordKuper.Common.dll` 1.6.4.2): `PawnCapacityLimit : DefCache<PawnCapacityDef>, IExposable` with a `FloatRange Limit`. `PawnFilter.CombinePawnCapacities` takes `main` when `main.FilterPawnCapacities.HasValue`, otherwise `fallback`, and shallow-copies the list (`.ToList()`). `SatisfiesFilter` capacities branch: for each limit with `Def != null`, `Limit.Includes(pawn.health.capacities.GetLevel(def))`. It is inclusive, has no state, entries with an unresolved def are skipped, and it returns `false` when `pawn.health?.capacities == null`. The other `PawnFilter` limits (skills, stats, traits, health states) are also stateless comparisons made on every update, with no hysteresis.
- `DefCache<T>` (Common): stores `_defName` and resolves lazily on first `.Def` via `DefProvider.Current.GetNamedSilentFail<T>`. It then latches `_isInitialized = true` and never re-resolves. `Label` is also cached on first read. `DefProvider.Current` is a public, settable `IDefProvider` seam ("Test fixtures may replace this and MUST restore it after each test").
- `Source/WorkManager/Cache/PawnCache.cs` `IsAllowedWorker` (l.143-156): returns `false` if `Pawn.WorkTypeIsDisabled`, otherwise returns `CombinedRulesDict[workType].IsAllowedWorker(Pawn)`, memoised per refresh. So a check added inside `WorkTypeAssignmentRule.IsAllowedWorker` is re-evaluated on every WorkManager priority update. This covers sprint AC "re-checked on every update".
- `Source/WorkManager/WorkPriorityUpdater.cs` update cycle: `MapComponentTick` (l.739) runs every 64 ticks (`UpdateTickMask`), gated by `Settings.WorkPrioritiesUpdateFrequency × 24h`. Each cycle, `UpdateCache` (l.907) resets every managed work type's cached priority to 0 (`PawnCache.Update`, l.253-257). The assignment passes then raise priorities, and `ApplyWorkPriorities` (l.78-95) writes them through `WorkTypePriorityHelper.SetPriority` (WorkTab-aware). So "disabled (priority 0)" is the default outcome for any managed pawn/work type that no pass assigns. The needs block needs no separate write path.
- Assignment passes that are gated by `PawnCache.IsAllowedWorker` (verified): `AssignCommonWork` (l.192), `AssignDedicatedWorkersForDay` primary candidates (l.257), `TryAssignDedicatedWorkersBySchedule` (l.778, including its bad-worker fail-safe, which draws only from `allowedWorkers`, l.821-824), `AssignWorkersBySkill` (l.576), `AssignWorkersByPassion` (l.527), `AssignWorkersByLearningRate` (l.484), `AssignLeftoverWorkTypes` (l.332, 356, 387) and `AssignWorkToIdlePawns` (l.445).
- NOT gated: the fail-safe in `AssignDedicatedWorkersForDay` (l.279-288). It builds `availableWorkers` from **all** `_capablePawns` not in `goodWorkers`, including pawns that fail `IsAllowedWorker`, and `GetDedicatedWorkersScores` (l.659) filters only on `IsManagedWork`. This is the path that enforces `EnsureWorkerAssigned`/`MinWorkerNumber` when schedule-aware selection is off (`UseScheduleForDedicatedWorkers == false`) or when no allowed worker has a Work hour. The research report's claim that "all assignment paths gate on IsAllowedWorker" is therefore incorrect for this branch. See G-3.
- `EnsureWorkerAssigned`/`MinWorkerNumber` are used only in the two dedicated paths (l.247-248, l.811-812: `targetWorkersCount = Max(target, MinWorkerNumber)`). Both return early when `allowedWorkers.Count == 0` (l.263, l.780). Current code therefore never assigns anyone when *no* pawn passes the filter. The existing `EnsureWorkerAssignedOnTooltip` ("...even if no pawn satisfies the allowed workers filter") already differs from the code (D-4 compounds this).
- Settings persistence: `Settings_WorkTypes.ExposeWorkTypesData` → `Scribe_Collections.Look(ref _workTypeRules, ..., LookMode.Deep)`. Settings load in the `WorkManagerMod` constructor (`GetSettings<Settings>()`), which runs before defs are loaded. `DefCache`'s name-only persistence is what makes `WorkTypeAssignmentRule` (and `PawnCapacityLimit` inside `PawnFilter`) safe there.
- Settings UI (`Settings_WorkTypes.cs`): sections are drawn with `Sections.DoLabeledSectionBox` + `Fields.*`. Inherit/undefined toggles use `Fields.DoLabeledCheckbox(..., ref bool? ...)` (l.236-238). Allowed-workers editing is fully delegated to `PawnFilterWidget.DoPawnFilter`. Common's per-section tri-state header `Sections.DoToggleableSectionHeader` and the delete icon `Common.Resources.Textures.Actions.Delete` are **internal** to Common. Public building blocks available to WorkManager: `Fields.DoLabeledPercentSlider(rect, indent, actionButtons, label, tooltip, ref float, min, max, step, icon, out remRect)`, `Fields.DoLabeledCheckbox(bool?)`, `IconButton(Texture2D?, Action, tooltip, isEnabled)`, `Buttons.DoActionButton`, `Sections.DoLabeledSectionBox`, vanilla `FloatMenu` and `TexUI.DismissTex` (the texture Common's Delete aliases).
- Tests: 63 NUnit tests (7 fixtures), all passing (see Dependencies). None of them builds a `Pawn` or `Need`. `IsInitializedTests` uses `FormatterServices.GetUninitializedObject` for `WorkManagerGameComponent`. `InternalsVisibleTo("LordKuper.WorkManager.Tests")` is set (`AssemblyInfo.cs`).
- In-code stubs: no `TODO`/`FIXME`/`HACK`/`ASD-` markers in `Source/WorkManager` or `Source/WorkManager.Tests`.

## Background research
<!-- architect: RimWorld 1.6 needs mechanics (Need/NeedDef, CurLevel/CurLevelPercentage, thresholds), Outdoors + Beauty needs, cabin fever / underground thoughts and mood effects, related mods handling need-aware work assignment. Sprint Acceptance bullet 1. Target persistent home: extend design/architecture/tech-reference/RimWorld-1.6.md (see migration plan #1). -->
Sources: decompiled local RimWorld 1.6.4871 rev590 `Assembly-CSharp.dll` (ilspycmd); Core `Data/Core/Defs/NeedDefs/Needs.xml` and `ThoughtDefs/Thoughts_Situation_Needs.xml`; decompiled workshop `LordKuper.Common.dll` 1.6.4.2 (item 3531352422); public mod sources and pages cited inline. Only Core is installed locally, so DLC facts are marked [unverified]. The architect spot-checked the Outdoors/Beauty NeedDefs and the NeedOutdoors thought stages against the local Core XML, and they match.

### R-1 Need API (RimWorld 1.6)
- `RimWorld.Need` (abstract): `NeedDef def`; `protected float curLevelInt`; `virtual float MaxLevel => 1f`; `virtual float CurLevel` (clamped to `[0, MaxLevel]`); **`float CurLevelPercentage => CurLevel / MaxLevel`**; `virtual float CurInstantLevel => -1f` (overridden by seekers = target level); `protected List<float> threshPercents` (per instance, not on `NeedDef`); `bool IsFrozen` (suspended, asleep with `freezeWhileSleeping`, in mental state with `freezeInMentalState`, dormant, unspawned/caravan/cryptosleep).
- `Pawn_NeedsTracker` (`pawn.needs`): `List<Need> AllNeeds`; `Need TryGetNeed(NeedDef)` returns `null` when the pawn lacks the need (linear scan over ~10 entries); `TryGetNeed<T>()`; typed shortcut fields (`mood`, `food`, `rest`, `joy`, `beauty`, `roomsize`, `outdoors`, `indoors`, `drugsDesire`, `comfort`, `learning`, `play`), each null when absent. `NeedsTrackerTick` runs `NeedInterval` every 150 ticks (1 h = 2500 ticks, 1 day = 60000).
- `NeedDef` fields that matter for applicability: `needClass`, `minIntelligence`, `colonistsOnly`, `colonistAndPrisonersOnly`, `playerMechsOnly`, `slavesOnly`, `neverOnPrisoner`, `neverOnSlave`, `onlyIfCausedByHediff/Gene/Trait/Ideo`, `titleRequiredAny`, `hediffRequiredAny`, `nullifyingPrecepts`, `requiredComps`, `developmentalStageFilter`. Display fields: `showOnNeedList` (default true), `listPriority`, `major`, `baseLevel` (0.5), `fallPerDay`, `seekerRisePerHour`, `seekerFallPerHour`, `freezeWhileSleeping`, `freezeInMentalState`.
- `Pawn_NeedsTracker.ShouldHaveNeed` gates on intelligence, developmental stage, colonist/prisoner/slave/mech flags, hediff/gene/trait/ideo disables and `onlyIfCausedBy*`, mutant whitelists (Anomaly), titles, nullifying precepts, `requiredComps`, and a hard-coded `defName == "Authority"` → false. Food requires `EatsFood`, Rest requires `needsRest`.
- Null pitfalls: `pawn.needs` is set to null on death, so use `pawn.needs?.TryGetNeed(def)`. Use `CurLevelPercentage`, not `CurLevel`, because Food and MechEnergy have `MaxLevel != 1`. For seekers (Beauty, Comfort, RoomSize), `CurLevel` is the smoothed value that thought workers read and `CurInstantLevel` is the momentary target. Mood-penalty avoidance should read `CurLevelPercentage`.

Core `Needs.xml` (verified locally for Beauty/Outdoors):

| defName | class | applicability gates | listPriority | showOnNeedList |
|---|---|---|---|---|
| Mood | Need_Mood | Humanlike; Baby/Child/Adult | 1000 (major) | false |
| Food | Need_Food | EatsFood | 800 | true |
| Rest | Need_Rest | needsRest | 700 | true |
| Joy ("recreation") | Need_Joy | Humanlike, colonistsOnly, neverOnPrisoner, neverOnSlave, Adult only | 500 | true |
| Beauty | Need_Beauty (seeker) | Humanlike, colonistAndPrisonersOnly; baseLevel 0.4, rise 0.32/h, fall 0.08/h, freezeWhileSleeping | 300 | true |
| Comfort | Need_Comfort (seeker) | Humanlike, colonistAndPrisonersOnly; nullified by Comfort_Ignored precept | 200 | true |
| DrugDesire ("chemical") | Need_Chemical_Any | Humanlike, colonistAndPrisonersOnly | 150 | true |
| Outdoors | Need_Outdoors | Humanlike, colonistAndPrisonersOnly, freezeWhileSleeping | 100 | dynamic |
| Indoors | Need_Indoors | only if caused by ideo/hediff/trait/gene | 100 | dynamic |
| RoomSize | Need_RoomSize (seeker) | Humanlike, colonistAndPrisonersOnly | 100 | false |

Other `Need` subclasses: `Need_Chemical` (per drug), and DLC needs `Need_Deathrest`, `Need_KillThirst`, `Need_Learning`, `Need_Play`, `Need_MechEnergy`, `Need_Suppression`, `Need_Sadism` [defNames unverified]. `Need_Authority` is always excluded. Hemogen is a gene resource, not a `Need`.

### R-2 Applicability by pawn category
- WorkManager manages only spawned, player-faction, non-mechanoid pawns with `workSettings.EverWork` and `skills` (`WorkPriorityUpdater.UpdateCache`). Mechs are therefore out of scope.
- Colonists (adult): all core humanlike needs.
- Slaves: no Joy; have Suppression (Ideology) [unverified defName].
- Guests/lodgers (quest pawns in the player faction): `colonistAndPrisonersOnly` needs (Beauty/Comfort/Outdoors/RoomSize) apply, Joy does not (colonistsOnly).
- Biotech children: no Joy; have Learning (and babies Play) [unverified].
- Anomaly mutants: only whitelisted needs.
- Undergrounder trait: `disablesNeeds: Outdoors` and `enablesNeeds: Indoors` (the pawn has no Outdoors need). Ideology "Indoors: preferred" and the Biotech indoor-dweller gene disable or neutralise Outdoors [unverified]. `Need_Outdoors.Disabled` pins `CurLevel = 1`.
- Consequence: a configured need is often simply absent for a given pawn. The vanilla convention (ThoughtWorkers skip absent needs) is that an absent need imposes nothing.

### R-3 Outdoors (`RimWorld.Need_Outdoors`)
Initial level 1.0. Each 150-tick interval adds `delta × 0.0025`. A pawn is "outdoors" when unspawned or when `Position.UsesOutdoorTemperature(map)` (room touches the map edge, or open-roof cells ≥ 25% of the room).

| situation | delta | per 150 t | per hour | floor |
|---|---|---|---|---|
| outdoors, no roof | +8 | +2% | +33.3% | — |
| indoors, no roof | +5 | +1.25% | +20.8% | — |
| outdoors, thin roof | +1 | +0.25% | +4.2% | — |
| indoors, thin roof | −0.32 | −0.08% | −1.33% | 0.2 |
| outdoors, thick (overhead-mountain) roof | −0.4 | −0.1% | −1.67% | 0.2 |
| indoors, thick roof | −0.45 | −0.1125% | −1.875% | 0.0 |

In bed, negative deltas are ×0.2. The need is frozen while asleep. Floors: a source never lowers the level below its floor, and a level already below the floor does not rise from that source. Only enclosed rooms under overhead mountain drive the level below 20%. Inference: open mining tunnels connected to the outside are usually part of the map-edge "outdoor" room, so mining alone tends to bottom out at cabin fever (−5). −7/−9 come from enclosed mountain rooms. Indoors under thick roof takes ~10.7 h per 20% band. 1 h under open sky restores ~33%.

| CurLevel | OutdoorsCategory | thought (NeedOutdoors) | mood |
|---|---|---|---|
| > 0.8 | Free | — | 0 |
| > 0.6 | NeedFreshAir | stuck indoors | −1 |
| > 0.4 | CabinFeverLight | trapped indoors | −3 |
| ≥ 0.2 | CabinFeverSevere | cabin fever | −5 |
| > 0.05 | Trapped | trapped underground | −7 |
| ≤ 0.05 | Entombed | entombed underground | −9 |

`threshPercents = {0.8, 0.6, 0.4, 0.2, 0.05}`. In 1.6, "cabin fever" is only a thought stage, not a mental state. `ThoughtWorker_NeedOutdoors` is inactive when `HostFaction != null`. Undergrounder thoughts: UndergrounderIndoors +3, UndergrounderUnderground +4. Reference: https://rimworldwiki.com/wiki/Outdoors.

Indoors need (mirror of Outdoors, only for pawns that have it): same thresholds 0.8/0.6/0.4/0.2/0.05 → −1/−3/−5/−7/−8. Deltas ×0.0025: indoor thick +2, indoor thin +1, indoor no-roof 0, outdoor thick 0, outdoor thin/none −0.25. No floor.

### R-4 Beauty (`RimWorld.Need_Beauty : Need_Seeker`)
`CurInstantLevel = Clamp01(0.4 + avgBeauty × 0.1)`, where `avgBeauty = BeautyUtility.AverageBeautyPerceptible(PositionHeld, MapHeld)`. It is 0.5 when unspawned or blind. The seeker moves `CurLevel` toward the target at +0.32/h (up) and −0.08/h (down), and is frozen while asleep. It reacts within hours.

| CurLevel | category | thought (NeedBeauty) | mood |
|---|---|---|---|
| > 0.99 | Beautiful | gorgeous | +15 |
| > 0.85 | VeryPretty | beautiful | +10 |
| > 0.65 | Pretty | pretty | +5 |
| > 0.35 | Neutral | — | 0 |
| > 0.15 | Ugly | unsightly | −5 |
| > 0.01 | VeryUgly | ugly | −10 |
| ≤ 0.01 | Hideous | hideous | −15 |

Mining context: `Filth_RubbleRock` Beauty −15 (indoors) / BeautyOutdoors −4. Rough and rough-hewn stone −1, smooth stone +2, natural soils −3 indoors / 0 outdoors. An average of −1 gives a target of 0.3 (unsightly). ≤ −2.5 gives ≤ 0.15 (ugly). The Ascetic trait does not affect Beauty. Blind pawns sit at 0.5.

### R-5 Other needs (behaviour relevant to a threshold filter)
- Mood: `showOnNeedList = false`, but meaningful as a filter.
- Rest: tired < 0.28, very tired < 0.14.
- Food: hungry at `FoodLevelPercentageWantEat × 0.8`, urgent at `× 0.4`. Churns constantly.
- Joy: thresholds 0.15/0.3/0.7/0.85. Absent for children, slaves and prisoners.
- Comfort: noisy (instant level 0 unless the pawn used a comfort item within the last 15 ticks).
- RoomSize: < 0.01 confined −10, < 0.3 cramped −5, ≥ 0.7 spacious +5. 1.0 in psychologically-outdoor rooms.
- DrugDesire/Chemical: conditional on addiction.
- DLC needs: see R-1.

### R-6 Precedents (need-aware work assignment)
- Free Will (https://github.com/paul-freeman/rimworld-freewill, `Priority.cs`): soft additive scoring. `ConsiderBeautyExpectations` (beauty `CurCategory` → Cleaning), `ConsiderThoughts` (NeedFood → Cooking/Hunting), `ConsiderSuppressionNeed`. No Outdoors consideration, no hysteresis, heavy try/catch.
- Brrr and Phew (Continued) (https://steamcommunity.com/sharedfiles/filedetails/?id=2195938471): "Ooh" triggers on cabin fever and keeps the pawn outside until Outdoors reaches 75% (an enter-low/exit-high latch). This is behaviour/job control, not a work-priority filter.
- SmarterScheduling (https://github.com/maarxx/SmarterScheduling): latch-based schedule state machine (behaviour mod, not a filter).
- Vanilla `DrugPolicyEntry.onlyIfMoodBelow` / `onlyIfJoyBelow`: float default 1.0, percent sliders, plain threshold with no latch. This is the closest vanilla analogue to the planned per-need threshold.
- Work Tab, Priority Master, AutoPriorities, Better Pawn Control, Colony Manager: no known need-gated work types [unverified]. The feature appears novel among work-priority mods.
- LordKuper.Common `PawnFilter` limits (capacities/skills/stats/traits/health states): stateless threshold/range checks evaluated on every update. WorkManager has no hysteresis or latch anywhere.

### R-7 WorkManager integration facts (verified against source; see Existing implementation found)
- Evaluation hook: `WorkTypeAssignmentRule.IsAllowedWorker` → memoised by `PawnCache.IsAllowedWorker` → used by every assignment pass except the day-level dedicated fail-safe (G-3).
- Cadence: `MapComponentTick` every 64 ticks, gated by `WorkPrioritiesUpdateFrequency` (default 1/24 day = hourly; minimum 1/48 day). Each update re-reads need levels, so assignments follow need changes at that cadence.
- ModSettings/DefCache pitfall: settings load in the `WorkManagerMod` constructor, before defs are loaded. `Scribe_Defs` / `LookMode.Def` there resolves to null and logs errors, so persist the NeedDef by name through `DefCache<NeedDef>`. `DefCache.Initialize` latches `_isInitialized` (and `Label` caches). Reading `.Def` or `.Label` before defs load caches null/defName permanently, so the load path (ExposeData / any post-load normalisation) must touch only `DefName`.

### R-8 Recommendations for design (not decisions)
1. Data model: WorkManager-local entry type `DefCache<NeedDef>, IExposable` with a `float` threshold in [0, 1]. The rule gets a list of these entries plus a tri-state flag (`bool?`, "undefined = inherit"), combined like `PawnFilter.CombinePawnCapacities` (take `main` list when the flag `HasValue`, else `fallback`; copy the list). Unresolved defs are kept in settings but skipped at evaluation.
2. Evaluation, in `WorkTypeAssignmentRule.IsAllowedWorker` after `SatisfiesFilter`: `need = pawn.needs?.TryGetNeed(def)`. `null` → pass (need not applicable, matching vanilla ThoughtWorkers). Otherwise block when `need.CurLevelPercentage < threshold`. The comparison is strict, so a 0% threshold never blocks. This mirrors the inclusive lower bound of `FloatRange.Includes` used by Common's capacity limits.
3. Oscillation / hysteresis: **resolved, no hysteresis** (user decision 2026-09-29: "Look at how other similar filters work - add hysteresis if others do"; orchestrator verified that no comparable filter latches, see R-6). The needs filter is a stateless threshold check consistent with the existing `PawnFilter` limits. Residual: fast needs (Beauty, Comfort, Rest, Food, Mood) may flip a work type on and off between hourly updates. Handle this with threshold guidance/tooltip text only.
4. UI need list: source from `DefDatabase<NeedDef>.AllDefsListForReading`. Do not filter by `showOnNeedList` (keeps Mood, RoomSize). Exclude `playerMechsOnly` and `Authority`. Sort by `listPriority` desc, then label. Use `description` as the tooltip. Optional hard-coded threshold hints for Outdoors (80/60/40/20/5) and Beauty (85/65/35/15) are a design choice.
5. Mining guidance (for tooltip/docs, not shipped defaults): Outdoors ~60% (before "trapped indoors" −3), Beauty ~35% (at "unsightly" −5). Blocking Mining does not by itself restore Outdoors unless the pawn then goes outside.
6. Performance: negligible (≤ a few `TryGetNeed` scans per pawn × configured work types per update, memoised per update).
7. Tests: absent need → pass; `pawn.needs == null` → pass; unresolved def → skipped; strict `<` boundary; `Combine` inherit/override; `Validate` clamps to [0, 1]; `DefaultRules` contain no needs entries.

## Gaps
<!-- architect: code side. Docs-side gaps noted below for completeness. -->
- Docs: no persistent requirements doc for work-type assignment rules (the sprint PRD becomes the first one).
- Docs: `RimWorld-1.6.md` tech-reference lacks needs API coverage (D-2 key-convention drift also present).
- Docs: `LordKuper.Common-1.6.md` "eligibility only via PawnFilter" convention needs a scoped exception (D-3).
- Docs: `EnsureWorkerAssigned` / `MinWorkerNumber` tooltips become inaccurate under the absolute needs block (D-4).
- G-1 Data model: `WorkTypeAssignmentRule` has no needs field and there is no per-need entry type. Required: the entry type (`DefCache<NeedDef>` + threshold), the list, and an inherit flag. `ExposeData` must persist them, `Combine` must merge them (inherit-or-override, same rule as the other fields), `Validate` must clamp thresholds to [0, 1] and drop null/duplicate entries, `CreateRule` must initialise them (inherit for specific rules, defined-but-empty for the default rule), and `Description` needs a summary line (per the PRD decision). `DefaultRules` must stay free of needs entries.
- G-2 Evaluation: `WorkTypeAssignmentRule.IsAllowedWorker` checks only `AllowedWorkers.SatisfiesFilter`. The needs check must be added there, after the pawn filter, with an absent-need → pass semantic.
- G-3 Absolute block is not guaranteed today: the `AssignDedicatedWorkersForDay` fail-safe (`WorkPriorityUpdater.cs` l.279-288) draws from all capable pawns, including pawns failing `IsAllowedWorker`. It runs when `UseScheduleForDedicatedWorkers` is off, or when no allowed worker has a Work hour, and then enforces `MinWorkerNumber`/`EnsureWorkerAssigned`. Without a change, a need-blocked pawn can receive the dedicated priority, which violates scope decision 5 and the AC "EnsureWorkerAssigned and MinWorkerNumber never assign a need-blocked pawn". **Open design question (for the ADR):** (a) exclude only need-blocked pawns from that fail-safe, which keeps today's AllowedWorkers behaviour byte-identical and satisfies the AC "existing default rules behave exactly as before"; or (b) restrict the fail-safe to `allowedWorkers` like the schedule-aware path (l.821-824), which also fixes the latent bypass of `AllowedWorkers` and of `Pawn.WorkTypeIsDisabled` but changes existing behaviour. Architect recommendation: (a) for this sprint, because it has the smallest blast radius and matches the AC. Record (b) as a follow-up.
- G-4 Settings UI: there is no needs section. WorkManager must build it itself from public Common primitives (`Fields.DoLabeledCheckbox(bool?)` for the inherit toggle, `Fields.DoLabeledPercentSlider` per need, `IconButton` + `TexUI.DismissTex` for delete, `FloatMenu` for "add need"), because `Sections.DoToggleableSectionHeader` and `Common.Resources.Textures.Actions.Delete` are internal to Common. It also needs a new cached height field and input-id constants in `Settings_WorkTypes.cs`, plus handling of rows whose NeedDef is unresolved (Common's capacities section silently hides such rows, so the user cannot delete them).
- G-5 "Available pawns" preview (`Settings_WorkTypes.UpdateAllowedWorkers`, l.574-581) shows only `AllowedWorkers.GetFilteredPawns` of the uncombined selected rule, and would not reflect the needs block (BA open point). A PRD decision is needed: keep it as is and adjust the tooltip, or also apply the needs check (live need levels, so the preview can go stale between refreshes).
- G-6 Strings: new `Resources.Strings.Settings.WorkTypes.*` members plus keys in 3 locales (section label/tooltip, inherit-toggle tooltip via `AppendUndefinedSettingTooltip`, add/remove need, threshold slider label/tooltip, empty state, summary line). D-4 rewording may also be needed.
- G-7 Tests: no test constructs a `Pawn`/`Need`. The evaluation needs a testable seam, for example a pure static predicate `(float? levelPercentage, float threshold) → blocked` plus `DefProvider.Current` (public Common seam) for NeedDef resolution. `Combine`/`Validate`/`DefaultRules` tests extend `WorkTypeAssignmentRuleComparerTests` / `StateIsolationTestBase`. Building real `Pawn`/`Pawn_NeedsTracker` graphs with `FormatterServices.GetUninitializedObject` + reflection is possible but brittle, and the architect advises against it.

## Risks
<!-- architect: code side. Docs-side risks below. -->
- Localization key drift (D-2): impact=new keys named under the documented `WorkManager.Settings_*` form would be inconsistent with sibling Work Types keys and `Resources.cs` nesting; mitigation=PRD/ADR names the `LordKuper.WorkManager.Settings.WorkTypes.*` form explicitly, and the tech-reference is corrected at design-promote.
- Missing locale keys: impact=raw key text shown in ru/zh UI; mitigation=add every new key to all three 1.6 locales (English placeholder text allowed, per ADR-0005 precedent), and cover this with an AC.
- Misleading tooltips (D-4): impact=users believe `EnsureWorkerAssigned`/`MinWorkerNumber` override the needs block; mitigation=PRD decides the reword, applied in all locales.
- Fail-safe bypass (G-3): impact=need-blocked pawn assigned a dedicated priority via `AssignDedicatedWorkersForDay` fallback, breaking the absolute-block AC (high); mitigation=ADR picks option (a) or (b); unit-testable only indirectly, so cover by design review + a focused test if the fallback candidate selection is extracted.
- DefCache latch before defs load: impact=reading `.Def`/`.Label` of a need entry during `ExposeData` (settings load in the mod constructor) caches `null`/defName permanently → filter silently inert and wrong labels until restart; mitigation=load path touches `DefName` only (no `Scribe_Defs`/`LookMode.Def`), resolution happens lazily at evaluation/UI time; mirror `PawnCapacityLimit`.
- Null list after loading older settings: impact=`Scribe_Collections`/`Scribe_Deep` set a missing node to `null` on load; `Validate()` runs only on Saving, and `Combine`/`IsAllowedWorker` use null-forgiving `!` (`AllowedWorkers!`, `DedicatedWorkerSettings!`) → `NullReferenceException` on first update for every existing user whose settings predate the field; mitigation=null-coalesce the needs list on load (e.g. `Scribe.mode == PostLoadInit` / `LoadingVars` branch, or null-tolerant `Combine`/evaluation) and test load of a rule without the node.
- Unresolved NeedDef (DLC/mod removed): impact=entry kept but inert; UI row may be invisible/undeletable if Common's pattern is copied; mitigation=skip at evaluation, render unresolved rows with defName + delete button.
- Work type starved by absolute block: impact=with Mining gated on Outdoors, a colony-wide cabin-fever episode leaves no miner despite `EnsureWorkerAssigned` (intended per scope decision 5, but surprising); mitigation=tooltip/PRD wording (D-4), threshold guidance (R-8.5).
- Fast-changing needs flip work types (no hysteresis by user decision): impact=Beauty/Comfort/Food/Rest/Mood thresholds may toggle a work type on/off between hourly updates, interrupting jobs; mitigation=documented guidance in tooltips; no latch (resolved, R-8.3).
- Common UI helpers internal: impact=needs section cannot reuse Common's tri-state toggleable header → visual mismatch vs. Allowed-workers sub-sections; mitigation=compose from public `Fields`/`Sections`/`IconButton`; any Common change is out of scope.
- Compatibility shims (custom-design-rules): impact=priority 0 is written through the existing `WorkTypePriorityHelper.SetPriority` path (WorkTab all-hours when present; PriorityMaster/MoreThanCapable unaffected because no new write path); mitigation=none needed beyond stating "no shim impact" in the ADR.
- Build hygiene: impact=`TreatWarningsAsErrors=True` in both Debug and Release plus `GenerateDocumentationFile` → any missing `<summary>` on new public/internal-visible members or nullable warning fails the build; mitigation=doc comments on every new member; run the verified build command below.
- Build/test environment (this WSL machine): impact=`commands.yaml` commands fail as written here — `RimWorldManagedDir`/`RIMWORLD_DIR`/`LORDKUPER_COMMON_DIR` unset (props default to `d:\Games\...` and `..\..\rimworld-common`, which is absent); only .NET SDK 8.0.425 at `/tmp/dotnet` (cannot parse `.slnx`, and C# 14 `field` keyword in `Settings_WorkTypes.cs` l.140 fails with CS0501); `dotnet test` of net472 on Linux needs `mono` (absent); `commands.yaml` `jb-*`/`designmd-*` entries use Windows paths; mitigation=verified working invocation recorded under Dependencies (SDK 10 in `/tmp/dotnet10`, env overrides, tests via Windows NUnit console through WSL interop). Note `/tmp` installs are ephemeral. The workshop Common DLL (1.6.4.2) may drift from the parent repo's `LordKuper.Common` — it compiles today (0 warnings), but Common is `Private=False` so runtime mismatches surface as MissingMethod/TypeLoad, not compile errors.

## Dependencies (optional)
- RimWorld 1.6 `Assembly-CSharp` (`RimWorld.Need`, `NeedDef`, `Pawn_NeedsTracker`, `DefDatabase<NeedDef>`): new API surface consumed; local install 1.6.4871 at `/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld`. No new package/dependency.
- `LordKuper.Common` (compile-only, `Private=False`): consumed surface grows by `DefCache<NeedDef>` usage, `Fields.DoLabeledPercentSlider`, `Fields.DoLabeledCheckbox(bool?)`, `IconButton`, and (tests) `DefProvider.Current`. Verified against workshop build 1.6.4.2 (`/mnt/c/Program Files (x86)/Steam/steamapps/workshop/content/294100/3531352422/1.6/Assemblies/LordKuper.Common.dll`); current WorkManager compiles against it with 0 warnings / 0 errors (Release, `TreatWarningsAsErrors`).
- Build/test environment — verified 2026-09-29 (no repo files modified; build ran in a copy at `/tmp/asd002/src`):
  - SDK: .NET SDK 10.0.401 installed to `/tmp/dotnet10` via `dotnet-install.sh --channel 10.0 --install-dir /tmp/dotnet10` (SDK 8.0.425 at `/tmp/dotnet` fails: MSB4068 on `.slnx`, CS0501 on C# 14 `field` keyword). The tech-reference `dotnet-framework-4.7.2.md` already states SDK 10 is the build toolchain.
  - Build (works, 0 warnings, 0 errors, both projects):
    `RIMWORLD_DIR="/mnt/c/Program Files (x86)/Steam/steamapps/common/RimWorld" LORDKUPER_COMMON_DIR="/mnt/c/Program Files (x86)/Steam/steamapps/workshop/content/294100/3531352422" /tmp/dotnet10/dotnet build Source/WorkManager.slnx -c Release`
    (Run from the repo root this writes `1.6/Assemblies/LordKuper.WorkManager.{dll,pdb,xml}`, which are tracked — in audit the build ran in a copy of `Source/` (`rsync -a --exclude bin --exclude obj Source /tmp/asd002/src/`) so no tracked output changed.)
  - Tests: `dotnet test` on Linux is blocked (net472 testhost requires `mono`, not installed). Working route via WSL interop to Windows .NET Framework:
    1. Build tests with a Windows-form metadata path: `... /tmp/dotnet10/dotnet build Source/WorkManager.Tests/WorkManager.Tests.csproj -c Release '-p:RimWorldManagedDirForTests=C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed'`
    2. Copy `Source/WorkManager.Tests/bin/Release/net472/*` to a Windows-local dir (used: `/mnt/c/Users/coops/AppData/Local/Temp/asd002-tests/bin`; UNC `\\wsl.localhost` paths are not supported by cmd).
    3. Run NUnit.ConsoleRunner 3.20.1 (nupkg unzipped to `.../asd002-tests/runner`): `./runner/tools/nunit3-console.exe 'bin\LordKuper.WorkManager.Tests.dll'` → **63/63 passed** (.NET Framework CLR 4.0.30319).
  - Recommendation (not a sprint change): record these overrides for the implementation phase; `commands.yaml` itself is workflow-owned config and is not edited here.

## Related open stubs (optional)

`.asd/project/stubs.md` does not exist, so there are no open stubs to surface. Architect confirmed: no `TODO`/`FIXME`/`HACK`/`ASD-` markers in `Source/WorkManager/` or `Source/WorkManager.Tests/` (grep, 2026-09-29).

| Sprint of origin | File:Line | Reason | Owner |
|---|---|---|---|
| — | — | no related open stubs (stubs.md absent) | — |

## Documentation migration plan

Items found outside ASD format/location that should become persistent docs in `design/`.
Items addressed by sprint design drafts are NOT listed here (they flow through design → design-promote).
Items NOT covered by sprint scope but worth promoting wait for design-promote handling.

No sources outside ASD format need migrating into `design/` for this sprint. All relevant existing docs already live in `design/` or `.asd/project/`. The updates below are to existing persistent docs, handled at design-promote. They are listed so they are not lost.

| # | Source (path/URL) | Format | Proposed target in `design/` | Type | Notes |
|---|---|---|---|---|---|
| — | — | — | — | — | no migrations |

Persistent-doc updates to carry to design-promote (not migrations):

1. `design/architecture/tech-reference/RimWorld-1.6.md`: add a "Needs (`RimWorld`)" API section from the architect's Background research (Need/NeedDef/`Pawn.needs`, level/percentage semantics, null-need cases). Architect's list of needed additions: `Need.CurLevelPercentage` vs `CurLevel`/`MaxLevel` (Food/MechEnergy `MaxLevel != 1`), seeker `CurInstantLevel` semantics, `IsFrozen`; `Pawn_NeedsTracker.TryGetNeed(NeedDef)` null-when-absent + `pawn.needs` null on death; `NeedDef` applicability flags and `ShouldHaveNeed` gates (incl. hard-coded `Authority` exclusion); `DefDatabase<NeedDef>` enumeration for UI; Outdoors/Beauty level→thought tables (R-3/R-4) as version-specific notes; `NeedsTrackerTick` 150-tick interval; "settings load before defs — never `Scribe_Defs` in ModSettings" as a project convention; bump "Last verified". Also correct the key-naming convention line to document the `LordKuper.WorkManager.Settings.WorkTypes.*` form alongside `WorkManager.Settings_<Area>_*` (D-2).
2. `design/architecture/tech-reference/LordKuper.Common-1.6.md`: amend the "eligibility only via `PawnFilter`" convention with the scoped exception for the WorkManager-local needs filter, referencing this sprint's ADR (D-3). Architect's list of needed additions: `DefCache<T>` lazy-resolve + `_isInitialized` latch + cached `Label` (pitfall before defs load); `DefProvider.Current` public test seam (must be restored after each test); `PawnCapacityLimit` as the limit-entry pattern and `PawnFilter.Combine*` inherit-or-override + list copy semantics; stateless `SatisfiesFilter` limits (no hysteresis) and unresolved-def skip; public UI primitives used (`Fields.DoLabeledPercentSlider`, `Fields.DoLabeledCheckbox(bool?)`, `IconButton`) and the fact that `Sections.DoToggleableSectionHeader` / `Resources.Textures.Actions.Delete` are internal; verified build version (workshop 1.6.4.2); bump "Last verified".
3. `design/product/prd.html` (new persistent PRD, promoted from the sprint draft): first requirements doc for work-type assignment rules. Possibly a place to later reverse-engineer the existing rule fields (AllowedWorkers, AssignEveryone, EnsureWorkerAssigned, MinWorkerNumber, DedicatedWorkerSettings). That is out of scope for this sprint and should be offered at design-promote only.
4. (Optional, out of sprint scope) `About/About.xml` description / README feature overview: README points to About.xml for a "full feature overview", but About.xml has a one-line description. This is user-facing mod metadata, not a `design/` doc. Flag it to the user only; there is no migration target.
