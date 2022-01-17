using UnityEngine;
using Verse;

namespace WorkManager
{
    internal static class Resources
    {
        internal static class Strings
        {
            internal static readonly string GlobalDisableTooltip = "WorkManager.GlobalDisableTooltip".Translate();

            internal static readonly string GlobalEnableTooltip = "WorkManager.GlobalEnableTooltip".Translate();

            internal static readonly string PawnDisableTooltip = "WorkManager.PawnDisableTooltip".Translate();
            internal static readonly string PawnEnableTooltip = "WorkManager.PawnEnableTooltip".Translate();

            internal static readonly string PawnScheduleDisableTooltip =
                "WorkManager.PawnScheduleDisableTooltip".Translate();

            internal static readonly string PawnScheduleEnableTooltip =
                "WorkManager.PawnScheduleEnableTooltip".Translate();

            internal static readonly string WorkManager = "WorkManager".Translate();
            internal static readonly string WorkTypeDisableTooltip = "WorkManager.WorkTypeDisableTooltip".Translate();
            internal static readonly string WorkTypeEnableTooltip = "WorkManager.WorkTypeEnableTooltip".Translate();

            internal static class Settings
            {
                internal static class Misc
                {
                    internal static readonly string Title = "WorkManager.Settings_Misc_Title".Translate();

                    internal static readonly string VerboseLogging =
                        "WorkManager.Settings_Misc_VerboseLogging".Translate();

                    internal static readonly string VerboseLoggingTooltip =
                        "WorkManager.Settings_Misc_VerboseLoggingTooltip".Translate();
                }

                internal static class Priorities
                {
                    internal static readonly string AllowMeleeHunters =
                        "WorkManager.Settings_Priorities_AllowMeleeHunters".Translate();

                    internal static readonly string AllowMeleeHuntersTooltip =
                        "WorkManager.Settings_Priorities_AllowMeleeHuntersTooltip".Translate();

                    internal static readonly string AssignAllWorkTypes =
                        "WorkManager.Settings_Priorities_AssignAllWorkTypes".Translate();

                    internal static readonly string AssignAllWorkTypesTooltip =
                        "WorkManager.Settings_Priorities_AssignAllWorkTypesTooltip".Translate();

                    internal static readonly string AssignMultipleDoctors =
                        "WorkManager.Settings_Priorities_AssignMultipleDoctors".Translate();

                    internal static readonly string AssignMultipleDoctorsTooltip =
                        "WorkManager.Settings_Priorities_AssignMultipleDoctorsTooltip".Translate();

                    internal static readonly string AssignWorkToIdlePawns =
                        "WorkManager.Settings_Priorities_AssignWorkToIdlePawns".Translate();

                    internal static readonly string AssignWorkToIdlePawnsTooltip =
                        "WorkManager.Settings_Priorities_AssignWorkToIdlePawnsTooltip".Translate();

                    internal static readonly string CountDownedAnimals =
                        "WorkManager.Settings_Priorities_CountDownedAnimals".Translate();

                    internal static readonly string CountDownedAnimalsTooltip =
                        "WorkManager.Settings_Priorities_CountDownedAnimalsTooltip".Translate();

                    internal static readonly string CountDownedColonists =
                        "WorkManager.Settings_Priorities_CountDownedColonists".Translate();

                    internal static readonly string CountDownedColonistsTooltip =
                        "WorkManager.Settings_Priorities_CountDownedColonistsTooltip".Translate();

                    internal static readonly string CountDownedGuests =
                        "WorkManager.Settings_Priorities_CountDownedGuests".Translate();

                    internal static readonly string CountDownedGuestsTooltip =
                        "WorkManager.Settings_Priorities_CountDownedGuestsTooltip".Translate();

                    internal static readonly string CountDownedPrisoners =
                        "WorkManager.Settings_Priorities_CountDownedPrisoners".Translate();

                    internal static readonly string CountDownedPrisonersTooltip =
                        "WorkManager.Settings_Priorities_CountDownedPrisonersTooltip".Translate();

