using UnityEngine;
using Verse;
using Strings = WorkManager.Resources.Strings.Settings.Priorities;

namespace WorkManager
{
    public partial class Settings
    {
        public static bool AllowMeleeHunters;
        public static bool AssignAllWorkTypes;
        public static bool AssignMultipleDoctors = true;
        public static bool AssignWorkToIdlePawns = true;
        public static bool CountDownedAnimals = true;
        public static bool CountDownedColonists = true;
        public static bool CountDownedGuests = true;
        public static bool CountDownedPrisoners = true;
        public static int DedicatedWorkerPriority = 1;
        public static int DoctoringPriority = 1;
        public static int HighestSkillPriority = 1;
        public static int IdlePriority = 4;
        public static int LeftoverPriority = 4;
        public static int MajorLearningRatePriority = 2;
        public static float MajorLearningRateThreshold = 1.2f;
        public static int MinorLearningRatePriority = 3;
        public static float MinorLearningRateThreshold = 0.8f;
        public static bool RecoveringPawnsUnfitForWork = true;
        public static bool SpecialRulesForDoctors = true;
        public static bool SpecialRulesForHunters = true;
        public static int UpdateFrequency = 24;
        public static bool UseDedicatedWorkers = true;
        public static bool UsePawnLearningRateThresholds;

