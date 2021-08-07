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

                    internal static readonly string MajorLearningRateThreshold =
                        "WorkManager.Settings_Priorities_MajorLearningRateThreshold".Translate();

                    internal static readonly string MajorLearningRateThresholdTooltip =
                        "WorkManager.Settings_Priorities_MajorLearningRateThresholdTooltip".Translate();

                    internal static readonly string MinorLearningRateThreshold =
                        "WorkManager.Settings_Priorities_MinorLearningRateThreshold".Translate();

                    internal static readonly string MinorLearningRateThresholdTooltip =
                        "WorkManager.Settings_Priorities_MinorLearningRateThresholdTooltip".Translate();

                    internal static readonly string RecoveringPawnsUnfitForWork =
                        "WorkManager.Settings_Priorities_RecoveringPawnsUnfitForWork".Translate();

                    internal static readonly string RecoveringPawnsUnfitForWorkTooltip =
                        "WorkManager.Settings_Priorities_RecoveringPawnsUnfitForWorkTooltip".Translate();

                    internal static readonly string SpecialRulesForDoctors =
                        "WorkManager.Settings_Priorities_SpecialRulesForDoctors".Translate();

                    internal static readonly string SpecialRulesForDoctorsTooltip =
                        "WorkManager.Settings_Priorities_SpecialRulesForDoctorsTooltip".Translate();

                    internal static readonly string SpecialRulesForHunters =
                        "WorkManager.Settings_Priorities_SpecialRulesForHunters".Translate();

                    internal static readonly string SpecialRulesForHuntersTooltip =
                        "WorkManager.Settings_Priorities_SpecialRulesForHuntersTooltip".Translate();

                    internal static readonly string Title = "WorkManager.Settings_Priorities_Title".Translate();

                    internal static readonly string UpdateInterval =
                        "WorkManager.Settings_Priorities_UpdateInterval".Translate();

                    internal static readonly string UpdateIntervalTooltip =
                        "WorkManager.Settings_Priorities_UpdateIntervalTooltip".Translate();

                    internal static readonly string UseDedicatedWorkers =
                        "WorkManager.Settings_Priorities_UseDedicatedWorkers".Translate();

                    internal static readonly string UseDedicatedWorkersTooltip =
                        "WorkManager.Settings_Priorities_UseDedicatedWorkersTooltip".Translate();

                    internal static readonly string UsePawnLearningRateThresholds =
                        "WorkManager.Settings_Priorities_UsePawnLearningRateThresholds".Translate();

                    internal static readonly string UsePawnLearningRateThresholdsTooltip =
                        "WorkManager.Settings_Priorities_UsePawnLearningRateThresholdsTooltip".Translate();
                }

                internal static class Schedule
                {
                    internal static readonly string ManageWorkSchedule =
                        "WorkManager.Settings_Schedule_ManageWorkSchedule".Translate();

                    internal static readonly string ManageWorkScheduleTooltip =
                        "WorkManager.Settings_Schedule_ManageWorkScheduleTooltip".Translate();

                    internal static readonly string Title = "WorkManager.Settings_Schedule_Title".Translate();
                }

                internal static class WorkTypes
                {
                    internal static readonly string AddAssignEveryoneWorkType =
                        "WorkManager.Settings_WorkTypes_AddAssignEveryoneWorkType".Translate();

                    internal static readonly string AllowDedicated =
                        "WorkManager.Settings_WorkTypes_AllowDedicated".Translate();

                    internal static readonly string AllowDedicatedTooltip =
                        "WorkManager.Settings_WorkTypes_AllowDedicatedTooltip".Translate();

                    internal static readonly string AssignEveryoneWorkTypes =
                        "WorkManager.Settings_WorkTypes_AssignEveryoneWorkTypes".Translate();

                    internal static readonly string AssignEveryoneWorkTypesTooltip =
                        "WorkManager.Settings_WorkTypes_AssignEveryoneWorkTypesTooltip".Translate();

                    internal static readonly string DeleteAssignEveryoneWorkType =
                        "WorkManager.Settings_WorkTypes_DeleteAssignEveryoneWorkType".Translate();

                    internal static readonly string ResetAssignEveryoneWorkTypes =
                        "WorkManager.Settings_WorkTypes_ResetAssignEveryoneWorkTypes".Translate();

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
            internal static readonly Texture2D GlobalToggleButtonDisabled =
                ContentFinder<Texture2D>.Get("work-manager-global-off");

            internal static readonly Texture2D GlobalToggleButtonEnabled =
                ContentFinder<Texture2D>.Get("work-manager-global-on");

            internal static readonly Texture2D PawnToggleButtonDisabled =
                ContentFinder<Texture2D>.Get("work-manager-pawn-off");

            internal static readonly Texture2D PawnToggleButtonEnabled =
                ContentFinder<Texture2D>.Get("work-manager-pawn-on");

            internal static readonly Texture2D PawnToggleButtonInactive =
                ContentFinder<Texture2D>.Get("work-manager-pawn-inactive");

            internal static readonly Texture2D PawnWorkTypeDisabled =
                ContentFinder<Texture2D>.Get("work-manager-pawn-work-type-off");

            internal static readonly Texture2D WorkTypeToggleButtonDisabled =
                ContentFinder<Texture2D>.Get("work-manager-work-type-off");

            internal static readonly Texture2D WorkTypeToggleButtonEnabled =
                ContentFinder<Texture2D>.Get("work-manager-work-type-on");

            internal static readonly Texture2D WorkTypeToggleButtonInactive =
                ContentFinder<Texture2D>.Get("work-manager-work-type-inactive");
        }
    }
}