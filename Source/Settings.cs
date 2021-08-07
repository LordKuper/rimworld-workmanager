﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Verse;
using Strings = WorkManager.Resources.Strings.Settings;

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
        public static bool Initialized;

        public static List<AssignEveryoneWorkType> AssignEveryoneWorkTypes =
            new List<AssignEveryoneWorkType>(DefaultAssignEveryoneWorkTypes);

        private static SettingsTabs _currentTab;

        public static bool AssignMultipleDoctors = true;
        public static bool AssignWorkToIdlePawns = true;
        public static bool CountDownedAnimals = true;
        public static bool CountDownedColonists = true;
        public static bool CountDownedGuests = true;
        public static bool CountDownedPrisoners = true;

        internal static MethodInfo IsBadWorkMethod;
        public static float MajorLearningRateThreshold = 1.2f;
        public static bool ManageWorkSchedule = true;
        public static float MinorLearningRateThreshold = 0.8f;
        public static bool RecoveringPawnsUnfitForWork = true;

        public static bool SpecialRulesForDoctors = true;
        public static bool SpecialRulesForHunters = true;

        private static readonly List<TabRecord> Tabs = new List<TabRecord>();
        public static int UpdateFrequency = 24;

        public static bool UseDedicatedWorkers = true;

        public static bool UsePawnLearningRateThresholds;
        public static bool VerboseLogging;

        private static IEnumerable<AssignEveryoneWorkType> DefaultAssignEveryoneWorkTypes =>
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

        private static void DoColumnsHeaders(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            var nameRect = new Rect(rect.x, rect.y, _nameColumnWidth, rect.height);
            Widgets.Label(nameRect, Strings.WorkTypes.WorkTypeName);
            TooltipHandler.TipRegion(nameRect, Strings.WorkTypes.WorkTypeNameTooltip);
            Text.Anchor = TextAnchor.MiddleCenter;
            var priorityRect = new Rect(nameRect.xMax, rect.y, _priorityColumnWidth, rect.height);
            Widgets.Label(priorityRect, Strings.WorkTypes.WorkTypePriority);
            TooltipHandler.TipRegion(priorityRect, Strings.WorkTypes.WorkTypePriorityTooltip);
            var allowDedicatedRect = new Rect(priorityRect.xMax, rect.y, _allowDedicatedColumnWidth, rect.height);
            Widgets.Label(allowDedicatedRect, Strings.WorkTypes.AllowDedicated);
            TooltipHandler.TipRegion(allowDedicatedRect, Strings.WorkTypes.AllowDedicatedTooltip);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DoMiscTab(Rect rect)
        {
            var listing = new Listing_Standard();
            const int boolSettingsCount = 1;
            var staticSettingsHeight = boolSettingsCount * (Text.LineHeight + listing.verticalSpacing);
            var viewRect = new Rect(rect.x, 0, rect.width - 16f, staticSettingsHeight);
            Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
            listing.Begin(viewRect);
            listing.CheckboxLabeled(Strings.Misc.VerboseLogging, ref VerboseLogging,
                Strings.Misc.VerboseLoggingTooltip);
            listing.End();
            Widgets.EndScrollView();
        }

        private static void DoPrioritiesTab(Rect rect)
        {
            var listing = new Listing_Standard();
            const int boolSettingsCount = 13;
            const int numericSettingsCount = 3;
            var viewRectHeight = boolSettingsCount * (Text.LineHeight + listing.verticalSpacing) +
                                 numericSettingsCount * (Text.LineHeight * 1.5f + listing.verticalSpacing);
            var viewRect = new Rect(rect.x, 0, rect.width - 16f, viewRectHeight);
            Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
            listing.Begin(viewRect);
            var optionRect = listing.GetRect(Text.LineHeight * 1.5f);
            var fieldRect = optionRect;
            var labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 2f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, Strings.Priorities.UpdateIntervalTooltip);
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, Strings.Priorities.UpdateInterval);
            UpdateFrequency = (int)Widgets.HorizontalSlider(fieldRect.ContractedBy(4f), UpdateFrequency, 1f, 120f, true,
                UpdateFrequency.ToString(), roundTo: 1);
            listing.Gap(listing.verticalSpacing);
            listing.CheckboxLabeled(Strings.Priorities.UsePawnLearningRateThresholds, ref UsePawnLearningRateThresholds,
                Strings.Priorities.UsePawnLearningRateThresholdsTooltip);
            optionRect = listing.GetRect(Text.LineHeight * 1.5f);
            fieldRect = optionRect;
            labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 2f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, Strings.Priorities.MajorLearningRateThresholdTooltip);
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, Strings.Priorities.MajorLearningRateThreshold);
            MajorLearningRateThreshold = Widgets.HorizontalSlider(fieldRect.ContractedBy(4f),
                MajorLearningRateThreshold, MinorLearningRateThreshold, UsePawnLearningRateThresholds ? 1f : 2f, true,
                MajorLearningRateThreshold.ToStringPercent());
            listing.Gap(listing.verticalSpacing);
            optionRect = listing.GetRect(Text.LineHeight * 1.5f);
            fieldRect = optionRect;
            labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 2f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, Strings.Priorities.MinorLearningRateThresholdTooltip);
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, Strings.Priorities.MinorLearningRateThreshold);
            MinorLearningRateThreshold = Widgets.HorizontalSlider(fieldRect.ContractedBy(4f),
                MinorLearningRateThreshold, 0.01f, MajorLearningRateThreshold, true,
                MinorLearningRateThreshold.ToStringPercent());
            listing.Gap(listing.verticalSpacing);
            listing.CheckboxLabeled(Strings.Priorities.UseDedicatedWorkers, ref UseDedicatedWorkers,
                Strings.Priorities.UseDedicatedWorkersTooltip);
            listing.CheckboxLabeled(Strings.Priorities.RecoveringPawnsUnfitForWork, ref RecoveringPawnsUnfitForWork,
                Strings.Priorities.RecoveringPawnsUnfitForWorkTooltip);
            listing.CheckboxLabeled(Strings.Priorities.AssignAllWorkTypes, ref AssignAllWorkTypes,
                Strings.Priorities.AssignAllWorkTypesTooltip);
            listing.CheckboxLabeled(Strings.Priorities.AssignWorkToIdlePawns, ref AssignWorkToIdlePawns,
                Strings.Priorities.AssignWorkToIdlePawnsTooltip);
            listing.CheckboxLabeled(Strings.Priorities.SpecialRulesForDoctors, ref SpecialRulesForDoctors,
                Strings.Priorities.SpecialRulesForDoctorsTooltip);
            listing.Indent(16f);
            listing.ColumnWidth -= 16f;
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Strings.Priorities.AssignMultipleDoctorsTooltip);
            Widgets.CheckboxLabeled(optionRect, Strings.Priorities.AssignMultipleDoctors, ref AssignMultipleDoctors,
                !SpecialRulesForDoctors);
            listing.Gap(listing.verticalSpacing);
            listing.Indent(16f);
            listing.ColumnWidth -= 16f;
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Strings.Priorities.CountDownedColonistsTooltip);
            Widgets.CheckboxLabeled(optionRect, Strings.Priorities.CountDownedColonists, ref CountDownedColonists,
                !(SpecialRulesForDoctors && AssignMultipleDoctors));
            listing.Gap(listing.verticalSpacing);
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Strings.Priorities.CountDownedGuestsTooltip);
            Widgets.CheckboxLabeled(optionRect, Strings.Priorities.CountDownedGuests, ref CountDownedGuests,
                !(SpecialRulesForDoctors && AssignMultipleDoctors));
            listing.Gap(listing.verticalSpacing);
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Strings.Priorities.CountDownedPrisonersTooltip);
            Widgets.CheckboxLabeled(optionRect, Strings.Priorities.CountDownedPrisoners, ref CountDownedPrisoners,
                !(SpecialRulesForDoctors && AssignMultipleDoctors));
            listing.Gap(listing.verticalSpacing);
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Strings.Priorities.CountDownedAnimalsTooltip);
            Widgets.CheckboxLabeled(optionRect, Strings.Priorities.CountDownedAnimals, ref CountDownedAnimals,
                !(SpecialRulesForDoctors && AssignMultipleDoctors));
            listing.Gap(listing.verticalSpacing);
            listing.Outdent(32f);
            listing.ColumnWidth += 32f;
            listing.CheckboxLabeled(Strings.Priorities.SpecialRulesForHunters, ref SpecialRulesForHunters,
                Strings.Priorities.SpecialRulesForHuntersTooltip);
            listing.Indent(16f);
            listing.ColumnWidth -= 16f;
            optionRect = listing.GetRect(Text.LineHeight);
            Widgets.DrawHighlightIfMouseover(optionRect);
            TooltipHandler.TipRegion(optionRect, Strings.Priorities.AllowMeleeHuntersTooltip);
            Widgets.CheckboxLabeled(optionRect, Strings.Priorities.AllowMeleeHunters, ref AllowMeleeHunters,
                !SpecialRulesForHunters);
            listing.Gap(listing.verticalSpacing);
            listing.Outdent(16f);
            listing.ColumnWidth += 16f;
            listing.End();
            Widgets.EndScrollView();
        }

        private static void DoScheduleTab(Rect rect)
        {
            var listing = new Listing_Standard();
            const int boolSettingsCount = 1;
            const int numericSettingsCount = 0;
            var staticSettingsHeight = boolSettingsCount * (Text.LineHeight + listing.verticalSpacing) +
                                       numericSettingsCount * (Text.LineHeight * 1.5f + listing.verticalSpacing);
            var viewRect = new Rect(rect.x, 0, rect.width - 16f, staticSettingsHeight);
            Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
            listing.Begin(viewRect);
            listing.CheckboxLabeled(Strings.Schedule.ManageWorkSchedule, ref ManageWorkSchedule,
                Strings.Schedule.ManageWorkScheduleTooltip);
            listing.End();
            Widgets.EndScrollView();
        }

        public static void DoWindowContents(Rect rect)
        {
            if (!Initialized) { Initialize(); }
            var tabDrawerRect = rect;
            tabDrawerRect.yMin += 32f;
            TabDrawer.DrawTabs(tabDrawerRect, Tabs, 500f);
            var activeTabRect = tabDrawerRect.ContractedBy(10);
            activeTabRect.xMax += 6f;
            switch (_currentTab)
            {
                case SettingsTabs.Priorities:
                    DoPrioritiesTab(activeTabRect);
                    break;
                case SettingsTabs.WorkTypes:
                    DoWorkTypesTab(activeTabRect);
                    break;
                case SettingsTabs.Schedule:
                    DoScheduleTab(activeTabRect);
                    break;
                case SettingsTabs.Misc:
                    DoMiscTab(activeTabRect);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void DoWorkTypeRow(Rect rect, AssignEveryoneWorkType workType)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            var color = GUI.color;
            var nameRect = new Rect(rect.x, rect.y, _nameColumnWidth, rect.height).ContractedBy(4f);
            if (!workType.IsWorkTypeLoaded)
            {
                TooltipHandler.TipRegion(nameRect, Strings.WorkTypes.WorkTypeNotLoadedTooltip);
                GUI.color = Color.red;
            }
            Widgets.Label(nameRect, workType.Label);
            GUI.color = color;
            workType.Priority = (int)Widgets.HorizontalSlider(
                new Rect(rect.x + _nameColumnWidth, rect.y, _priorityColumnWidth, rect.height).ContractedBy(4f),
                workType.Priority, 1f, 4f, true, workType.Priority.ToString(), roundTo: 1);
            Widgets.Checkbox(rect.x + _nameColumnWidth + _priorityColumnWidth + (_allowDedicatedColumnWidth / 2f - 12),
                rect.y, ref workType.AllowDedicated, 24f, paintable: true);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DoWorkTypesTab(Rect rect)
        {
            var listing = new Listing_Standard();
            var viewRectHeight = Text.LineHeight + 35f + Text.LineHeight * 2 + Text.SpaceBetweenLines +
                                 AssignEveryoneWorkTypes.Count * 35f;
            var viewRect = new Rect(rect.x, 0, rect.width - 16f, viewRectHeight);
            Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
            listing.Begin(viewRect);
            var headerRect = listing.GetRect(Text.LineHeight);
            TooltipHandler.TipRegion(headerRect, Strings.WorkTypes.AssignEveryoneWorkTypesTooltip);
            Widgets.Label(headerRect, Strings.WorkTypes.AssignEveryoneWorkTypes);
            var buttonRowRect = listing.GetRect(35f);
            var buttonRect = new Rect(buttonRowRect.x, buttonRowRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Strings.WorkTypes.AddAssignEveryoneWorkType))
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
            if (Widgets.ButtonText(buttonRect, Strings.WorkTypes.DeleteAssignEveryoneWorkType))
            {
                Find.WindowStack.Add(new FloatMenu(AssignEveryoneWorkTypes.OrderBy(workType => workType.Label)
                    .Select(workType =>
                        new FloatMenuOption(workType.Label, () => { AssignEveryoneWorkTypes.Remove(workType); }))
                    .ToList()));
            }
            buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Strings.WorkTypes.ResetAssignEveryoneWorkTypes))
            {
                AssignEveryoneWorkTypes.Clear();
                AssignEveryoneWorkTypes.AddRange(DefaultAssignEveryoneWorkTypes);
            }
            var columnHeadersRect = listing.GetRect(Text.LineHeight * 2 + Text.SpaceBetweenLines);
            CalculateColumnsWidths(columnHeadersRect);
            DoColumnsHeaders(columnHeadersRect);
            foreach (var workType in AssignEveryoneWorkTypes) { DoWorkTypeRow(listing.GetRect(35f), workType); }
            listing.End();
            Widgets.EndScrollView();
        }

        public override void ExposeData()
        {
            base.ExposeData();
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
            Scribe_Values.Look(ref VerboseLogging, nameof(VerboseLogging));
            Scribe_Collections.Look(ref AssignEveryoneWorkTypes, nameof(AssignEveryoneWorkTypes), LookMode.Deep);
            Scribe_Values.Look(ref ManageWorkSchedule, nameof(ManageWorkSchedule), true);
        }

        public static void Initialize()
        {
            if (Initialized) { return; }
            if (AssignEveryoneWorkTypes == null)
            {
                AssignEveryoneWorkTypes = new List<AssignEveryoneWorkType>(DefaultAssignEveryoneWorkTypes);
            }
            if (UpdateFrequency == 0) { UpdateFrequency = 24; }
            if (!Tabs.Any())
            {
                Tabs.Add(new TabRecord(Strings.Priorities.Title, () =>
                {
                    _currentTab = SettingsTabs.Priorities;
                    _scrollPosition.Set(0, 0);
                }, () => _currentTab == SettingsTabs.Priorities));
                Tabs.Add(new TabRecord(Strings.WorkTypes.Title, () =>
                {
                    _currentTab = SettingsTabs.WorkTypes;
                    _scrollPosition.Set(0, 0);
                }, () => _currentTab == SettingsTabs.WorkTypes));
                Tabs.Add(new TabRecord(Strings.Schedule.Title, () =>
                {
                    _currentTab = SettingsTabs.Schedule;
                    _scrollPosition.Set(0, 0);
                }, () => _currentTab == SettingsTabs.Schedule));
                Tabs.Add(new TabRecord(Strings.Misc.Title, () =>
                {
                    _currentTab = SettingsTabs.Misc;
                    _scrollPosition.Set(0, 0);
                }, () => _currentTab == SettingsTabs.Misc));
            }
            Initialized = true;
        }
    }
}