        private static void DoPrioritiesTab(Rect rect)
        {
            var listing = new Listing_Standard();
            var boolSettingsCount = 7;
            if (SpecialRulesForDoctors)
            {
                boolSettingsCount++;
                if (AssignMultipleDoctors) { boolSettingsCount += 4; }
            }
            if (SpecialRulesForHunters) { boolSettingsCount++; }
            var numericSettingsCount = 6;
            if (AssignAllWorkTypes) { numericSettingsCount++; }
            if (AssignWorkToIdlePawns) { numericSettingsCount++; }
            if (SpecialRulesForDoctors) { numericSettingsCount++; }
            var height = boolSettingsCount * (Text.LineHeight + listing.verticalSpacing) +
                         numericSettingsCount * (Text.LineHeight * 1.5f + listing.verticalSpacing);
            var viewRect = new Rect(rect.x, 0, rect.width - 16f, height);
            Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
            listing.Begin(viewRect);
            DoIntegerSlider(listing, ref UpdateFrequency, 1, 120, Strings.UpdateFrequency,
                Strings.UpdateFrequencyTooltip);
            listing.CheckboxLabeled(Strings.UseDedicatedWorkers, ref UseDedicatedWorkers,
                Strings.UseDedicatedWorkersTooltip);
            listing.Indent(16f);
            listing.ColumnWidth -= 16f;
            if (UseDedicatedWorkers)
            {
                DoIntegerSlider(listing, ref DedicatedWorkerPriority, 1, MaxPriority, Strings.DedicatedWorkerPriority,
                    Strings.DedicatedWorkerPriorityTooltip);
            }
            else
            {
                DoIntegerSlider(listing, ref HighestSkillPriority, 1, MaxPriority, Strings.HighestSkillPriority,
                    Strings.HighestSkillPriorityTooltip);
            }
            listing.Outdent(16f);
            listing.ColumnWidth += 16f;
            listing.CheckboxLabeled(Strings.UsePawnLearningRateThresholds, ref UsePawnLearningRateThresholds,
                Strings.UsePawnLearningRateThresholdsTooltip);
            if (UsePawnLearningRateThresholds)
            {
                if (MajorLearningRateThreshold > 1f) { MajorLearningRateThreshold = 1f; }
                if (MinorLearningRateThreshold > 1f) { MinorLearningRateThreshold = 1f; }
            }
            DoPercentSlider(listing, ref MajorLearningRateThreshold, MinorLearningRateThreshold,
                UsePawnLearningRateThresholds ? 1f : 2f, Strings.MajorLearningRateThreshold,
                Strings.MajorLearningRateThresholdTooltip);
            DoIntegerSlider(listing, ref MajorLearningRatePriority, 1, MaxPriority, Strings.MajorLearningRatePriority,
                Strings.MajorLearningRatePriorityTooltip);
            DoPercentSlider(listing, ref MinorLearningRateThreshold, 0.01f, MajorLearningRateThreshold,
                Strings.MinorLearningRateThreshold, Strings.MinorLearningRateThresholdTooltip);
            DoIntegerSlider(listing, ref MinorLearningRatePriority, 1, MaxPriority, Strings.MinorLearningRatePriority,
                Strings.MinorLearningRatePriorityTooltip);
            listing.CheckboxLabeled(Strings.RecoveringPawnsUnfitForWork, ref RecoveringPawnsUnfitForWork,
                Strings.RecoveringPawnsUnfitForWorkTooltip);
            listing.CheckboxLabeled(Strings.AssignAllWorkTypes, ref AssignAllWorkTypes,
                Strings.AssignAllWorkTypesTooltip);
            if (AssignAllWorkTypes)
            {
                listing.Indent(16f);
                listing.ColumnWidth -= 16f;
                DoIntegerSlider(listing, ref LeftoverPriority, 1, MaxPriority, Strings.LeftoverPriority,
                    Strings.LeftoverPriorityTooltip);
                listing.Outdent(16f);
                listing.ColumnWidth += 16f;
            }
            listing.CheckboxLabeled(Strings.AssignWorkToIdlePawns, ref AssignWorkToIdlePawns,
                Strings.AssignWorkToIdlePawnsTooltip);
            if (AssignWorkToIdlePawns)
            {
                listing.Indent(16f);
                listing.ColumnWidth -= 16f;
                DoIntegerSlider(listing, ref IdlePriority, 1, MaxPriority, Strings.IdlePriority,
                    Strings.IdlePriorityTooltip);
                listing.Outdent(16f);
                listing.ColumnWidth += 16f;
            }
            listing.CheckboxLabeled(Strings.SpecialRulesForDoctors, ref SpecialRulesForDoctors,
                Strings.SpecialRulesForDoctorsTooltip);
            if (SpecialRulesForDoctors)
            {
                listing.Indent(16f);
                listing.ColumnWidth -= 16f;
                DoIntegerSlider(listing, ref DoctoringPriority, 1, MaxPriority, Strings.DoctoringPriority,
                    Strings.DoctoringPriorityTooltip);
                listing.CheckboxLabeled(Strings.AssignMultipleDoctors, ref AssignMultipleDoctors,
                    Strings.AssignMultipleDoctorsTooltip);
                if (AssignMultipleDoctors)
                {
                    listing.Indent(16f);
                    listing.ColumnWidth -= 16f;
                    listing.CheckboxLabeled(Strings.CountDownedColonists, ref CountDownedColonists,
                        Strings.CountDownedColonistsTooltip);
                    listing.CheckboxLabeled(Strings.CountDownedGuests, ref CountDownedGuests,
                        Strings.CountDownedGuestsTooltip);
                    listing.CheckboxLabeled(Strings.CountDownedPrisoners, ref CountDownedPrisoners,
                        Strings.CountDownedPrisonersTooltip);
                    listing.CheckboxLabeled(Strings.CountDownedAnimals, ref CountDownedAnimals,
                        Strings.CountDownedAnimalsTooltip);
                    listing.Outdent(16f);
                    listing.ColumnWidth += 16f;
                }
                listing.Outdent(16f);
                listing.ColumnWidth += 16f;
            }
            listing.CheckboxLabeled(Strings.SpecialRulesForHunters, ref SpecialRulesForHunters,
                Strings.SpecialRulesForHuntersTooltip);
            if (SpecialRulesForHunters)
            {
                listing.Indent(16f);
                listing.ColumnWidth -= 16f;
                listing.CheckboxLabeled(Strings.AllowMeleeHunters, ref AllowMeleeHunters,
                    Strings.AllowMeleeHuntersTooltip);
                listing.Outdent(16f);
                listing.ColumnWidth += 16f;
            }
            listing.End();
            Widgets.EndScrollView();
        }

