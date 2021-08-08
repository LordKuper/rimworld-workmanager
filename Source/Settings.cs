using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Verse;
using Strings = WorkManager.Resources.Strings.Settings;

namespace WorkManager
{
    public partial class Settings : ModSettings
    {
        private static SettingsTabs _currentTab;
        private static Vector2 _scrollPosition;

        internal static MethodInfo GetPriorityMethod;
        public static bool Initialized;

        internal static MethodInfo IsBadWorkMethod;

        internal static int MaxPriority = 4;

        internal static MethodInfo SetPriorityMethod;
        private static readonly List<TabRecord> Tabs = new List<TabRecord>();

        private static void DoIntegerSlider(Listing listing, ref int value, int minValue, int maxValue, string label,
            string tooltip)
        {
            var optionRect = listing.GetRect(Text.LineHeight * 1.5f);
            var fieldRect = optionRect;
            var labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 2f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, tooltip);
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, label);
            value = (int)Widgets.HorizontalSlider(fieldRect.ContractedBy(4f), value, minValue, maxValue, true,
                value.ToString(), roundTo: 1);
            listing.Gap(listing.verticalSpacing);
        }

        private static void DoPercentSlider(Listing listing, ref float value, float minValue, float maxValue,
            string label, string tooltip)
        {
            var optionRect = listing.GetRect(Text.LineHeight * 1.5f);
            var fieldRect = optionRect;
            var labelRect = optionRect;
            fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 2f);
            labelRect.xMax = fieldRect.xMin;
            TooltipHandler.TipRegion(optionRect, tooltip);
            Widgets.DrawHighlightIfMouseover(optionRect);
            Widgets.Label(labelRect, label);
            value = Widgets.HorizontalSlider(fieldRect.ContractedBy(4f), value, minValue, maxValue, true,
                value.ToStringPercent());
            listing.Gap(listing.verticalSpacing);
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

        public override void ExposeData()
        {
            base.ExposeData();
            ExposeWorkPrioritiesData();
            ExposeMiscData();
            ExposeWorkTypesData();
            ExposeSchedulesData();
        }

        public static void Initialize()
        {
            if (Initialized) { return; }
            InitializeTabs();
            InitializeWorkPriorities();
            InitializeWorkTypes();
            InitializeSchedules();
            Initialized = true;
        }

        private static void InitializeTabs()
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
    }
}