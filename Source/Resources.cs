using System;
using System.Collections.Concurrent;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
using static LordKuper.Common.Resources.Strings;

namespace LordKuper.WorkManager;

internal static class Resources
{
    internal static class Strings
    {
        internal static readonly string GlobalDisableTooltip = "WorkManager.GlobalDisableTooltip".Translate();
        internal static readonly string GlobalEnableTooltip = "WorkManager.GlobalEnableTooltip".Translate();
        internal static readonly string ModTitle = $"{WorkManagerMod.ModId}.{nameof(ModTitle)}".Translate();
        internal static readonly string PawnDisableTooltip = "WorkManager.PawnDisableTooltip".Translate();
        internal static readonly string PawnEnableTooltip = "WorkManager.PawnEnableTooltip".Translate();

        internal static readonly string PawnScheduleDisableTooltip =
            "WorkManager.PawnScheduleDisableTooltip".Translate();

        internal static readonly string PawnScheduleEnableTooltip = "WorkManager.PawnScheduleEnableTooltip".Translate();
        internal static readonly string WorkTypeDisableTooltip = "WorkManager.WorkTypeDisableTooltip".Translate();
        internal static readonly string WorkTypeEnableTooltip = "WorkManager.WorkTypeEnableTooltip".Translate();

        /// <summary>
        ///     Provides localized labels and tooltips for <see cref="WorkManager.DedicatedWorkerMode" /> values.
        /// </summary>
        internal static class DedicatedWorkerMode
        {
            /// <summary>
            ///     Caches translated labels for each <see cref="WorkManager.DedicatedWorkerMode" />.
            /// </summary>
            private static readonly ConcurrentDictionary<WorkManager.DedicatedWorkerMode, string> Labels = new();

            /// <summary>
            ///     Caches translated tooltips for each <see cref="WorkManager.DedicatedWorkerMode" />.
            /// </summary>
            private static readonly ConcurrentDictionary<WorkManager.DedicatedWorkerMode, string> Tooltips = new();

            /// <summary>
            ///     Gets the localized label for the specified <see cref="WorkManager.DedicatedWorkerMode" />.
            /// </summary>
            /// <param name="mode">The dedicated worker mode, or <c>null</c> for the default label.</param>
            /// <returns>The translated label string.</returns>
            public static string GetDedicatedWorkerModeLabel(WorkManager.DedicatedWorkerMode? mode)
            {
                return mode == null
                    ? Settings.WorkTypes.DefaultWorkTypeRuleLabel
                    : GetDedicatedWorkerModeLabel(mode.Value);
            }

            /// <summary>
            ///     Retrieves the localized label for the specified <see cref="WorkManager.DedicatedWorkerMode" />.
            /// </summary>
            /// <remarks>
            ///     The label is generated based on the mode and is localized using the
            ///     translation system.
            /// </remarks>
            /// <param name="mode">The dedicated worker mode for which to retrieve the label.</param>
            /// <returns>A string containing the localized label for the specified mode.</returns>
            public static string GetDedicatedWorkerModeLabel(WorkManager.DedicatedWorkerMode mode)
            {
                return Labels.GetOrAdd(mode,
                    dwm => $"{WorkManagerMod.ModId}.{nameof(DedicatedWorkerMode)}.{dwm}.Label".Translate());
            }

            /// <summary>
            ///     Gets the localized tooltip for the specified <see cref="WorkManager.DedicatedWorkerMode" />.
            /// </summary>
            /// <param name="mode">The dedicated worker mode, or <c>null</c> for the undefined setting tooltip.</param>
            /// <returns>The translated tooltip string.</returns>
            public static string GetDedicatedWorkerModeTooltip(WorkManager.DedicatedWorkerMode? mode)
            {
                return mode == null
                    ? Settings.WorkTypes.WorkTypeRuleUndefinedSettingTooltip
                    : GetDedicatedWorkerModeTooltip(mode.Value);
            }