        private static void ExposeWorkPrioritiesData()
        {
            Scribe_Values.Look(ref UpdateFrequency, nameof(UpdateFrequency), 24);
            Scribe_Values.Look(ref UsePawnLearningRateThresholds, nameof(UsePawnLearningRateThresholds));
            Scribe_Values.Look(ref MajorLearningRateThreshold, nameof(MajorLearningRateThreshold), 1.2f);
            Scribe_Values.Look(ref MinorLearningRateThreshold, nameof(MinorLearningRateThreshold), 0.8f);
            Scribe_Values.Look(ref UseDedicatedWorkers, nameof(UseDedicatedWorkers), true);
            Scribe_Values.Look(ref SpecialRulesForDoctors, nameof(SpecialRulesForDoctors), true);
            Scribe_Values.Look(ref AssignMultipleDoctors, nameof(AssignMultipleDoctors), true);
            Scribe_Values.Look(ref CountDownedColonists, nameof(CountDownedColonists), true);
            Scribe_Values.Look(ref CountDownedGuests, nameof(CountDownedGuests), true);
            Scribe_Values.Look(ref CountDownedPrisoners, nameof(CountDownedPrisoners), true);
            Scribe_Values.Look(ref CountDownedAnimals, nameof(CountDownedAnimals), true);
            Scribe_Values.Look(ref RecoveringPawnsUnfitForWork, nameof(RecoveringPawnsUnfitForWork), true);
            Scribe_Values.Look(ref SpecialRulesForHunters, nameof(SpecialRulesForHunters), true);
            Scribe_Values.Look(ref AllowMeleeHunters, nameof(AllowMeleeHunters));
            Scribe_Values.Look(ref AssignAllWorkTypes, nameof(AssignAllWorkTypes));
            Scribe_Values.Look(ref AssignWorkToIdlePawns, nameof(AssignWorkToIdlePawns), true);
            Scribe_Values.Look(ref DedicatedWorkerPriority, nameof(DedicatedWorkerPriority), 1);
            Scribe_Values.Look(ref HighestSkillPriority, nameof(HighestSkillPriority), 1);
            Scribe_Values.Look(ref MajorLearningRatePriority, nameof(MajorLearningRatePriority), 2);
            Scribe_Values.Look(ref MinorLearningRatePriority, nameof(MinorLearningRatePriority), 3);
            Scribe_Values.Look(ref IdlePriority, nameof(IdlePriority), 4);
            Scribe_Values.Look(ref LeftoverPriority, nameof(LeftoverPriority), 4);
            Scribe_Values.Look(ref DoctoringPriority, nameof(DoctoringPriority), 1);
        }

        private static void InitializeWorkPriorities()
        {
            if (UpdateFrequency == 0) { UpdateFrequency = 24; }
            if (DedicatedWorkerPriority == 0) { DedicatedWorkerPriority = 1; }
            if (HighestSkillPriority == 0) { HighestSkillPriority = 1; }
            if (MajorLearningRateThreshold == 0) { MajorLearningRateThreshold = 1.2f; }
            if (MajorLearningRatePriority == 0) { MajorLearningRatePriority = 2; }
            if (MinorLearningRateThreshold == 0) { MinorLearningRateThreshold = 0.8f; }
            if (MinorLearningRatePriority == 0) { MinorLearningRatePriority = 3; }
            if (IdlePriority == 0) { IdlePriority = 4; }
            if (LeftoverPriority == 0) { LeftoverPriority = 4; }
            if (DoctoringPriority == 0) { DoctoringPriority = 1; }
        }
    }
}