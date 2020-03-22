using System.Diagnostics.CodeAnalysis;
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
        public static int UpdateFrequency = 24;

        public static void DoWindowContents(Rect rect)
        {
            var options = new Listing_Standard();
            options.Begin(rect);
            var optionRect = options.GetRect(Text.LineHeight);
            var fieldRect = optionRect;
            var labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 8f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, (string) "WorkManager.UpdateIntervalTooltip".Translate());
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, (string) "WorkManager.UpdateInterval".Translate());
            var updateFrequencyBuffer = UpdateFrequency.ToString();
            Widgets.TextFieldNumeric(fieldRect, ref UpdateFrequency, ref updateFrequencyBuffer, 1, 120);
            options.Gap(options.verticalSpacing);
            options.CheckboxLabeled("WorkManager.AssignMultipleDoctors".Translate(), ref AssignMultipleDoctors,
                "WorkManager.AssignMultipleDoctorsTooltip".Translate());
            options.CheckboxLabeled("WorkManager.AssignAllWorkTypes".Translate(), ref AssignAllWorkTypes,
                "WorkManager.AssignAllWorkTypesTooltip".Translate());
            options.CheckboxLabeled("WorkManager.AllHaulers".Translate(), ref AllHaulers,
                "WorkManager.AllHaulersTooltip".Translate());
            options.CheckboxLabeled("WorkManager.AllCleaners".Translate(), ref AllCleaners,
                "WorkManager.AllCleanersTooltip".Translate());
            options.End();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref UpdateFrequency, "UpdateFrequency", 24);
            Scribe_Values.Look(ref AssignMultipleDoctors, "AssignMultipleDoctors", true);
            Scribe_Values.Look(ref AssignAllWorkTypes, "AssignAllWorkTypes");
            Scribe_Values.Look(ref AllHaulers, "AllHaulers", true);
            Scribe_Values.Look(ref AllCleaners, "AllCleaners", true);
        }
    }
}