using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using LordKuper.Common.Cache;
using LordKuper.Common.Filters;
using LordKuper.Common.Helpers;
using RimWorld;
using UnityEngine;
using Verse;
using PawnHealthState = LordKuper.Common.Filters.PawnHealthState;
using Strings = LordKuper.WorkManager.Resources.Strings.Settings.WorkTypes;

namespace LordKuper.WorkManager;

/// <summary>
///     Represents a rule for assigning work types to pawns, including allowed workers, dedicated worker settings,
///     and assignment priorities.
/// </summary>
internal class WorkTypeAssignmentRule : DefCache<WorkTypeDef>, IExposable
{
    /// <summary>
    ///     The forbidden pawn health states for allowed workers.
    /// </summary>
    private const PawnHealthState AllowedWorkersForbiddenPawnHealthStates = PawnHealthState.Dead;

    /// <summary>
    ///     The default priority value for <see cref="AssignEveryonePriority" />.
    /// </summary>
    private const int AssignEveryonePriorityDefault = 1;

    /// <summary>
    ///     The forbidden pawn types for allowed workers.
    /// </summary>
    private static readonly PawnType[] AllowedWorkersForbiddenPawnTypes =
    [
        PawnType.Undefined, PawnType.Prisoner, PawnType.Animal
    ];

    /// <summary>
    ///     Cached default rule (the one with a <c>null</c> <see cref="DefCache{T}.DefName" />).
    /// </summary>
    private static WorkTypeAssignmentRule? _defaultRule;

    /// <summary>
    ///     Cached default rules keyed by work type def name, built once from <see cref="DefaultRules" />.
    /// </summary>
    private static Dictionary<string, WorkTypeAssignmentRule>? _defaultRulesByName;

    /// <summary>
    ///     The filter specifying which pawns are allowed to be assigned to this work type.
    ///     Nullable because <see cref="Verse.Scribe_Deep" /> can set this to null during loading.
    /// </summary>
    public PawnFilter? AllowedWorkers = new();

    /// <summary>
    ///     Indicates whether all pawns should be assigned to this work type.
    /// </summary>
    public bool? AssignEveryone;

    /// <summary>
    ///     The priority for assigning everyone to this work type.
    /// </summary>
    public int AssignEveryonePriority = AssignEveryonePriorityDefault;

    /// <summary>
    ///     The settings for dedicated workers for this work type.
    ///     Nullable because <see cref="Verse.Scribe_Deep" /> can set this to null during loading.
    /// </summary>
    public DedicatedWorkerSettings? DedicatedWorkerSettings = new();

    /// <summary>
    ///     Indicates whether at least one worker should always be assigned to this work type.
    /// </summary>
    public bool? EnsureWorkerAssigned;

    /// <summary>
    ///     Gets or sets the minimum number of workers to be assigned.
    /// </summary>
    public int MinWorkerNumber;

    /// <summary>
    ///     Indicates whether the needs filter is applied: <c>null</c> inherits from the default rule, <c>false</c> is off,
    ///     <c>true</c> is on.
    /// </summary>
    public bool? FilterNeeds;

    /// <summary>
    ///     The need thresholds. While the filter is on, a pawn below any threshold is not assigned this work type.
    ///     Nullable because <see cref="Verse.Scribe_Collections" /> sets this to null when loading older settings.
    /// </summary>
    public List<NeedLimit>? NeedLimits = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkTypeAssignmentRule" /> class.
    /// </summary>
    [UsedImplicitly]
    public WorkTypeAssignmentRule() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkTypeAssignmentRule" /> class for the specified work type.
    /// </summary>
    /// <param name="workTypeDefName">The name of the work type definition, or <c>null</c> for the default rule.</param>
    public WorkTypeAssignmentRule(string? workTypeDefName) : base(workTypeDefName) { }

