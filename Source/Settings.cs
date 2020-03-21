using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using Verse;

namespace WorkManager
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class Settings : ModSettings
    {
        public static bool AssignAllWorkTypes = true;
        public static bool AssignMultipleDoctors = true;

        public static void DoWindowContents(Rect rect)
        {
            var options = new Listing_Standard();
            options.Begin(rect);
            options.CheckboxLabeled("WorkManager.AssignMultipleDoctors".Translate(), ref AssignMultipleDoctors,
                "WorkManager.AssignMultipleDoctorsTooltip".Translate());
            options.CheckboxLabeled("WorkManager.AssignAllWorkTypes".Translate(), ref AssignAllWorkTypes,
                "WorkManager.AssignAllWorkTypesTooltip".Translate());
            options.End();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref AssignMultipleDoctors, "AssignMultipleDoctors", true);
            Scribe_Values.Look(ref AssignAllWorkTypes, "AssignAllWorkTypes", true);
        }
    }
}