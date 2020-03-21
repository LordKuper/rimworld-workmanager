using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Verse;

namespace WorkManager
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class Settings : ModSettings
    {
        public static int UpdateFrequency = 1;
        private static string _updateFrequencyBuffer = UpdateFrequency.ToString();
        public static bool AssignAllWorkTypes = true;
        public static bool AssignMultipleDoctors = true;

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
            Widgets.TextFieldNumeric(fieldRect, ref UpdateFrequency, ref _updateFrequencyBuffer, 1, 10);
            options.Gap(options.verticalSpacing);
            options.CheckboxLabeled("WorkManager.AssignMultipleDoctors".Translate(), ref AssignMultipleDoctors,
                "WorkManager.AssignMultipleDoctorsTooltip".Translate());
            options.CheckboxLabeled("WorkManager.AssignAllWorkTypes".Translate(), ref AssignAllWorkTypes,
                "WorkManager.AssignAllWorkTypesTooltip".Translate());
            options.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref UpdateFrequency, "UpdateFrequency", 1);
            Scribe_Values.Look(ref AssignMultipleDoctors, "AssignMultipleDoctors", true);
            Scribe_Values.Look(ref AssignAllWorkTypes, "AssignAllWorkTypes", true);
        }
    }
}