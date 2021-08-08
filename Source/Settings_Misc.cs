using UnityEngine;
using Verse;
using Strings = WorkManager.Resources.Strings.Settings.Misc;

namespace WorkManager
{
    public partial class Settings

    {
        public static bool VerboseLogging;

        private static void DoMiscTab(Rect rect)
        {
            var listing = new Listing_Standard();
            const int boolSettingsCount = 1;
            var height = boolSettingsCount * (Text.LineHeight + listing.verticalSpacing);
            var viewRect = new Rect(rect.x, 0, rect.width - 16f, height);
            Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
            listing.Begin(viewRect);
            listing.CheckboxLabeled(Strings.VerboseLogging, ref VerboseLogging, Strings.VerboseLoggingTooltip);
            listing.End();
            Widgets.EndScrollView();
        }

        private static void ExposeMiscData()
        {
            Scribe_Values.Look(ref VerboseLogging, nameof(VerboseLogging));
        }
    }
}