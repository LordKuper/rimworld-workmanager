using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;

namespace WorkManager
{
    public class Settings : ModSettings
    {
        private static float _allowDedicatedColumnWidth;

        private static float _nameColumnWidth;
        private static float _priorityColumnWidth;
        private static Vector2 _scrollPosition;
        public static bool AllowMeleeHunters;
        public static bool AssignAllWorkTypes;

        public static List<AssignEveryoneWorkType> AssignEveryoneWorkTypes =
            new List<AssignEveryoneWorkType>(DefaultAssignEveryoneWorkTypes);

        public static bool AssignMultipleDoctors = true;
        public static bool AssignWorkToIdlePawns = true;
        public static bool CountDownedAnimals = true;
        public static bool CountDownedColonists = true;
        public static bool CountDownedGuests = true;
        public static bool CountDownedPrisoners = true;
        internal static MethodInfo IsBadWorkMethod;
        public static float MajorLearningRateThreshold = 0.75f;
        public static float MinorLearningRateThreshold = 0.5f;
        public static bool RecoveringPawnsUnfitForWork = true;

        public static bool SpecialRulesForDoctors = true;
        public static bool SpecialRulesForHunters = true;
        public static int UpdateFrequency = 24;

        public static bool UseDedicatedWorkers = true;

        public static bool UsePawnLearningRateThresholds = true;
        public static bool VerboseLogging;

        public static IEnumerable<AssignEveryoneWorkType> DefaultAssignEveryoneWorkTypes =>
            new[]
            {
                new AssignEveryoneWorkType("Firefighter", 1, false),
                new AssignEveryoneWorkType("Patient", 1, false),
                new AssignEveryoneWorkType("PatientBedRest", 1, false),
                new AssignEveryoneWorkType("BasicWorker", 1, false), new AssignEveryoneWorkType("Hauling", 4, true),
                new AssignEveryoneWorkType("Cleaning", 4, true)
            };

        private static void CalculateColumnsWidths(Rect rect)
        {
            _priorityColumnWidth = rect.width * 0.25f;
            _allowDedicatedColumnWidth = rect.width * 0.1f;
            _nameColumnWidth = rect.width - (_priorityColumnWidth + _allowDedicatedColumnWidth);
        }

