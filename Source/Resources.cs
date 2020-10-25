using UnityEngine;
using Verse;

namespace WorkManager
{
    internal static class Resources
    {
        internal static class Strings
        {
            internal static readonly string AllCleaners = "WorkManager.AllCleaners".Translate();
            internal static readonly string AllCleanersTooltip = "WorkManager.AllCleanersTooltip".Translate();
            internal static readonly string AllHaulers = "WorkManager.AllHaulers".Translate();
            internal static readonly string AllHaulersTooltip = "WorkManager.AllHaulersTooltip".Translate();
            internal static readonly string AllHunters = "WorkManager.AllHunters".Translate();
            internal static readonly string AllHuntersTooltip = "WorkManager.AllHuntersTooltip".Translate();

            internal static readonly string AllowMeleeHunters = "WorkManager.AllowMeleeHunters".Translate();

            internal static readonly string AllowMeleeHuntersTooltip =
                "WorkManager.AllowMeleeHuntersTooltip".Translate();

            internal static readonly string AssignAllWorkTypes = "WorkManager.AssignAllWorkTypes".Translate();

            internal static readonly string AssignAllWorkTypesTooltip =
                "WorkManager.AssignAllWorkTypesTooltip".Translate();

            internal static readonly string AssignMultipleDoctors = "WorkManager.AssignMultipleDoctors".Translate();

            internal static readonly string AssignMultipleDoctorsTooltip =
                "WorkManager.AssignMultipleDoctorsTooltip".Translate();

            internal static readonly string AssignWorkToIdlePawns = "WorkManager.AssignWorkToIdlePawns".Translate();

            internal static readonly string AssignWorkToIdlePawnsTooltip =
                "WorkManager.AssignWorkToIdlePawnsTooltip".Translate();

            internal static readonly string GlobalDisableTooltip = "WorkManager.GlobalDisableTooltip".Translate();

            internal static readonly string GlobalEnableTooltip = "WorkManager.GlobalEnableTooltip".Translate();
            internal static readonly string PawnDisableTooltip = "WorkManager.PawnDisableTooltip".Translate();
            internal static readonly string PawnEnableTooltip = "WorkManager.PawnEnableTooltip".Translate();

            internal static readonly string RecoveringPawnsUnfitForWork =
                "WorkManager.RecoveringPawnsUnfitForWork".Translate();

            internal static readonly string RecoveringPawnsUnfitForWorkTooltip =
                "WorkManager.RecoveringPawnsUnfitForWorkTooltip".Translate();

            internal static readonly string SpecialRulesForHunters = "WorkManager.SpecialRulesForHunters".Translate();

            internal static readonly string SpecialRulesForHuntersTooltip =
                "WorkManager.SpecialRulesForHuntersTooltip".Translate();

            internal static readonly string UpdateInterval = "WorkManager.UpdateInterval".Translate();
            internal static readonly string UpdateIntervalTooltip = "WorkManager.UpdateIntervalTooltip".Translate();
            internal static readonly string VerboseLogging = "WorkManager.VerboseLogging".Translate();
            internal static readonly string VerboseLoggingTooltip = "WorkManager.VerboseLoggingTooltip".Translate();
            internal static readonly string WorkManager = "WorkManager".Translate();
            internal static readonly string WorkTypeDisableTooltip = "WorkManager.WorkTypeDisableTooltip".Translate();
            internal static readonly string WorkTypeEnableTooltip = "WorkManager.WorkTypeEnableTooltip".Translate();
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