using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UnityEngine;
using Verse;

namespace WorkManager
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class Settings : ModSettings
    {
        public static bool AllCleaners = true;
        public static bool AllHaulers = true;
        public static bool AssignAllWorkTypes;
        public static bool AssignMultipleDoctors = true;
        public static bool AssignWorkToIdlePawns = true;
        internal static MethodInfo IsBadWorkMethod;
        public static bool RecoveringPawnsUnfitForWork = true;
        public static bool SpecialRulesForHunters = true;
        public static int UpdateFrequency = 24;
        public static bool VerboseLogging;

        public static void DoWindowContents(Rect rect)
        {
            var options = new Listing_Standard();
            options.Begin(rect);
            var optionRect = options.GetRect(Text.LineHeight);
            var fieldRect = optionRect;
            var labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 8f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, Resources.Strings.UpdateIntervalTooltip);
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, Resources.Strings.UpdateInterval);
            var updateFrequencyBuffer = UpdateFrequency.ToString();
            Widgets.TextFieldNumeric(fieldRect, ref UpdateFrequency, ref updateFrequencyBuffer, 1, 120);
            options.Gap(options.verticalSpacing);
            options.CheckboxLabeled(Resources.Strings.AssignMultipleDoctors, ref AssignMultipleDoctors,
                Resources.Strings.AssignMultipleDoctorsTooltip);
            options.CheckboxLabeled(Resources.Strings.RecoveringPawnsUnfitForWork, ref RecoveringPawnsUnfitForWork,
                Resources.Strings.RecoveringPawnsUnfitForWorkTooltip);
            options.CheckboxLabeled(Resources.Strings.SpecialRulesForHunters, ref SpecialRulesForHunters,
                Resources.Strings.SpecialRulesForHuntersTooltip);
            options.CheckboxLabeled(Resources.Strings.AssignAllWorkTypes, ref AssignAllWorkTypes,
                Resources.Strings.AssignAllWorkTypesTooltip);
            options.CheckboxLabeled(Resources.Strings.AssignWorkToIdlePawns, ref AssignWorkToIdlePawns,
                Resources.Strings.AssignWorkToIdlePawnsTooltip);
            options.CheckboxLabeled(Resources.Strings.AllHaulers, ref AllHaulers, Resources.Strings.AllHaulersTooltip);
            options.CheckboxLabeled(Resources.Strings.AllCleaners, ref AllCleaners,
                Resources.Strings.AllCleanersTooltip);
            options.CheckboxLabeled(Resources.Strings.VerboseLogging, ref VerboseLogging,
                Resources.Strings.VerboseLoggingTooltip);
            options.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref UpdateFrequency, nameof(UpdateFrequency), 24);
            Scribe_Values.Look(ref AssignMultipleDoctors, nameof(AssignMultipleDoctors), true);
            Scribe_Values.Look(ref RecoveringPawnsUnfitForWork, nameof(RecoveringPawnsUnfitForWork), true);
            Scribe_Values.Look(ref SpecialRulesForHunters, nameof(SpecialRulesForHunters), true);
            Scribe_Values.Look(ref AssignAllWorkTypes, nameof(AssignAllWorkTypes));
            Scribe_Values.Look(ref AssignWorkToIdlePawns, nameof(AssignWorkToIdlePawns), true);
            Scribe_Values.Look(ref AllHaulers, nameof(AllHaulers), true);
            Scribe_Values.Look(ref AllCleaners, nameof(AllCleaners), true);
            Scribe_Values.Look(ref VerboseLogging, nameof(VerboseLogging));
        }
    }
}