        private static void DoAssignEveryoneWorkTypesArea(Listing listing)
        {
            if (AssignEveryoneWorkTypes == null)
            {
                AssignEveryoneWorkTypes = new List<AssignEveryoneWorkType>(DefaultAssignEveryoneWorkTypes);
            }
            var headerRect = listing.GetRect(Text.LineHeight);
            TooltipHandler.TipRegion(headerRect, Resources.Strings.AssignEveryoneWorkTypesTooltip);
            Widgets.Label(headerRect, Resources.Strings.AssignEveryoneWorkTypes);
            var buttonRowRect = listing.GetRect(35f);
            var buttonRect = new Rect(buttonRowRect.x, buttonRowRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Resources.Strings.AddAssignEveryoneWorkType))
            {
                var options = new List<FloatMenuOption>();
                foreach (var workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w =>
                        w.visible && !AssignEveryoneWorkTypes.Any(workType =>
                            workType.WorkTypeDefName.Equals(w.defName, StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(workTypeDef => workTypeDef.labelShort))
                {
                    options.Add(new FloatMenuOption(workTypeDef.labelShort,
                        () =>
                        {
                            AssignEveryoneWorkTypes.Add(new AssignEveryoneWorkType(workTypeDef.defName, 1, false));
                        }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Resources.Strings.DeleteAssignEveryoneWorkType))
            {
                Find.WindowStack.Add(new FloatMenu(AssignEveryoneWorkTypes.OrderBy(workType => workType.Label)
                    .Select(workType =>
                        new FloatMenuOption(workType.Label, () => { AssignEveryoneWorkTypes.Remove(workType); }))
                    .ToList()));
            }
            buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Resources.Strings.ResetAssignEveryoneWorkTypes))
            {
                AssignEveryoneWorkTypes.Clear();
                AssignEveryoneWorkTypes.AddRange(DefaultAssignEveryoneWorkTypes);
            }
            var columnHeadersRect = listing.GetRect(Text.LineHeight * 2 + Text.SpaceBetweenLines);
            CalculateColumnsWidths(columnHeadersRect);
            DoColumnsHeaders(columnHeadersRect);
            foreach (var workType in AssignEveryoneWorkTypes) { DoWorkTypeRow(listing.GetRect(35f), workType); }
        }

        private static void DoColumnsHeaders(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            var nameRect = new Rect(rect.x, rect.y, _nameColumnWidth, rect.height);
            Widgets.Label(nameRect, Resources.Strings.WorkTypeName);
            TooltipHandler.TipRegion(nameRect, Resources.Strings.WorkTypeNameTooltip);
            Text.Anchor = TextAnchor.MiddleCenter;
            var priorityRect = new Rect(nameRect.xMax, rect.y, _priorityColumnWidth, rect.height);
            Widgets.Label(priorityRect, Resources.Strings.WorkTypePriority);
            TooltipHandler.TipRegion(priorityRect, Resources.Strings.WorkTypePriorityTooltip);
            var allowDedicatedRect = new Rect(priorityRect.xMax, rect.y, _allowDedicatedColumnWidth, rect.height);
            Widgets.Label(allowDedicatedRect, Resources.Strings.AllowDedicated);
            TooltipHandler.TipRegion(allowDedicatedRect, Resources.Strings.AllowDedicatedTooltip);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public static void DoWindowContents(Rect rect)
        {
            if (AssignEveryoneWorkTypes == null)
            {
                AssignEveryoneWorkTypes = new List<AssignEveryoneWorkType>(DefaultAssignEveryoneWorkTypes);
            }
            if (UpdateFrequency == 0) { UpdateFrequency = 24; }
            var listing = new Listing_Standard();
            const int boolSettingsCount = 14;
            const int numericSettingsCount = 3;
            var staticSettingsHeight = boolSettingsCount * (Text.LineHeight + listing.verticalSpacing) +
                                       numericSettingsCount * (Text.LineHeight * 1.5f + listing.verticalSpacing);
            var viewRectHeight = staticSettingsHeight + 12f + Text.LineHeight + 35f + Text.LineHeight * 2 +
                                 Text.SpaceBetweenLines + AssignEveryoneWorkTypes.Count * 35f;
            var viewRect = new Rect(rect.x, 0, rect.width - 16f, viewRectHeight);
            listing.BeginScrollView(rect, ref _scrollPosition, ref viewRect);
            var optionRect = listing.GetRect(Text.LineHeight * 1.5f);
            var fieldRect = optionRect;
            var labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 2f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, Resources.Strings.UpdateIntervalTooltip);
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, Resources.Strings.UpdateInterval);
            UpdateFrequency = (int) Widgets.HorizontalSlider(fieldRect.ContractedBy(4f), UpdateFrequency, 1f, 120f,
                true, UpdateFrequency.ToString(), roundTo: 1);
            listing.Gap(listing.verticalSpacing);
            listing.CheckboxLabeled(Resources.Strings.UsePawnLearningRateThresholds, ref UsePawnLearningRateThresholds,
                Resources.Strings.UsePawnLearningRateThresholdsTooltip);
            optionRect = listing.GetRect(Text.LineHeight * 1.5f);
            fieldRect = optionRect;
            labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 2f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, Resources.Strings.MajorLearningRateThresholdTooltip);
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, Resources.Strings.MajorLearningRateThreshold);
            MajorLearningRateThreshold = Widgets.HorizontalSlider(fieldRect.ContractedBy(4f),
                MajorLearningRateThreshold, MinorLearningRateThreshold, UsePawnLearningRateThresholds ? 1f : 2f, true,
                MajorLearningRateThreshold.ToStringPercent());
            listing.Gap(listing.verticalSpacing);
            optionRect = listing.GetRect(Text.LineHeight * 1.5f);
            fieldRect = optionRect;
            labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 2f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, Resources.Strings.MinorLearningRateThresholdTooltip);
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, Resources.Strings.MinorLearningRateThreshold);
            MinorLearningRateThreshold = Widgets.HorizontalSlider(fieldRect.ContractedBy(4f),
                MinorLearningRateThreshold, 0.01f, MajorLearningRateThreshold, true,
                MinorLearningRateThreshold.ToStringPercent());
            listing.Gap(listing.verticalSpacing);
            listing.CheckboxLabeled(Resources.Strings.UseDedicatedWorkers, ref UseDedicatedWorkers,
                Resources.Strings.UseDedicatedWorkersTooltip);
            listing.CheckboxLabeled(Resources.Strings.RecoveringPawnsUnfitForWork, ref RecoveringPawnsUnfitForWork,
                Resources.Strings.RecoveringPawnsUnfitForWorkTooltip);
            listing.CheckboxLabeled(Resources.Strings.AssignAllWorkTypes, ref AssignAllWorkTypes,
                Resources.Strings.AssignAllWorkTypesTooltip);
            listing.CheckboxLabeled(Resources.Strings.AssignWorkToIdlePawns, ref AssignWorkToIdlePawns,
                Resources.Strings.AssignWorkToIdlePawnsTooltip);
            listing.CheckboxLabeled(Resources.Strings.SpecialRulesForDoctors, ref SpecialRulesForDoctors,
                Resources.Strings.SpecialRulesForDoctorsTooltip);
            listing.Indent(16f);
            listing.ColumnWidth -= 16f;
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Resources.Strings.AssignMultipleDoctorsTooltip);
            Widgets.CheckboxLabeled(optionRect, Resources.Strings.AssignMultipleDoctors, ref AssignMultipleDoctors,
                !SpecialRulesForDoctors);
            listing.Gap(listing.verticalSpacing);
            listing.Indent(16f);
            listing.ColumnWidth -= 16f;
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Resources.Strings.CountDownedColonistsTooltip);
            Widgets.CheckboxLabeled(optionRect, Resources.Strings.CountDownedColonists, ref CountDownedColonists,
                !(SpecialRulesForDoctors && AssignMultipleDoctors));
            listing.Gap(listing.verticalSpacing);
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Resources.Strings.CountDownedGuestsTooltip);
            Widgets.CheckboxLabeled(optionRect, Resources.Strings.CountDownedGuests, ref CountDownedGuests,
                !(SpecialRulesForDoctors && AssignMultipleDoctors));
            listing.Gap(listing.verticalSpacing);
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Resources.Strings.CountDownedPrisonersTooltip);
            Widgets.CheckboxLabeled(optionRect, Resources.Strings.CountDownedPrisoners, ref CountDownedPrisoners,
                !(SpecialRulesForDoctors && AssignMultipleDoctors));
            listing.Gap(listing.verticalSpacing);
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Resources.Strings.CountDownedAnimalsTooltip);
            Widgets.CheckboxLabeled(optionRect, Resources.Strings.CountDownedAnimals, ref CountDownedAnimals,
                !(SpecialRulesForDoctors && AssignMultipleDoctors));
            listing.Gap(listing.verticalSpacing);
            listing.Outdent(32f);
            listing.ColumnWidth += 32f;
            listing.CheckboxLabeled(Resources.Strings.SpecialRulesForHunters, ref SpecialRulesForHunters,
                Resources.Strings.SpecialRulesForHuntersTooltip);
            listing.Indent(16f);
            listing.ColumnWidth -= 16f;
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Resources.Strings.AllowMeleeHuntersTooltip);
            Widgets.CheckboxLabeled(optionRect, Resources.Strings.AllowMeleeHunters, ref AllowMeleeHunters,
                !SpecialRulesForHunters);
            listing.Gap(listing.verticalSpacing);
            listing.Outdent(16f);
            listing.ColumnWidth += 16f;
            listing.CheckboxLabeled(Resources.Strings.VerboseLogging, ref VerboseLogging,
                Resources.Strings.VerboseLoggingTooltip);
            listing.GapLine();
            DoAssignEveryoneWorkTypesArea(listing);
            listing.EndScrollView(ref viewRect);
        }

        private static void DoWorkTypeRow(Rect rect, AssignEveryoneWorkType workType)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            var color = GUI.color;
            var nameRect = new Rect(rect.x, rect.y, _nameColumnWidth, rect.height).ContractedBy(4f);
            if (!workType.IsWorkTypeLoaded)
            {
                TooltipHandler.TipRegion(nameRect, Resources.Strings.WorkTypeNotLoadedTooltip);
                GUI.color = Color.red;
            }
            Widgets.Label(nameRect, workType.Label);
            GUI.color = color;
            workType.Priority = (int) Widgets.HorizontalSlider(
                new Rect(rect.x + _nameColumnWidth, rect.y, _priorityColumnWidth, rect.height).ContractedBy(4f),
                workType.Priority, 1f, 4f, true, workType.Priority.ToString(), roundTo: 1);
            Widgets.Checkbox(rect.x + _nameColumnWidth + _priorityColumnWidth + (_allowDedicatedColumnWidth / 2f - 12),
                rect.y, ref workType.AllowDedicated, 24f, paintable: true);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref UpdateFrequency, nameof(UpdateFrequency), 24);
            Scribe_Values.Look(ref UsePawnLearningRateThresholds, nameof(UsePawnLearningRateThresholds), true);
            Scribe_Values.Look(ref MajorLearningRateThreshold, nameof(MajorLearningRateThreshold), 0.75f);
            Scribe_Values.Look(ref MinorLearningRateThreshold, nameof(MinorLearningRateThreshold), 0.5f);
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
            Scribe_Values.Look(ref VerboseLogging, nameof(VerboseLogging));
            Scribe_Collections.Look(ref AssignEveryoneWorkTypes, nameof(AssignEveryoneWorkTypes), LookMode.Deep);
        }
    }
}