                    internal static readonly string DedicatedWorkerPriority =
                        "WorkManager.Settings_Priorities_DedicatedWorkerPriority".Translate();

                    internal static readonly string DedicatedWorkerPriorityTooltip =
                        "WorkManager.Settings_Priorities_DedicatedWorkerPriorityTooltip".Translate();

                    internal static readonly string DoctoringPriority =
                        "WorkManager.Settings_Priorities_DoctoringPriority".Translate();

                    internal static readonly string DoctoringPriorityTooltip =
                        "WorkManager.Settings_Priorities_DoctoringPriorityTooltip".Translate();

                    internal static readonly string HighestSkillPriority =
                        "WorkManager.Settings_Priorities_HighestSkillPriority".Translate();

                    internal static readonly string HighestSkillPriorityTooltip =
                        "WorkManager.Settings_Priorities_HighestSkillPriorityTooltip".Translate();

                    internal static readonly string IdlePriority =
                        "WorkManager.Settings_Priorities_IdlePriority".Translate();

                    internal static readonly string IdlePriorityTooltip =
                        "WorkManager.Settings_Priorities_IdlePriorityTooltip".Translate();

                    internal static readonly string LeftoverPriority =
                        "WorkManager.Settings_Priorities_LeftoverPriority".Translate();

                    internal static readonly string LeftoverPriorityTooltip =
                        "WorkManager.Settings_Priorities_LeftoverPriorityTooltip".Translate();

                    internal static readonly string MajorLearningRatePriority =
                        "WorkManager.Settings_Priorities_MajorLearningRatePriority".Translate();

                    internal static readonly string MajorLearningRatePriorityTooltip =
                        "WorkManager.Settings_Priorities_MajorLearningRatePriorityTooltip".Translate();

                    internal static readonly string MajorLearningRateThreshold =
                        "WorkManager.Settings_Priorities_MajorLearningRateThreshold".Translate();

                    internal static readonly string MajorLearningRateThresholdTooltip =
                        "WorkManager.Settings_Priorities_MajorLearningRateThresholdTooltip".Translate();

                    internal static readonly string MajorPassionPriority =
                        "WorkManager.Settings_Priorities_MajorPassionPriority".Translate();

                    internal static readonly string MajorPassionPriorityTooltip =
                        "WorkManager.Settings_Priorities_MajorPassionPriorityTooltip".Translate();

                    internal static readonly string MinorLearningRatePriority =
                        "WorkManager.Settings_Priorities_MinorLearningRatePriority".Translate();

                    internal static readonly string MinorLearningRatePriorityTooltip =
                        "WorkManager.Settings_Priorities_MinorLearningRatePriorityTooltip".Translate();

                    internal static readonly string MinorLearningRateThreshold =
                        "WorkManager.Settings_Priorities_MinorLearningRateThreshold".Translate();

                    internal static readonly string MinorLearningRateThresholdTooltip =
                        "WorkManager.Settings_Priorities_MinorLearningRateThresholdTooltip".Translate();

                    internal static readonly string MinorPassionPriority =
                        "WorkManager.Settings_Priorities_MinorPassionPriority".Translate();

                    internal static readonly string MinorPassionPriorityTooltip =
                        "WorkManager.Settings_Priorities_MinorPassionPriorityTooltip".Translate();

                    internal static readonly string RecoveringPawnsUnfitForWork =
                        "WorkManager.Settings_Priorities_RecoveringPawnsUnfitForWork".Translate();

                    internal static readonly string RecoveringPawnsUnfitForWorkTooltip =
                        "WorkManager.Settings_Priorities_RecoveringPawnsUnfitForWorkTooltip".Translate();

                    internal static readonly string UncontrollablePawnsUnfitForWork =
                        "WorkManager.Settings_Priorities_UncontrollablePawnsUnfitForWork".Translate();

                    internal static readonly string UncontrollablePawnsUnfitForWorkTooltip =
                        "WorkManager.Settings_Priorities_UncontrollablePawnsUnfitForWorkTooltip".Translate();

