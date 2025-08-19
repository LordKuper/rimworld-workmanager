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
        PawnHealthState.Downed, PawnHealthState.Dead
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
            EnsureWorkerAssigned = true
        },
        new("Firefighter")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = false },
            AssignEveryone = true,
            AssignEveryonePriority = 1,
            EnsureWorkerAssigned = false
        },
        new("Patient")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = false },
            AssignEveryone = true,
            AssignEveryonePriority = 1,
            EnsureWorkerAssigned = false
        },
        new("PatientBedRest")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = false },
            AssignEveryone = true,
            AssignEveryonePriority = 1,
            EnsureWorkerAssigned = false
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
            AssignEveryonePriority = 1,
            EnsureWorkerAssigned = false
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
            AssignEveryonePriority = 4,
            EnsureWorkerAssigned = false
        },
        new("Cleaning")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates]
            },
            DedicatedWorkerSettings = new DedicatedWorkerSettings { TriStateMode = true, AllowDedicated = false },
            AssignEveryone = true,
            AssignEveryonePriority = 4,
            EnsureWorkerAssigned = false
        },
        new("Doctor")
        {
            AllowedWorkers = new PawnFilter
            {
                TriStateMode = true,
                ForbiddenPawnTypes = [..AllowedWorkersForbiddenPawnTypes],
                ForbiddenPawnHealthStates = [..AllowedWorkersForbiddenPawnHealthStates]
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
            AssignEveryone = false,
            EnsureWorkerAssigned = true
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
                stringBuilder.AppendIndented(
                    $"{Strings.EnsureWorkerAssignedLabel}: ".Colorize(ColoredText.ExpectationsColor), 2);
                stringBuilder.AppendLine(EnsureWorkerAssigned.Value
                    ? Strings.WorkTypeRuleEnabledSettingTooltip
                    : Strings.WorkTypeRuleDisabledSettingTooltip);
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
        Scribe_Deep.Look(ref DedicatedWorkerSettings, nameof(DedicatedWorkerSettings));
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
    ///     Validates and normalizes the rule's settings, ensuring all values are within allowed ranges.
    /// </summary>
    private void Validate()
    {
        DedicatedWorkerSettings ??= new DedicatedWorkerSettings();
        AllowedWorkers ??= new PawnFilter();
        AllowedWorkers.ForbiddenPawnTypes = new HashSet<PawnType>(AllowedWorkersForbiddenPawnTypes);
        AllowedWorkers.ForbiddenPawnHealthStates =
            new HashSet<PawnHealthState>(AllowedWorkersForbiddenPawnHealthStates);
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