    /// <summary>
    ///     Gets the default set of work type assignment rules.
    /// </summary>
    internal static IEnumerable<WorkTypeAssignmentRule> DefaultRules =>
    [
        new(null)
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = false,
                FilterPawnTypes = true,
                AllowedPawnTypes = [PawnType.Colonist, PawnType.Guest, PawnType.Slave],
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                FilterPawnHealthStates = true,
                AllowedPawnHealthStates = PawnHealthState.Healthy,
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates,
                FilterWorkPassions = false,
                FilterPawnCapacities = false,
                FilterPawnSkills = false,
                FilterPawnStats = false,
                FilterPawnTraits = false,
                FilterWorkCapacities = false
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                TriStateMode = false,
                AllowDedicated = true,
                Mode = DedicatedWorkerMode.CapablePawnRatio,
                CapablePawnRatioFactor = 1f
            },
            AssignEveryone = null,
            FilterNeeds = false,
            AssignEveryonePriority = 1,
            EnsureWorkerAssigned = true,
            MinWorkerNumber = 1
        },
        new("Firefighter")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates,
                FilterPawnHealthStates = true,
                AllowedPawnHealthStates = PawnHealthState.Healthy | PawnHealthState.Resting
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
                { TriStateMode = true, AllowDedicated = false },
            AssignEveryone = true,
            AssignEveryonePriority = 1, EnsureWorkerAssigned = false,
            MinWorkerNumber = 0
        },
        new("Patient")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates,
                FilterPawnHealthStates = true,
                AllowedPawnHealthStates = PawnHealthState.Healthy | PawnHealthState.Resting |
                                          PawnHealthState.NeedsTending | PawnHealthState.Downed |
                                          PawnHealthState.Mental
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
                { TriStateMode = true, AllowDedicated = false },
            AssignEveryone = true,
            AssignEveryonePriority = 1, EnsureWorkerAssigned = false,
            MinWorkerNumber = 0
        },
        new("PatientBedRest")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates,
                FilterPawnHealthStates = true,
                AllowedPawnHealthStates = PawnHealthState.Healthy | PawnHealthState.Resting |
                                          PawnHealthState.NeedsTending | PawnHealthState.Downed |
                                          PawnHealthState.Mental
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
                { TriStateMode = true, AllowDedicated = false },
            AssignEveryone = true,
            AssignEveryonePriority = 1, EnsureWorkerAssigned = false,
            MinWorkerNumber = 0
        },
        new("BasicWorker")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
                { TriStateMode = true, AllowDedicated = false },
            AssignEveryone = true,
            AssignEveryonePriority = 1, EnsureWorkerAssigned = false,
            MinWorkerNumber = 0
        },
        new("Hauling")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
                { TriStateMode = true, AllowDedicated = true },
            AssignEveryone = true,
            AssignEveryonePriority = 4, EnsureWorkerAssigned = true,
            MinWorkerNumber = 1
        },
        new("Cleaning")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
                { TriStateMode = true, AllowDedicated = true },
            AssignEveryone = true,
            AssignEveryonePriority = 4, EnsureWorkerAssigned = true,
            MinWorkerNumber = 1
        },
        new("Doctor")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates,
                FilterPawnHealthStates = true,
                AllowedPawnHealthStates = PawnHealthState.Healthy | PawnHealthState.Resting
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                TriStateMode = true,
                AllowDedicated = true,
                Mode = DedicatedWorkerMode.PawnCount,
                PawnCountFactor = 1f,
                PawnCountFilter = new PawnFilter
                {
                    TriStateMode = false,
                    FilterPawnTypes = true,
                    AllowedPawnTypes =
                    [
                        PawnType.Colonist, PawnType.Guest, PawnType.Slave, PawnType.Prisoner,
                        PawnType.Animal
                    ],
                    FilterPawnHealthStates = true,
                    AllowedPawnHealthStates = PawnHealthState.NeedsTending
                }
            },
            AssignEveryone = false, EnsureWorkerAssigned = true,
            MinWorkerNumber = 2
        },
        new("Hunting")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates,
                FilterPawnPrimaryWeaponTypes = true,
                AllowedPawnPrimaryWeaponTypes = [PawnPrimaryWeaponType.Ranged]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                TriStateMode = true
            },
            AssignEveryone = false, EnsureWorkerAssigned = false,
            MinWorkerNumber = 0
        }
    ];

    /// <summary>
    ///     Gets a detailed description of the rule, including assignment and dedicated worker settings.
    /// </summary>
    public string Description
    {
        get
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(Strings.WorkTypeRuleSummary.AsTipTitle());
            stringBuilder.AppendLineIndented(
                $"{Strings.AssignmentSettingsLabel}".Colorize(ColoredText.ColonistCountColor), 1);
            var anyValue = false;
            if (EnsureWorkerAssigned.HasValue)
            {
                anyValue = true;
                if (EnsureWorkerAssigned == true)
                {
                    stringBuilder.AppendIndented(
                        $"{Strings.MinWorkerNumberLabel}: ".Colorize(ColoredText.ExpectationsColor),
                        2);
                    stringBuilder.AppendLine(MinWorkerNumber.ToString("N0"));
                }
                else
                {
                    stringBuilder.AppendIndented(
                        $"{Strings.AllowDedicatedWorkerLabel}: ".Colorize(ColoredText
                            .ExpectationsColor), 2);
                    stringBuilder.AppendLine(Strings.WorkTypeRuleDisabledSettingTooltip);
                }
            }
            if (AssignEveryone.HasValue)
            {
                anyValue = true;
                stringBuilder.AppendIndented(
                    $"{Strings.AssignEveryoneLabel}: ".Colorize(ColoredText.ExpectationsColor), 2);
                stringBuilder.AppendLine(AssignEveryone.Value
                    ? AssignEveryonePriority.ToString("N0")
                    : Strings.WorkTypeRuleDisabledSettingTooltip);
            }
            if (!anyValue)
                stringBuilder.AppendLineIndented(Strings.WorkTypeRuleUndefinedSectionTooltip, 2);
            if (WorkManagerMod.Settings.UseDedicatedWorkers)
            {
                anyValue = false;
                stringBuilder.AppendLineIndented(
                    $"{Strings.DedicatedWorkerSettingsLabel}".Colorize(ColoredText
                        .ColonistCountColor), 1);
                var dedicated = DedicatedWorkerSettings!;
                if (dedicated.AllowDedicated.HasValue)
                {
                    anyValue = true;
                    stringBuilder.AppendIndented(
                        $"{Strings.AllowDedicatedWorkerLabel}: ".Colorize(ColoredText
                            .ExpectationsColor), 2);
                    stringBuilder.AppendLine(dedicated.AllowDedicated.Value
                        ? Strings.WorkTypeRuleEnabledSettingTooltip
                        : Strings.WorkTypeRuleDisabledSettingTooltip);
                    if (dedicated.AllowDedicated.Value && dedicated.Mode.HasValue)
                    {
                        stringBuilder.AppendIndented(
                            $"{Strings.DedicatedWorkerModeLabel}: ".Colorize(ColoredText
                                .ExpectationsColor), 2);
                        stringBuilder.AppendLine(
                            Resources.Strings.DedicatedWorkerMode.GetDedicatedWorkerModeLabel(
                                dedicated.Mode));
                        switch (dedicated.Mode)
                        {
                            case DedicatedWorkerMode.Constant:
                                stringBuilder.AppendIndented(
                                    $"{Strings.ConstantWorkerCountLabel}: ".Colorize(ColoredText
                                        .ExpectationsColor), 2);
                                stringBuilder.AppendLine(
                                    dedicated.ConstantWorkerCount.ToString("N0"));
                                break;
                            case DedicatedWorkerMode.WorkTypeCount:
                                stringBuilder.AppendIndented(
                                    $"{Strings.WorkTypeCountFactorLabel}: ".Colorize(ColoredText
                                        .ExpectationsColor), 2);
                                stringBuilder.AppendLine(
                                    dedicated.WorkTypeCountFactor.ToString("F2"));
                                break;
                            case DedicatedWorkerMode.CapablePawnRatio:
                                stringBuilder.AppendIndented(
                                    $"{Strings.CapablePawnRatioFactorLabel}: ".Colorize(ColoredText
                                        .ExpectationsColor), 2);
                                stringBuilder.AppendLine(
                                    dedicated.CapablePawnRatioFactor.ToString("F1"));
                                break;
                            case DedicatedWorkerMode.PawnCount:
                                stringBuilder.AppendIndented(
                                    $"{Strings.PawnCountFactorLabel}: ".Colorize(ColoredText
                                        .ExpectationsColor), 2);
                                stringBuilder.AppendLine(dedicated.PawnCountFactor.ToString("F1"));
                                if (dedicated.PawnCountFilter != null)
                                    stringBuilder.AppendLine(
                                        dedicated.PawnCountFilter.GetSummary(2));
                                break;
                            case null:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                if (!anyValue)
                    stringBuilder.AppendLineIndented(Strings.WorkTypeRuleUndefinedSectionTooltip,
                        2);
            }
            stringBuilder.AppendLineIndented(
                $"{Strings.AllowedWorkersLabel}".Colorize(ColoredText.ColonistCountColor), 1);
            if (AllowedWorkers != null)
                stringBuilder.AppendLine(AllowedWorkers.GetSummary(2));
            stringBuilder.AppendLineIndented(
                $"{Strings.NeedsLabel}".Colorize(ColoredText.ColonistCountColor), 1);
            if (!FilterNeeds.HasValue)
            {
                stringBuilder.AppendLineIndented(Strings.WorkTypeRuleUndefinedSectionTooltip, 2);
            }
            else if (!FilterNeeds.Value)
            {
                stringBuilder.AppendLineIndented(Strings.WorkTypeRuleDisabledSettingTooltip, 2);
            }
            else if (NeedLimits == null || NeedLimits.Count == 0)
            {
                stringBuilder.AppendLineIndented(Strings.NeedsEmptyLabel, 2);
            }
            else
            {
                foreach (var limit in NeedLimits)
                {
                    stringBuilder.AppendLineIndented(
                        $"{limit.Label} [{limit.Limit.min.ToStringPercent()}..{limit.Limit.max.ToStringPercent()}]",
                        2);
                }
            }
            return stringBuilder.ToString();
        }
    }

    /// <summary>
    ///     Gets the label for this rule, or the default label if not set.
    /// </summary>
    public override string Label => base.Label ?? Strings.DefaultWorkTypeRuleLabel;

    /// <summary>
    ///     Serializes and deserializes the rule data.
    /// </summary>
    public new void ExposeData()
    {
        if (Scribe.mode == LoadSaveMode.Saving) Validate();
        base.ExposeData();
        Scribe_Deep.Look(ref AllowedWorkers, nameof(AllowedWorkers));
        Scribe_Values.Look(ref AssignEveryone, nameof(AssignEveryone));
        Scribe_Values.Look(ref AssignEveryonePriority, nameof(AssignEveryonePriority),
            AssignEveryonePriorityDefault);
        Scribe_Values.Look(ref EnsureWorkerAssigned, nameof(EnsureWorkerAssigned));
        Scribe_Values.Look(ref MinWorkerNumber, nameof(MinWorkerNumber));
        Scribe_Deep.Look(ref DedicatedWorkerSettings, nameof(DedicatedWorkerSettings));
        Scribe_Values.Look(ref FilterNeeds, nameof(FilterNeeds));
        Scribe_Collections.Look(ref NeedLimits, nameof(NeedLimits), LookMode.Deep);
        if (Scribe.mode == LoadSaveMode.LoadingVars) ValidateNeedsFilter();
    }

    /// <summary>
    ///     Combines two <see cref="WorkTypeAssignmentRule" /> instances into a single rule by applying fallback logic.
    /// </summary>
    /// <param name="main">The primary <see cref="WorkTypeAssignmentRule" /> to use. Cannot be <see langword="null" />.</param>
    /// <param name="fallback">
    ///     The fallback <see cref="WorkTypeAssignmentRule" /> to use when values in <paramref name="main" /> are not set.
    ///     Cannot be <see langword="null" />.
    /// </param>
    /// <returns>
    ///     A new <see cref="WorkTypeAssignmentRule" /> instance that merges the values from <paramref name="main" /> and
    ///     <paramref name="fallback" />. Values from <paramref name="main" /> take precedence unless they are
    ///     <see
    ///         langword="null" />
    ///     or unset, in which case values from <paramref name="fallback" /> are used.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown if <paramref name="main" /> or <paramref name="fallback" /> is
    ///     <see langword="null" />.
    /// </exception>
    public static WorkTypeAssignmentRule Combine(WorkTypeAssignmentRule main,
        WorkTypeAssignmentRule fallback)
    {
        if (main == null) throw new ArgumentNullException(nameof(main));
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        var needsSource = main.FilterNeeds.HasValue ? main : fallback;
        return new WorkTypeAssignmentRule(main.DefName)
        {
            FilterNeeds = needsSource.FilterNeeds,
            NeedLimits = [.. needsSource.NeedLimits ?? []],
            EnsureWorkerAssigned = main.EnsureWorkerAssigned ?? fallback.EnsureWorkerAssigned,
            MinWorkerNumber = main.EnsureWorkerAssigned.HasValue
                ? main.MinWorkerNumber
                : fallback.MinWorkerNumber,
            AssignEveryone = main.AssignEveryone ?? fallback.AssignEveryone,
            AssignEveryonePriority = main.AssignEveryone.HasValue
                ? main.AssignEveryonePriority
                : fallback.AssignEveryonePriority,
            DedicatedWorkerSettings = DedicatedWorkerSettings.Combine(main.DedicatedWorkerSettings!,
                fallback.DedicatedWorkerSettings!),
            AllowedWorkers = PawnFilter.Combine(main.AllowedWorkers!, fallback.AllowedWorkers!)
        };
    }

    /// <summary>
    ///     Creates a new <see cref="WorkTypeAssignmentRule" /> for the specified work type.
    /// </summary>
    /// <param name="workTypeDefName">The name of the work type definition, or <c>null</c> for the default rule.</param>
    /// <returns>A new <see cref="WorkTypeAssignmentRule" /> instance.</returns>
    internal static WorkTypeAssignmentRule CreateRule(string? workTypeDefName)
    {
        return new WorkTypeAssignmentRule(workTypeDefName)
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = workTypeDefName != null,
                ForbiddenPawnTypes = [.. AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                TriStateMode = workTypeDefName != null
            },
            FilterNeeds = workTypeDefName == null ? false : null,
            NeedLimits = []
        };
    }

    /// <summary>
    ///     Returns the cached default rule matching <paramref name="defName" />, or the fallback default rule
    ///     (the one with a <c>null</c> def name) if no specific default exists.
    /// </summary>
    /// <param name="defName">The work type def name, or <c>null</c> for the fallback default rule.</param>
    private static WorkTypeAssignmentRule GetDefaultRule(string? defName)
    {
        if (_defaultRulesByName == null)
        {
            _defaultRulesByName = new Dictionary<string, WorkTypeAssignmentRule>();
            foreach (var rule in DefaultRules)
            {
                if (rule.DefName == null)
                    _defaultRule = rule;
                else
                    _defaultRulesByName[rule.DefName] = rule;
            }
        }
        return defName != null && _defaultRulesByName.TryGetValue(defName, out var specific)
            ? specific
            : _defaultRule!;
    }

    /// <summary>
    ///     Calculates the target number of workers based on the current dedicated worker mode and relevant parameters.
    /// </summary>
    /// <param name="map">The map context used to filter pawns when calculating the worker count in certain modes.</param>
    /// <param name="capablePawnCount">The number of pawns capable of performing work, used in ratio-based calculations.</param>
    /// <param name="dedicatedWorkTypesCount">The number of dedicated work types, used in work type-based calculations.</param>
    /// <returns>
    ///     The calculated target number of workers based on the selected <see cref="DedicatedWorkerMode" /> and the provided
    ///     parameters.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown if the <see cref="DedicatedWorkerMode" /> is set to an unsupported
    ///     value.
    /// </exception>
    public int GetTargetWorkersCount(Map map, int capablePawnCount, int dedicatedWorkTypesCount)
    {
        switch (DedicatedWorkerSettings!.Mode)
        {
            case DedicatedWorkerMode.Constant:
                return DedicatedWorkerSettings.ConstantWorkerCount;
            case DedicatedWorkerMode.WorkTypeCount:
                return Mathf.CeilToInt(DedicatedWorkerSettings.WorkTypeCountFactor *
                                       dedicatedWorkTypesCount);
            case DedicatedWorkerMode.CapablePawnRatio:
                return Mathf.CeilToInt(DedicatedWorkerSettings.CapablePawnRatioFactor *
                                       ((float)capablePawnCount / dedicatedWorkTypesCount));
            case DedicatedWorkerMode.PawnCount:
                var filteredPawns =
                    DedicatedWorkerSettings.PawnCountFilter!.GetFilteredPawns([map], null);
#if DEBUG
                var filteredPawnNames = new List<string>(filteredPawns.Count);
                foreach (var p in filteredPawns)
                {
                    filteredPawnNames.Add(p.LabelShort);
                }
                Logger.LogMessage(
                    $"Pawns eligible for target worker count: {string.Join(", ", filteredPawnNames)}");
#endif
                return Mathf.CeilToInt(
                    DedicatedWorkerSettings.PawnCountFactor * filteredPawns.Count);
            case null:
                return 0;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    ///     Determines whether the specified pawn is allowed to perform work based on the defined criteria.
    /// </summary>
    /// <param name="pawn">The pawn to evaluate. Cannot be <see langword="null" />.</param>
    /// <returns>
    ///     <see langword="true" /> if the pawn satisfies the allowed worker criteria; otherwise, <see langword="false" />
    ///     .
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="pawn" /> is <see langword="null" />.</exception>
    public bool IsAllowedWorker(Pawn pawn)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        return AllowedWorkers!.SatisfiesFilter(pawn, Def) && !IsNeedBlocked(pawn);
    }

    /// <summary>
    ///     Determines whether the needs filter blocks this work type for the specified pawn.
    /// </summary>
    /// <param name="pawn">The pawn to evaluate.</param>
    /// <returns>
    ///     <see langword="true" /> if the filter is on and the pawn's level for any listed need is outside its range.
    /// </returns>
    internal bool IsNeedBlocked(Pawn pawn)
    {
        return IsNeedBlocked(FilterNeeds, NeedLimits,
            def => pawn.needs?.TryGetNeed(def)?.CurLevelPercentage);
    }

    /// <summary>
    ///     Determines whether any need in <paramref name="limits" /> is outside its inclusive range. Entries whose def is
    ///     unresolved and needs the pawn does not have are ignored.
    /// </summary>
    /// <param name="filterNeeds">The filter state; only <c>true</c> can block.</param>
    /// <param name="limits">The need ranges.</param>
    /// <param name="getLevelPercentage">
    ///     Returns the pawn's level for a need as a fraction of its maximum, or <c>null</c> when the pawn lacks the need.
    /// </param>
    /// <returns><see langword="true" /> if the work type is blocked.</returns>
    internal static bool IsNeedBlocked(bool? filterNeeds, IReadOnlyList<NeedLimit>? limits,
        Func<NeedDef, float?> getLevelPercentage)
    {
        if (filterNeeds != true || limits == null || limits.Count == 0) return false;
        foreach (var limit in limits)
        {
            var def = limit.Def;
            if (def == null) continue;
            var level = getLevelPercentage(def);
            if (level.HasValue && (level.Value < limit.Limit.min || level.Value > limit.Limit.max)) return true;
        }
        return false;
    }

    /// <summary>
    ///     Normalizes the needs filter: creates the list, drops empty and duplicate entries, normalizes ranges (NaN reset, clamp, swap) and sets the
    ///     default rule's state. Reads only def names and ranges, so it is safe before defs are loaded.
    /// </summary>
    internal void ValidateNeedsFilter()
    {
        NeedLimits ??= [];
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        NeedLimits.RemoveAll(limit =>
            limit == null || string.IsNullOrEmpty(limit.DefName) || !seen.Add(limit.DefName!));
        foreach (var limit in NeedLimits)
        {
            var min = float.IsNaN(limit.Limit.min) ? NeedLimit.DefaultMin : Mathf.Clamp01(limit.Limit.min);
            var max = float.IsNaN(limit.Limit.max) ? NeedLimit.DefaultMax : Mathf.Clamp01(limit.Limit.max);
            limit.Limit = min <= max ? new FloatRange(min, max) : new FloatRange(max, min);
        }
        if (DefName == null) FilterNeeds ??= false;
    }

    /// <summary>
    ///     Validates and normalizes the rule's settings, ensuring all values are within allowed ranges.
    /// </summary>
    private void Validate()
    {
        var defaultRule = GetDefaultRule(DefName);
        DedicatedWorkerSettings ??= new DedicatedWorkerSettings();
        ValidateNeedsFilter();
        AllowedWorkers ??= new PawnFilter();
        AllowedWorkers.Validate();
        AllowedWorkers.ForbiddenPawnTypes = [.. AllowedWorkersForbiddenPawnTypes];
        AllowedWorkers.ForbiddenPawnHealthStates = AllowedWorkersForbiddenPawnHealthStates;
        if (AllowedWorkers is { FilterPawnHealthStates: true, AllowedPawnHealthStates: PawnHealthState.None })
            AllowedWorkers.AllowedPawnHealthStates =
                defaultRule.AllowedWorkers!.AllowedPawnHealthStates;
        if (AllowedWorkers.FilterPawnPrimaryWeaponTypes == true &&
            AllowedWorkers.AllowedPawnPrimaryWeaponTypes.Count == 0)
            AllowedWorkers.AllowedPawnPrimaryWeaponTypes =
                [.. defaultRule.AllowedWorkers!.AllowedPawnPrimaryWeaponTypes];
        AssignEveryonePriority = Mathf.Clamp(AssignEveryonePriority, 0,
            WorkManagerMod.Settings.MaxWorkTypePriority);
        if (DefName == null)
        {
            AllowedWorkers.TriStateMode = false;
            DedicatedWorkerSettings.TriStateMode = false;
            AssignEveryone ??= false;
            EnsureWorkerAssigned ??= true;
        }
        else
        {
            AllowedWorkers.TriStateMode = true;
            DedicatedWorkerSettings.TriStateMode = true;
        }
    }
}