            /// <summary>
            ///     Retrieves the tooltip text associated with the specified <see cref="WorkManager.DedicatedWorkerMode" />.
            /// </summary>
            /// <remarks>
            ///     The tooltip text is localized and retrieved based on the provided
            ///     <paramref
            ///         name="mode" />
            ///     .
            /// </remarks>
            /// <param name="mode">The dedicated worker mode for which to retrieve the tooltip.</param>
            /// <returns>A string containing the tooltip text for the specified mode.</returns>
            public static string GetDedicatedWorkerModeTooltip(WorkManager.DedicatedWorkerMode mode)
            {
                return Tooltips.GetOrAdd(mode,
                    dwm => $"{WorkManagerMod.ModId}.{nameof(DedicatedWorkerMode)}.{dwm}.Tooltip".Translate());
            }
        }

        internal static class Settings
        {
            internal static class Schedule
            {
                internal static readonly string AddWorkShift = "WorkManager.Settings_Schedule_AddWorkShift".Translate();

                internal static readonly string ColonistWorkShifts =
                    "WorkManager.Settings_Schedule_ColonistWorkShifts".Translate();

                internal static readonly string ColonistWorkShiftsTooltip =
                    "WorkManager.Settings_Schedule_ColonistWorkShiftsTooltip".Translate();

                internal static readonly string DeleteWorkShift =
                    "WorkManager.Settings_Schedule_DeleteWorkShift".Translate();

                internal static readonly string ManageWorkSchedule =
                    "WorkManager.Settings_Schedule_ManageWorkSchedule".Translate();

                internal static readonly string ManageWorkScheduleTooltip =
                    "WorkManager.Settings_Schedule_ManageWorkScheduleTooltip".Translate();

                internal static readonly string NightOwlWorkShifts =
                    "WorkManager.Settings_Schedule_NightOwlWorkShifts".Translate();

                internal static readonly string NightOwlWorkShiftsTooltip =
                    "WorkManager.Settings_Schedule_NightOwlWorkShiftsTooltip".Translate();

                internal static readonly string ResetWorkShifts =
                    "WorkManager.Settings_Schedule_ResetWorkShifts".Translate();

                internal static readonly string Title = "WorkManager.Settings_Schedule_Title".Translate();

                internal static readonly string UpdateFrequency =
                    "WorkManager.Settings_Schedule_UpdateFrequency".Translate();

                internal static readonly string UpdateFrequencyTooltip =
                    "WorkManager.Settings_Schedule_UpdateFrequencyTooltip".Translate();

                internal static readonly string WorkShiftColumnHeader =
                    "WorkManager.Settings_Schedule_WorkShiftColumnHeader".Translate();

                internal static readonly string WorkShiftColumnHeaderTooltip =
                    "WorkManager.Settings_Schedule_WorkShiftColumnHeaderTooltip".Translate();

                internal static readonly string WorkShiftNumberColumnHeader =
                    "WorkManager.Settings_Schedule_WorkShiftNumberColumnHeader".Translate();

                internal static readonly string WorkShiftThresholdColumnHeader =
                    "WorkManager.Settings_Schedule_WorkShiftThresholdColumnHeader".Translate();

                internal static readonly string WorkShiftThresholdColumnHeaderTooltip =
                    "WorkManager.Settings_Schedule_WorkShiftThresholdColumnHeaderTooltip".Translate();
            }

            internal static class WorkPriorities
            {
                internal static readonly string AssignAllWorkTypes =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(AssignAllWorkTypes)}"
                        .Translate();

                internal static readonly string AssignAllWorkTypesTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(AssignAllWorkTypesTooltip)}"
                        .Translate();

                internal static readonly string AssignWorkToIdlePawns =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(AssignWorkToIdlePawns)}"
                        .Translate();

                internal static readonly string AssignWorkToIdlePawnsTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(AssignWorkToIdlePawnsTooltip)}"
                        .Translate();

                internal static readonly string DedicatedWorkerLearningRateScoreFactor =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(DedicatedWorkerLearningRateScoreFactor)}"
                        .Translate();

                internal static readonly string DedicatedWorkerLearningRateScoreFactorTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(DedicatedWorkerLearningRateScoreFactorTooltip)}"
                        .Translate();

                internal static readonly string DedicatedWorkerPassionScoreFactor =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(DedicatedWorkerPassionScoreFactor)}"
                        .Translate();

                internal static readonly string DedicatedWorkerPassionScoreFactorTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(DedicatedWorkerPassionScoreFactorTooltip)}"
                        .Translate();

                internal static readonly string DedicatedWorkerPriority =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(DedicatedWorkerPriority)}"
                        .Translate();

                internal static readonly string DedicatedWorkerPriorityTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(DedicatedWorkerPriorityTooltip)}"
                        .Translate();

                internal static readonly string DedicatedWorkerSkillScoreFactor =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(DedicatedWorkerSkillScoreFactor)}"
                        .Translate();

                internal static readonly string DedicatedWorkerSkillScoreFactorTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(DedicatedWorkerSkillScoreFactorTooltip)}"
                        .Translate();

                internal static readonly string HighestSkillPriority =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(HighestSkillPriority)}"
                        .Translate();

                internal static readonly string HighestSkillPriorityTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(HighestSkillPriorityTooltip)}"
                        .Translate();

                internal static readonly string IdlePriority =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(IdlePriority)}"
                        .Translate();

                internal static readonly string IdlePriorityTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(IdlePriorityTooltip)}"
                        .Translate();

                internal static readonly string LeftoverPriority =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(LeftoverPriority)}"
                        .Translate();

                internal static readonly string LeftoverPriorityTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(LeftoverPriorityTooltip)}"
                        .Translate();

                internal static readonly string MajorLearningRatePriority =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(MajorLearningRatePriority)}"
                        .Translate();

                internal static readonly string MajorLearningRatePriorityTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(MajorLearningRatePriorityTooltip)}"
                        .Translate();

                internal static readonly string MajorLearningRateThreshold =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(MajorLearningRateThreshold)}"
                        .Translate();

                internal static readonly string MajorLearningRateThresholdTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(MajorLearningRateThresholdTooltip)}"
                        .Translate();

                internal static readonly string MinorLearningRatePriority =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(MinorLearningRatePriority)}"
                        .Translate();

                internal static readonly string MinorLearningRatePriorityTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(MinorLearningRatePriorityTooltip)}"
                        .Translate();

                internal static readonly string MinorLearningRateThreshold =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(MinorLearningRateThreshold)}"
                        .Translate();

                internal static readonly string MinorLearningRateThresholdTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(MinorLearningRateThresholdTooltip)}"
                        .Translate();

                internal static readonly string Title =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(Title)}".Translate();

                internal static readonly string UpdateFrequency =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UpdateFrequency)}"
                        .Translate();

                internal static readonly string UpdateFrequencyTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UpdateFrequencyTooltip)}"
                        .Translate();

                internal static readonly string UseDedicatedWorkers =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UseDedicatedWorkers)}"
                        .Translate();

                internal static readonly string UseDedicatedWorkersTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UseDedicatedWorkersTooltip)}"
                        .Translate();

                internal static readonly string UseLearningRatesPriorities =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UseLearningRatesPriorities)}"
                        .Translate();

                internal static readonly string UseLearningRatesPrioritiesTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UseLearningRatesPrioritiesTooltip)}"
                        .Translate();

                internal static readonly string UsePassionPriorities =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UsePassionPriorities)}"
                        .Translate();

                internal static readonly string UsePassionPrioritiesTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UsePassionPrioritiesTooltip)}"
                        .Translate();

                internal static readonly string UsePawnLearningRateThresholds =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UsePawnLearningRateThresholds)}"
                        .Translate();

                internal static readonly string UsePawnLearningRateThresholdsTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkPriorities)}.{nameof(UsePawnLearningRateThresholdsTooltip)}"
                        .Translate();
            }

            internal static class WorkTypes
            {
                private static string _allowDedicatedWorkerTooltip;
                private static string _allowDedicatedWorkerTriStateTooltip;
                private static string _assignEveryoneTooltip;
                private static string _assignEveryoneTriStateTooltip;
                private static string _ensureWorkerAssignedTooltip;
                private static string _ensureWorkerAssignedTriStateTooltip;
                internal static readonly string AddWorkType = "WorkManager.Settings_WorkTypes_AddWorkType".Translate();

                internal static readonly string AddWorkTypeTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AddWorkTypeTooltip)}"
                        .Translate();

                internal static readonly string AllowDedicated =
                    "WorkManager.Settings_WorkTypes_AllowDedicated".Translate();

                internal static readonly string AllowDedicatedTooltip =
                    "WorkManager.Settings_WorkTypes_AllowDedicatedTooltip".Translate();

                internal static readonly string AllowDedicatedWorkerLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AllowDedicatedWorkerLabel)}"
                        .Translate();

                internal static readonly string AllowDedicatedWorkerOffTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AllowDedicatedWorkerOffTooltip)}"
                        .Translate();

                internal static readonly string AllowDedicatedWorkerOnTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AllowDedicatedWorkerOnTooltip)}"
                        .Translate();

                internal static readonly string AllowedWorkersLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AllowedWorkersLabel)}"
                        .Translate();

                internal static readonly string AllowedWorkersTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AllowedWorkersTooltip)}"
                        .Translate();

                internal static readonly string AssignEveryoneLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AssignEveryoneLabel)}"
                        .Translate();

                internal static readonly string AssignEveryoneOffTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AssignEveryoneOffTooltip)}"
                        .Translate();

                internal static readonly string AssignEveryoneOnTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AssignEveryoneOnTooltip)}"
                        .Translate();

                internal static readonly string AssignEveryonePriorityLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AssignEveryonePriorityLabel)}"
                        .Translate();

                internal static readonly string AssignEveryonePriorityTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AssignEveryonePriorityTooltip)}"
                        .Translate();

                internal static readonly string AssignEveryoneWorkTypes =
                    "WorkManager.Settings_WorkTypes_AssignEveryoneWorkTypes".Translate();

                internal static readonly string AssignEveryoneWorkTypesTooltip =
                    "WorkManager.Settings_WorkTypes_AssignEveryoneWorkTypesTooltip".Translate();

                internal static readonly string AssignmentSettingsLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AssignmentSettingsLabel)}"
                        .Translate();

                internal static readonly string AssignmentSettingsTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AssignmentSettingsTooltip)}"
                        .Translate();

                internal static readonly string AvailablePawnsLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AvailablePawnsLabel)}"
                        .Translate();

                internal static readonly string AvailablePawnsTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(AvailablePawnsTooltip)}"
                        .Translate();

                internal static readonly string CapablePawnRatioFactorLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(CapablePawnRatioFactorLabel)}"
                        .Translate();

                internal static readonly string CapablePawnRatioFactorTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(CapablePawnRatioFactorTooltip)}"
                        .Translate();

                internal static readonly string ConstantWorkerCountLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(ConstantWorkerCountLabel)}"
                        .Translate();

                internal static readonly string ConstantWorkerCountTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(ConstantWorkerCountTooltip)}"
                        .Translate();

                internal static readonly string DedicatedWorkerModeLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(DedicatedWorkerModeLabel)}"
                        .Translate();

                internal static readonly string DedicatedWorkerModeTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(DedicatedWorkerModeTooltip)}"
                        .Translate();

                internal static readonly string DedicatedWorkerSettingsLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(DedicatedWorkerSettingsLabel)}"
                        .Translate();

                internal static readonly string DedicatedWorkerSettingsTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(DedicatedWorkerSettingsTooltip)}"
                        .Translate();

                internal static readonly string DefaultWorkTypeRuleLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(DefaultWorkTypeRuleLabel)}"
                        .Translate();

                internal static readonly string DeleteWorkType =
                    "WorkManager.Settings_WorkTypes_DeleteWorkType".Translate();

                internal static readonly string DeleteWorkTypeTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(DeleteWorkTypeTooltip)}"
                        .Translate();

                internal static readonly string DisabledWorkTypesForForeigners =
                    "WorkManager.Settings_WorkTypes_DisabledWorkTypesForForeigners".Translate();

                internal static readonly string DisabledWorkTypesForForeignersTooltip =
                    "WorkManager.Settings_WorkTypes_DisabledWorkTypesForForeignersTooltip".Translate();

                internal static readonly string DisabledWorkTypesForSlaves =
                    "WorkManager.Settings_WorkTypes_DisabledWorkTypesForSlaves".Translate();

                internal static readonly string DisabledWorkTypesForSlavesTooltip =
                    "WorkManager.Settings_WorkTypes_DisabledWorkTypesForSlavesTooltip".Translate();

                internal static readonly string EnsureWorkerAssignedLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(EnsureWorkerAssignedLabel)}"
                        .Translate();

                internal static readonly string EnsureWorkerAssignedOffTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(EnsureWorkerAssignedOffTooltip)}"
                        .Translate();

                internal static readonly string EnsureWorkerAssignedOnTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(EnsureWorkerAssignedOnTooltip)}"
                        .Translate();

                internal static readonly string NoRuleSelected =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(NoRuleSelected)}"
                        .Translate();

                internal static readonly string PawnCountFactorLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(PawnCountFactorLabel)}"
                        .Translate();

                internal static readonly string PawnCountFactorTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(PawnCountFactorTooltip)}"
                        .Translate();

                internal static readonly string ResetWorkTypes =
                    "WorkManager.Settings_WorkTypes_ResetWorkTypes".Translate();

                internal static readonly string ResetWorkTypesTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(ResetWorkTypesTooltip)}"
                        .Translate();

                internal static readonly string SelectWorkTypeTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(SelectWorkTypeTooltip)}"
                        .Translate();

                internal static readonly string Title =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(Title)}".Translate();

                internal static readonly string WorkTypeCountFactorLabel =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeCountFactorLabel)}"
                        .Translate();

                internal static readonly string WorkTypeCountFactorTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeCountFactorTooltip)}"
                        .Translate();

                internal static readonly string WorkTypeName =
                    "WorkManager.Settings_WorkTypes_WorkTypeName".Translate();

                internal static readonly string WorkTypeNameTooltip =
                    "WorkManager.Settings_WorkTypes_WorkTypeNameTooltip".Translate();

                internal static readonly string WorkTypeNotLoadedTooltip =
                    "WorkManager.Settings_WorkTypes_WorkTypeNotLoadedTooltip".Translate();

                internal static readonly string WorkTypePriority =
                    "WorkManager.Settings_WorkTypes_WorkTypePriority".Translate();

                internal static readonly string WorkTypePriorityTooltip =
                    "WorkManager.Settings_WorkTypes_WorkTypePriorityTooltip".Translate();

                internal static readonly string WorkTypeRuleDisabledSettingTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeRuleDisabledSettingTooltip)}"
                        .Translate();

                internal static readonly string WorkTypeRuleEnabledSettingTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeRuleEnabledSettingTooltip)}"
                        .Translate();

                internal static readonly string WorkTypeRuleHeader =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeRuleHeader)}"
                        .Translate();

                internal static readonly string WorkTypeRuleHeaderDefault =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeRuleHeaderDefault)}"
                        .Translate();

                internal static readonly string WorkTypeRuleHeaderTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeRuleHeaderTooltip)}"
                        .Translate();

                internal static readonly string WorkTypeRuleHeaderTooltipDefault =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeRuleHeaderTooltipDefault)}"
                        .Translate();

                internal static readonly string WorkTypeRuleSummary =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeRuleSummary)}"
                        .Translate();

                public static readonly string WorkTypeRuleUndefinedSectionTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeRuleUndefinedSectionTooltip)}"
                        .Translate();

                public static readonly string WorkTypeRuleUndefinedSettingTooltip =
                    $"{WorkManagerMod.ModId}.{nameof(Settings)}.{nameof(WorkTypes)}.{nameof(WorkTypeRuleUndefinedSettingTooltip)}"
                        .Translate();

                [NotNull]
                private static string AppendUndefinedSettingTooltip(string tooltip)
                {
                    return
                        $"{tooltip}{Environment.NewLine}{MultiCheckboxStates.Partial}: {WorkTypeRuleUndefinedSettingTooltip}";
                }

                [NotNull]
                public static string GetAllowDedicatedWorkerTooltip(bool triState)
                {
                    if (!triState)
                        return _allowDedicatedWorkerTooltip ??= string.Concat(MultiCheckboxStates.On, ": ",
                            AllowDedicatedWorkerOnTooltip, Environment.NewLine, MultiCheckboxStates.Off, ": ",
                            AllowDedicatedWorkerOffTooltip);
                    if (_allowDedicatedWorkerTriStateTooltip != null) return _allowDedicatedWorkerTriStateTooltip;
                    var baseTooltip = GetAllowDedicatedWorkerTooltip(false);
                    _allowDedicatedWorkerTriStateTooltip = AppendUndefinedSettingTooltip(baseTooltip);
                    return _allowDedicatedWorkerTriStateTooltip;
                }

                [NotNull]
                public static string GetAssignEveryoneTooltip(bool triState)
                {
                    if (!triState)
                        return _assignEveryoneTooltip ??= string.Concat(MultiCheckboxStates.On, ": ",
                            AssignEveryoneOnTooltip, Environment.NewLine, MultiCheckboxStates.Off, ": ",
                            AssignEveryoneOffTooltip);
                    if (_assignEveryoneTriStateTooltip != null) return _assignEveryoneTriStateTooltip;
                    var baseTooltip = GetAssignEveryoneTooltip(false);
                    _assignEveryoneTriStateTooltip = AppendUndefinedSettingTooltip(baseTooltip);
                    return _assignEveryoneTriStateTooltip;
                }

                [NotNull]
                public static string GetEnsureWorkerAssignedTooltip(bool triState)
                {
                    if (!triState)
                        return _ensureWorkerAssignedTooltip ??= string.Concat(MultiCheckboxStates.On, ": ",
                            EnsureWorkerAssignedOnTooltip, Environment.NewLine, MultiCheckboxStates.Off, ": ",
                            EnsureWorkerAssignedOffTooltip);
                    if (_ensureWorkerAssignedTriStateTooltip != null) return _ensureWorkerAssignedTriStateTooltip;
                    var baseTooltip = GetEnsureWorkerAssignedTooltip(false);
                    _ensureWorkerAssignedTriStateTooltip = AppendUndefinedSettingTooltip(baseTooltip);
                    return _ensureWorkerAssignedTriStateTooltip;
                }
            }
        }
    }

    [StaticConstructorOnStartup]
    internal static class Textures
    {
        internal static readonly Texture2D PawnToggleButtonDisabled =
            ContentFinder<Texture2D>.Get("work-manager-pawn-off");

        internal static readonly Texture2D PawnToggleButtonEnabled =
            ContentFinder<Texture2D>.Get("work-manager-pawn-on");

        internal static readonly Texture2D PawnToggleButtonInactive =
            ContentFinder<Texture2D>.Get("work-manager-pawn-inactive");

        internal static readonly Texture2D PawnWorkTypeDisabled =
            ContentFinder<Texture2D>.Get("work-manager-pawn-work-type-off");

        internal static readonly Texture2D PrioritiesToggleButtonDisabled =
            ContentFinder<Texture2D>.Get("work-manager-priorities-off");

        internal static readonly Texture2D PrioritiesToggleButtonEnabled =
            ContentFinder<Texture2D>.Get("work-manager-priorities-on");

        internal static readonly Texture2D ScheduleToggleButtonDisabled =
            ContentFinder<Texture2D>.Get("work-manager-schedule-off");

        internal static readonly Texture2D ScheduleToggleButtonEnabled =
            ContentFinder<Texture2D>.Get("work-manager-schedule-on");

        internal static readonly Texture2D WorkTypeToggleButtonDisabled =
            ContentFinder<Texture2D>.Get("work-manager-work-type-off");

        internal static readonly Texture2D WorkTypeToggleButtonEnabled =
            ContentFinder<Texture2D>.Get("work-manager-work-type-on");

        internal static readonly Texture2D WorkTypeToggleButtonInactive =
            ContentFinder<Texture2D>.Get("work-manager-work-type-inactive");
    }
}