                    internal static readonly string SpecialRulesForDoctors =
                        "WorkManager.Settings_Priorities_SpecialRulesForDoctors".Translate();

                    internal static readonly string SpecialRulesForDoctorsTooltip =
                        "WorkManager.Settings_Priorities_SpecialRulesForDoctorsTooltip".Translate();

                    internal static readonly string SpecialRulesForHunters =
                        "WorkManager.Settings_Priorities_SpecialRulesForHunters".Translate();

                    internal static readonly string SpecialRulesForHuntersTooltip =
                        "WorkManager.Settings_Priorities_SpecialRulesForHuntersTooltip".Translate();

                    internal static readonly string Title = "WorkManager.Settings_Priorities_Title".Translate();

                    internal static readonly string UpdateFrequency =
                        "WorkManager.Settings_Priorities_UpdateFrequency".Translate();

                    internal static readonly string UpdateFrequencyTooltip =
                        "WorkManager.Settings_Priorities_UpdateFrequencyTooltip".Translate();

                    internal static readonly string UseDedicatedWorkers =
                        "WorkManager.Settings_Priorities_UseDedicatedWorkers".Translate();

                    internal static readonly string UseDedicatedWorkersTooltip =
                        "WorkManager.Settings_Priorities_UseDedicatedWorkersTooltip".Translate();

                    internal static readonly string UseLearningRates =
                        "WorkManager.Settings_Priorities_UseLearningRates".Translate();

                    internal static readonly string UseLearningRatesTooltip =
                        "WorkManager.Settings_Priorities_UseLearningRatesTooltip".Translate();

                    internal static readonly string UsePawnLearningRateThresholds =
                        "WorkManager.Settings_Priorities_UsePawnLearningRateThresholds".Translate();

                    internal static readonly string UsePawnLearningRateThresholdsTooltip =
                        "WorkManager.Settings_Priorities_UsePawnLearningRateThresholdsTooltip".Translate();
                }

                internal static class Schedule
                {
                    internal static readonly string AddWorkShift =
                        "WorkManager.Settings_Schedule_AddWorkShift".Translate();

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

                internal static class WorkTypes
                {
                    internal static readonly string AddWorkType =
                        "WorkManager.Settings_WorkTypes_AddWorkType".Translate();

                    internal static readonly string AllowDedicated =
                        "WorkManager.Settings_WorkTypes_AllowDedicated".Translate();

                    internal static readonly string AllowDedicatedTooltip =
                        "WorkManager.Settings_WorkTypes_AllowDedicatedTooltip".Translate();

                    internal static readonly string AssignEveryoneWorkTypes =
                        "WorkManager.Settings_WorkTypes_AssignEveryoneWorkTypes".Translate();

                    internal static readonly string AssignEveryoneWorkTypesTooltip =
                        "WorkManager.Settings_WorkTypes_AssignEveryoneWorkTypesTooltip".Translate();

                    internal static readonly string DeleteWorkType =
                        "WorkManager.Settings_WorkTypes_DeleteWorkType".Translate();

                    internal static readonly string DisabledWorkTypesForForeigners =
                        "WorkManager.Settings_WorkTypes_DisabledWorkTypesForForeigners".Translate();

                    internal static readonly string DisabledWorkTypesForForeignersTooltip =
                        "WorkManager.Settings_WorkTypes_DisabledWorkTypesForForeignersTooltip".Translate();

                    internal static readonly string DisabledWorkTypesForSlaves =
                        "WorkManager.Settings_WorkTypes_DisabledWorkTypesForSlaves".Translate();

                    internal static readonly string DisabledWorkTypesForSlavesTooltip =
                        "WorkManager.Settings_WorkTypes_DisabledWorkTypesForSlavesTooltip".Translate();

                    internal static readonly string ResetWorkTypes =
                        "WorkManager.Settings_WorkTypes_ResetWorkTypes".Translate();

                    internal static readonly string Title = "WorkManager.Settings_WorkTypes_Title".Translate();

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
}