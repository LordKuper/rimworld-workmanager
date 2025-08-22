using System;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using LordKuper.Common.Cache;
using LordKuper.Common.Filters;
using LordKuper.Common.Helpers;
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
    ///     The default priority value for <see cref="AssignEveryonePriority" />.
    /// </summary>
    private const int AssignEveryonePriorityDefault = 1;

    /// <summary>
    ///     The forbidden pawn health states for allowed workers.
    /// </summary>
    private static readonly PawnHealthState[] AllowedWorkersForbiddenPawnHealthStates =
    [
        PawnHealthState.Dead
    ];

    /// <summary>
    ///     The forbidden pawn types for allowed workers.
    /// </summary>
    private static readonly PawnType[] AllowedWorkersForbiddenPawnTypes =
    [
        PawnType.Undefined, PawnType.Prisoner, PawnType.Animal
    ];

    /// <summary>
    ///     The filter specifying which pawns are allowed to be assigned to this work type.
    /// </summary>
    public PawnFilter AllowedWorkers = new();

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
    /// </summary>
    public DedicatedWorkerSettings DedicatedWorkerSettings = new();

    /// <summary>
    ///     Indicates whether at least one worker should always be assigned to this work type.
    /// </summary>
    public bool? EnsureWorkerAssigned;

    /// <summary>
    ///     Gets or sets the minimum number of workers to be assigned.
    /// </summary>
    public int MinWorkerNumber;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkTypeAssignmentRule" /> class.
    /// </summary>
    [UsedImplicitly]
    public WorkTypeAssignmentRule() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkTypeAssignmentRule" /> class for the specified work type.
    /// </summary>
    /// <param name="workTypeDefName">The name of the work type definition, or <c>null</c> for the default rule.</param>
    public WorkTypeAssignmentRule([CanBeNull] string workTypeDefName) : base(workTypeDefName) { }

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
                AllowedPawnHealthStates = [PawnHealthState.Healthy],
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates],
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
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates], FilterPawnHealthStates = true,
                AllowedPawnHealthStates = [PawnHealthState.Healthy, PawnHealthState.Resting]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = false },
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
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates], FilterPawnHealthStates = true,
                AllowedPawnHealthStates =
                [
                    PawnHealthState.Healthy, PawnHealthState.Resting, PawnHealthState.NeedsTending,
                    PawnHealthState.Downed, PawnHealthState.Mental
                ]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = false },
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
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates], FilterPawnHealthStates = true,
                AllowedPawnHealthStates =
                [
                    PawnHealthState.Healthy, PawnHealthState.Resting, PawnHealthState.NeedsTending,
                    PawnHealthState.Downed, PawnHealthState.Mental
                ]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = false },
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
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = false },
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
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = true },
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
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = true },
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
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates], FilterPawnHealthStates = true,
                AllowedPawnHealthStates = [PawnHealthState.Healthy, PawnHealthState.Resting]
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
                        PawnType.Colonist, PawnType.Guest, PawnType.Slave, PawnType.Prisoner, PawnType.Animal
                    ],
                    FilterPawnHealthStates = true,
                    AllowedPawnHealthStates = [PawnHealthState.NeedsTending]
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
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates],
                FilterPawnPrimaryWeaponTypes = true, AllowedPawnPrimaryWeaponTypes = [PawnPrimaryWeaponType.Ranged]
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
    [NotNull]
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
                        $"{Strings.MinWorkerNumberLabel}: ".Colorize(ColoredText.ExpectationsColor), 2);
                    stringBuilder.AppendLine(MinWorkerNumber.ToString("N0"));
                }
                else
                {
                    stringBuilder.AppendIndented(
                        $"{Strings.AllowDedicatedWorkerLabel}: ".Colorize(ColoredText.ExpectationsColor), 2);
                    stringBuilder.AppendLine(Strings.WorkTypeRuleDisabledSettingTooltip);
                }
            }
            if (AssignEveryone.HasValue)
            {
                anyValue = true;
                stringBuilder.AppendIndented($"{Strings.AssignEveryoneLabel}: ".Colorize(ColoredText.ExpectationsColor),
                    2);
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
                    $"{Strings.DedicatedWorkerSettingsLabel}".Colorize(ColoredText.ColonistCountColor), 1);
                var dedicated = DedicatedWorkerSettings;
                if (dedicated.AllowDedicated.HasValue)
                {
                    anyValue = true;
                    stringBuilder.AppendIndented(
                        $"{Strings.AllowDedicatedWorkerLabel}: ".Colorize(ColoredText.ExpectationsColor), 2);
                    stringBuilder.AppendLine(dedicated.AllowDedicated.Value
                        ? Strings.WorkTypeRuleEnabledSettingTooltip
                        : Strings.WorkTypeRuleDisabledSettingTooltip);
                    if (dedicated.AllowDedicated.Value && dedicated.Mode.HasValue)
                    {
                        stringBuilder.AppendIndented(
                            $"{Strings.DedicatedWorkerModeLabel}: ".Colorize(ColoredText.ExpectationsColor), 2);
                        stringBuilder.AppendLine(
                            Resources.Strings.DedicatedWorkerMode.GetDedicatedWorkerModeLabel(dedicated.Mode));
                        switch (dedicated.Mode)
                        {
                            case DedicatedWorkerMode.Constant:
                                stringBuilder.AppendIndented(
                                    $"{Strings.ConstantWorkerCountLabel}: ".Colorize(ColoredText.ExpectationsColor), 2);
                                stringBuilder.AppendLine(dedicated.ConstantWorkerCount.ToString("N0"));
                                break;
                            case DedicatedWorkerMode.WorkTypeCount:
                                stringBuilder.AppendIndented(
                                    $"{Strings.WorkTypeCountFactorLabel}: ".Colorize(ColoredText.ExpectationsColor), 2);
                                stringBuilder.AppendLine(dedicated.WorkTypeCountFactor.ToString("F2"));
                                break;
                            case DedicatedWorkerMode.CapablePawnRatio:
                                stringBuilder.AppendIndented(
                                    $"{Strings.CapablePawnRatioFactorLabel}: ".Colorize(ColoredText.ExpectationsColor),
                                    2);
                                stringBuilder.AppendLine(dedicated.CapablePawnRatioFactor.ToString("F1"));
                                break;
                            case DedicatedWorkerMode.PawnCount:
                                stringBuilder.AppendIndented(
                                    $"{Strings.PawnCountFactorLabel}: ".Colorize(ColoredText.ExpectationsColor), 2);
                                stringBuilder.AppendLine(dedicated.PawnCountFactor.ToString("F1"));
                                if (dedicated.PawnCountFilter != null)
                                    stringBuilder.AppendLine(dedicated.PawnCountFilter.GetSummary(2));
                                break;
                            case null:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                }
                if (!anyValue)
                    stringBuilder.AppendLineIndented(Strings.WorkTypeRuleUndefinedSectionTooltip, 2);
            }
            stringBuilder.AppendLineIndented($"{Strings.AllowedWorkersLabel}".Colorize(ColoredText.ColonistCountColor),
                1);
            if (AllowedWorkers != null)
                stringBuilder.AppendLine(AllowedWorkers.GetSummary(2));
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
        Scribe_Values.Look(ref AssignEveryonePriority, nameof(AssignEveryonePriority), AssignEveryonePriorityDefault);
        Scribe_Values.Look(ref EnsureWorkerAssigned, nameof(EnsureWorkerAssigned));
        Scribe_Values.Look(ref MinWorkerNumber, nameof(MinWorkerNumber));
        Scribe_Deep.Look(ref DedicatedWorkerSettings, nameof(DedicatedWorkerSettings));
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
    [NotNull]
    public static WorkTypeAssignmentRule Combine([NotNull] WorkTypeAssignmentRule main,
        [NotNull] WorkTypeAssignmentRule fallback)
    {
        if (main == null) throw new ArgumentNullException(nameof(main));
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        return new WorkTypeAssignmentRule(main.DefName)
        {
            EnsureWorkerAssigned = main.EnsureWorkerAssigned ?? fallback.EnsureWorkerAssigned,
            MinWorkerNumber = main.EnsureWorkerAssigned.HasValue ? main.MinWorkerNumber : fallback.MinWorkerNumber,
            AssignEveryone = main.AssignEveryone ?? fallback.AssignEveryone,
            AssignEveryonePriority = main.AssignEveryone.HasValue
                ? main.AssignEveryonePriority
                : fallback.AssignEveryonePriority,
            DedicatedWorkerSettings =
                DedicatedWorkerSettings.Combine(main.DedicatedWorkerSettings, fallback.DedicatedWorkerSettings),
            AllowedWorkers = PawnFilter.Combine(main.AllowedWorkers, fallback.AllowedWorkers)
        };
    }

    /// <summary>
    ///     Creates a new <see cref="WorkTypeAssignmentRule" /> for the specified work type.
    /// </summary>
    /// <param name="workTypeDefName">The name of the work type definition, or <c>null</c> for the default rule.</param>
    /// <returns>A new <see cref="WorkTypeAssignmentRule" /> instance.</returns>
    [NotNull]
    internal static WorkTypeAssignmentRule CreateRule([CanBeNull] string workTypeDefName)
    {
        return new WorkTypeAssignmentRule(workTypeDefName)
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = workTypeDefName != null,
                ForbiddenPawnTypes = [.. AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = [.. AllowedWorkersForbiddenPawnHealthStates]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings
            {
                TriStateMode = workTypeDefName != null
            }
        };
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
        switch (DedicatedWorkerSettings.Mode)
        {
            case DedicatedWorkerMode.Constant:
                return DedicatedWorkerSettings.ConstantWorkerCount;
            case DedicatedWorkerMode.WorkTypeCount:
                return Mathf.CeilToInt(DedicatedWorkerSettings.WorkTypeCountFactor * dedicatedWorkTypesCount);
            case DedicatedWorkerMode.CapablePawnRatio:
                return Mathf.CeilToInt(DedicatedWorkerSettings.CapablePawnRatioFactor *
                                       ((float)capablePawnCount / dedicatedWorkTypesCount));
            case DedicatedWorkerMode.PawnCount:
                var filteredPawns = DedicatedWorkerSettings.PawnCountFilter.GetFilteredPawns([map], null);
#if DEBUG
                Logger.LogMessage(
                    $"Pawns eligible for target worker count: {string.Join(", ", filteredPawns.Select(p => p.LabelShort))}");
#endif
                return Mathf.CeilToInt(DedicatedWorkerSettings.PawnCountFactor * filteredPawns.Count);
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
    public bool IsAllowedWorker([NotNull] Pawn pawn)
    {
        if (pawn == null) throw new ArgumentNullException(nameof(pawn));
        return AllowedWorkers.SatisfiesFilter(pawn, Def);
    }

    /// <summary>
    ///     Validates and normalizes the rule's settings, ensuring all values are within allowed ranges.
    /// </summary>
    private void Validate()
    {
        DedicatedWorkerSettings ??= new DedicatedWorkerSettings();
        AllowedWorkers ??= new PawnFilter();
        AllowedWorkers.ForbiddenPawnTypes = [.. AllowedWorkersForbiddenPawnTypes];
        AllowedWorkers.ForbiddenPawnHealthStates = [.. AllowedWorkersForbiddenPawnHealthStates];
        AssignEveryonePriority = Mathf.Clamp(AssignEveryonePriority, 1, WorkManagerMod.Settings.MaxWorkTypePriority);
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