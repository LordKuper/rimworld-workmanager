using System;
using System.Collections.Generic;
using System.Linq;
using LordKuper.WorkManager.Helpers;
using UnityEngine;
using Verse;
using Strings = LordKuper.WorkManager.Resources.Strings.Settings.WorkTypes;

namespace LordKuper.WorkManager.Settings
{
    public partial class Settings
    {
        private static float _assignEveryoneAllowDedicatedColumnWidth;
        private static float _assignEveryonePriorityColumnWidth;
        private static float _assignEveryoneTableWidth;
        private static float _assignEveryoneWorkTypeNameColumnWidth;
        public static List<WorkTypeSettings> WorkTypeSettings = new List<WorkTypeSettings>(DefaultWorkTypeSettings);
        public static WorkTypeSettings SelectedWorkTypeSettings;

        public static List<AssignEveryoneWorkType> AssignEveryoneWorkTypes =
            new List<AssignEveryoneWorkType>(DefaultAssignEveryoneWorkTypes);

        public static List<DisabledWorkType> DisabledWorkTypesForSlaves =
            new List<DisabledWorkType>(DefaultDisabledWorkTypesForSlaves);

        public static List<DisabledWorkType> DisabledWorkTypesForForeigners =
            new List<DisabledWorkType>(DefaultDisabledWorkTypesForForeigners);

        private static IEnumerable<AssignEveryoneWorkType> DefaultAssignEveryoneWorkTypes =>
            new[]
            {
                new AssignEveryoneWorkType("Firefighter", 1, false),
                new AssignEveryoneWorkType("Patient", 1, false),
                new AssignEveryoneWorkType("PatientBedRest", 1, false),
                new AssignEveryoneWorkType("BasicWorker", 1, false), new AssignEveryoneWorkType("Hauling", 4, true),
                new AssignEveryoneWorkType("Cleaning", 4, true)
            };

        private static IEnumerable<DisabledWorkType> DefaultDisabledWorkTypesForForeigners =>
            new[]
            {
                new DisabledWorkType("Art"), new DisabledWorkType("Crafting"), new DisabledWorkType("Tailoring"),
                new DisabledWorkType("Smithing"), new DisabledWorkType("Construction")
            };

        private static IEnumerable<DisabledWorkType> DefaultDisabledWorkTypesForSlaves =>
            new[]
            {
                new DisabledWorkType("Art"), new DisabledWorkType("Crafting"), new DisabledWorkType("Tailoring"),
                new DisabledWorkType("Smithing"), new DisabledWorkType("Construction")
            };

        private static IEnumerable<WorkTypeSettings> DefaultWorkTypeSettings =>
            new[]
            {
                new WorkTypeSettings(),
                new WorkTypeSettings("Doctor")
                {
                    DedicatedWorkersMode = WorkManager.Settings.WorkTypeSettings.DedicatedMode.PawnCount,
                    PawnSettings = new WorkTypePawnSettings
                    {
                        AllowColonists = true,
                        AllowForeigners = true,
                        AllowSlaves = true,
                        AssignEveryone = false,
                        AllowInjured = false,
                        AllowUncontrollable = false,
                        AllowNeedingRest = true,
                        CapacityLimits = new List<PawnCapacityLimit>
                        {
                            new PawnCapacityLimit("Manipulation", 0.5f, null)
                        },
                        WorkCapacities = new Dictionary<string, bool> { { "Caring", true } }
                    }
                }
            };

        private static void DoAssignEveryoneWorkTypes(Listing listing)
        {
            var headerRect = listing.GetRect(Text.LineHeight);
            TooltipHandler.TipRegion(headerRect, Strings.AssignEveryoneWorkTypesTooltip);
            Widgets.Label(headerRect, Strings.AssignEveryoneWorkTypes);
            DoButtonRow();
            DoTable();
            return;

            void DoButtonRow()
            {
                var buttonRowRect = listing.GetRect(35f);
                var buttonRect = new Rect(buttonRowRect.x, buttonRowRect.y, 150f, 35f);
                if (Widgets.ButtonText(buttonRect, Strings.AddWorkType))
                {
                    Find.WindowStack.Add(new FloatMenu(DefDatabase<WorkTypeDef>.AllDefsListForReading
                        .Where(w => w.visible && !Enumerable.Any(AssignEveryoneWorkTypes,
                            workType => workType.WorkTypeDefName.Equals(w.defName, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(wtd => wtd.labelShort).Select(wtd => new FloatMenuOption(wtd.labelShort,
                            () => { AssignEveryoneWorkTypes.Add(new AssignEveryoneWorkType(wtd.defName, 1, false)); }))
                        .ToList()));
                }
                buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
                if (Widgets.ButtonText(buttonRect, Strings.DeleteWorkType, active: AssignEveryoneWorkTypes.Any()))
                {
                    Find.WindowStack.Add(new FloatMenu(AssignEveryoneWorkTypes.OrderBy(workType => workType.Label)
                        .Select(workType =>
                            new FloatMenuOption(workType.Label, () => { AssignEveryoneWorkTypes.Remove(workType); }))
                        .ToList()));
                }
                buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
                if (Widgets.ButtonText(buttonRect, Strings.ResetWorkTypes))
                {
                    AssignEveryoneWorkTypes.Clear();
                    AssignEveryoneWorkTypes.AddRange(DefaultAssignEveryoneWorkTypes);
                }
            }

            void DoTable()
            {
                DoHeader();
                foreach (var workType in AssignEveryoneWorkTypes)
                {
                    DoRow(workType);
                }
                return;

                void DoHeader()
                {
                    var columnHeadersRect = listing.GetRect(Text.LineHeight * 2 + Text.SpaceBetweenLines);
                    if (Math.Abs(columnHeadersRect.width - _assignEveryoneTableWidth) > 0.1f)
                    {
                        _assignEveryoneTableWidth = columnHeadersRect.width;
                        _assignEveryonePriorityColumnWidth = columnHeadersRect.width * 0.25f;
                        _assignEveryoneAllowDedicatedColumnWidth = columnHeadersRect.width * 0.1f;
                        _assignEveryoneWorkTypeNameColumnWidth = columnHeadersRect.width -
                                                                 (_assignEveryonePriorityColumnWidth +
                                                                  _assignEveryoneAllowDedicatedColumnWidth);
                    }
                    Text.Anchor = TextAnchor.MiddleLeft;
                    var nameRect = new Rect(columnHeadersRect.x, columnHeadersRect.y,
                        _assignEveryoneWorkTypeNameColumnWidth, columnHeadersRect.height);
                    Widgets.Label(nameRect, Strings.WorkTypeName);
                    TooltipHandler.TipRegion(nameRect, Strings.WorkTypeNameTooltip);
                    Text.Anchor = TextAnchor.MiddleCenter;
                    var priorityRect = new Rect(nameRect.xMax, columnHeadersRect.y, _assignEveryonePriorityColumnWidth,
                        columnHeadersRect.height);
                    Widgets.Label(priorityRect, Strings.WorkTypePriority);
                    TooltipHandler.TipRegion(priorityRect, Strings.WorkTypePriorityTooltip);
                    var allowDedicatedRect = new Rect(priorityRect.xMax, columnHeadersRect.y,
                        _assignEveryoneAllowDedicatedColumnWidth, columnHeadersRect.height);
                    Widgets.Label(allowDedicatedRect, Strings.AllowDedicated);
                    TooltipHandler.TipRegion(allowDedicatedRect, Strings.AllowDedicatedTooltip);
                    Text.Anchor = TextAnchor.UpperLeft;
                }

                void DoRow(AssignEveryoneWorkType workType)
                {
                    var rect = listing.GetRect(35f);
                    Text.Anchor = TextAnchor.MiddleLeft;
                    var color = GUI.color;
                    var nameRect = new Rect(rect.x, rect.y, _assignEveryoneWorkTypeNameColumnWidth, rect.height)
                        .ContractedBy(4f);
                    if (!workType.IsWorkTypeLoaded)
                    {
                        TooltipHandler.TipRegion(nameRect, Strings.WorkTypeNotLoadedTooltip);
                        GUI.color = Color.red;
                    }
                    Widgets.Label(nameRect, workType.Label);
                    GUI.color = color;
                    workType.Priority = (int)Widgets.HorizontalSlider(
                        new Rect(rect.x + _assignEveryoneWorkTypeNameColumnWidth, rect.y,
                            _assignEveryonePriorityColumnWidth, rect.height).ContractedBy(4f), workType.Priority, 1f,
                        MaxPriority, true, workType.Priority.ToString(), roundTo: 1);
                    Widgets.Checkbox(
                        rect.x + _assignEveryoneWorkTypeNameColumnWidth + _assignEveryonePriorityColumnWidth +
                        (_assignEveryoneAllowDedicatedColumnWidth / 2f - 12), rect.y, ref workType.AllowDedicated,
                        paintable: true);
                    Text.Anchor = TextAnchor.UpperLeft;
                }
            }
        }

        private static void DoDisabledWorkTypeRow(Rect rect, DisabledWorkType workType)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            var color = GUI.color;
            var nameRect = rect.ContractedBy(4f);
            if (!workType.IsWorkTypeLoaded)
            {
                TooltipHandler.TipRegion(nameRect, Strings.WorkTypeNotLoadedTooltip);
                GUI.color = Color.red;
            }
            Widgets.Label(nameRect, workType.Label);
            GUI.color = color;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DoDisabledWorkTypesButtonRow(Listing listing, List<DisabledWorkType> disabledWorkTypes)
        {
            var buttonRowRect = listing.GetRect(35f);
            var buttonRect = new Rect(buttonRowRect.x, buttonRowRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Strings.AddWorkType))
            {
                var options = new List<FloatMenuOption>();
                foreach (var workTypeDef in DefDatabase<WorkTypeDef>.AllDefsListForReading.Where(w =>
                                 w.visible && !Enumerable.Any(disabledWorkTypes,
                                     workType => workType.WorkTypeDefName.Equals(w.defName,
                                         StringComparison.OrdinalIgnoreCase)))
                             .OrderBy(workTypeDef => workTypeDef.labelShort))
                {
                    options.Add(new FloatMenuOption(workTypeDef.labelShort,
                        () => { disabledWorkTypes.Add(new DisabledWorkType(workTypeDef.defName)); }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Strings.DeleteWorkType, active: disabledWorkTypes.Any()))
            {
                Find.WindowStack.Add(new FloatMenu(disabledWorkTypes.OrderBy(workType => workType.Label)
                    .Select(workType => new FloatMenuOption(workType.Label,
                        () => { disabledWorkTypes.Remove(workType); })).ToList()));
            }
            buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Strings.ResetWorkTypes))
            {
                disabledWorkTypes.Clear();
                disabledWorkTypes.AddRange(DefaultDisabledWorkTypesForForeigners);
            }
        }

        private static void DoDisabledWorkTypesForForeigners(Listing listing)
        {
            var headerRect = listing.GetRect(Text.LineHeight);
            TooltipHandler.TipRegion(headerRect, Strings.DisabledWorkTypesForForeignersTooltip);
            Widgets.Label(headerRect, Strings.DisabledWorkTypesForForeigners);
            DoDisabledWorkTypesButtonRow(listing, DisabledWorkTypesForForeigners);
            DoDisabledWorkTypesTable(listing, DisabledWorkTypesForForeigners);
        }

        private static void DoDisabledWorkTypesForSlaves(Listing listing)
        {
            var headerRect = listing.GetRect(Text.LineHeight);
            TooltipHandler.TipRegion(headerRect, Strings.DisabledWorkTypesForSlavesTooltip);
            Widgets.Label(headerRect, Strings.DisabledWorkTypesForSlaves);
            DoDisabledWorkTypesButtonRow(listing, DisabledWorkTypesForSlaves);
            DoDisabledWorkTypesTable(listing, DisabledWorkTypesForSlaves);
        }

        private static void DoDisabledWorkTypesTable(Listing listing, IEnumerable<DisabledWorkType> disabledWorkTypes)
        {
            var columnHeadersRect = listing.GetRect(Text.LineHeight * 2 + Text.SpaceBetweenLines);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(columnHeadersRect, Strings.WorkTypeName);
            TooltipHandler.TipRegion(columnHeadersRect, Strings.WorkTypeNameTooltip);
            Text.Anchor = TextAnchor.UpperLeft;
            foreach (var workType in disabledWorkTypes)
            {
                DoDisabledWorkTypeRow(listing.GetRect(35f), workType);
            }
        }

        private static void DoWorkTypesTab(Rect rect)
        {
            var listing = new Listing_Standard();
            var contentHeight = Text.LineHeight + 35f + Text.LineHeight * 2 + Text.SpaceBetweenLines +
                                AssignEveryoneWorkTypes.Count * 35f + listing.verticalSpacing + Text.LineHeight + 35f +
                                Text.LineHeight * 2 + Text.SpaceBetweenLines +
                                DisabledWorkTypesForForeigners.Count * 35f + 10000f;
            if (ModsConfig.IdeologyActive)
            {
                contentHeight += listing.verticalSpacing + Text.LineHeight + 35f + Text.LineHeight * 2 +
                                 Text.SpaceBetweenLines + DisabledWorkTypesForSlaves.Count * 35f;
            }
            DoButtonRow();
            var contentRect = new Rect(rect.x, rect.y + UIHelper.ButtonHeight + listing.verticalSpacing, rect.width,
                rect.height - (UIHelper.ButtonHeight + listing.verticalSpacing));
            var viewRect = new Rect(contentRect.x, contentRect.y, contentRect.width - 16f, contentHeight);
            Widgets.BeginScrollView(contentRect, ref _scrollPosition, viewRect);
            listing.Begin(viewRect);
            DoAssignEveryoneWorkTypes(listing);
            listing.GapLine(listing.verticalSpacing);
            DoDisabledWorkTypesForForeigners(listing);
            if (ModsConfig.IdeologyActive)
            {
                listing.GapLine(listing.verticalSpacing);
                DoDisabledWorkTypesForSlaves(listing);
            }
            DoPawnFilter();
            listing.End();
            Widgets.EndScrollView();
            return;

            void DoButtonRow()
            {
                const int buttonCount = 4;
                var buttonWidth = (rect.width - UIHelper.ButtonGap * (buttonCount - 1)) / buttonCount;
                if (Widgets.ButtonText(new Rect(rect.x, rect.y, buttonWidth, UIHelper.ButtonHeight),
                        Strings.SelectWorkType))
                {
                    Find.WindowStack.Add(new FloatMenu(WorkTypeSettings.Select(wts =>
                        new FloatMenuOption(wts.WorkTypeName, () => SelectedWorkTypeSettings = wts)).ToList()));
                }
                if (Widgets.ButtonText(
                        new Rect(rect.x + buttonWidth + UIHelper.ButtonGap, rect.y, buttonWidth, UIHelper.ButtonHeight),
                        Strings.AddWorkType))
                {
                    Find.WindowStack.Add(new FloatMenu(DefDatabase<WorkTypeDef>.AllDefsListForReading
                        .Where(wtd => wtd.visible && !Enumerable.Any(WorkTypeSettings,
                            wts => wtd.defName.Equals(wts.WorkTypeDefName, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(wtd => wtd.labelShort).Select(wtd => new FloatMenuOption(wtd.labelShort, () =>
                        {
                            var wts = new WorkTypeSettings(wtd.defName);
                            WorkTypeSettings.Add(wts);
                            SelectedWorkTypeSettings = wts;
                        })).ToList()));
                }
                if (Widgets.ButtonText(
                        new Rect(rect.x + (buttonWidth + UIHelper.ButtonGap) * 2, rect.y, buttonWidth,
                            UIHelper.ButtonHeight), Strings.DeleteWorkType))
                {
                    Find.WindowStack.Add(new FloatMenu(WorkTypeSettings.Where(wts => wts.WorkTypeDefName != null)
                        .Select(wts => new FloatMenuOption(wts.WorkTypeName, () =>
                        {
                            _ = WorkTypeSettings.Remove(wts);
                            if (wts == SelectedWorkTypeSettings)
                            {
                                SelectedWorkTypeSettings = null;
                            }
                        })).ToList()));
                }
                if (Widgets.ButtonText(
                        new Rect(rect.x + (buttonWidth + UIHelper.ButtonGap) * 3, rect.y, buttonWidth,
                            UIHelper.ButtonHeight), Strings.ResetWorkTypes))
                {
                    WorkTypeSettings.Clear();
                    WorkTypeSettings.AddRange(DefaultWorkTypeSettings);
                }
            }

            void DoPawnFilter() { }
        }

        private static void ExposeWorkTypesData()
        {
            Scribe_Collections.Look(ref AssignEveryoneWorkTypes, nameof(AssignEveryoneWorkTypes), LookMode.Deep);
            Scribe_Collections.Look(ref DisabledWorkTypesForForeigners, nameof(DisabledWorkTypesForForeigners),
                LookMode.Deep);
            Scribe_Collections.Look(ref DisabledWorkTypesForSlaves, nameof(DisabledWorkTypesForSlaves), LookMode.Deep);
            Scribe_Collections.Look(ref WorkTypeSettings, nameof(WorkTypeSettings), LookMode.Deep);
        }

        private static void InitializeWorkTypes()
        {
            if (AssignEveryoneWorkTypes == null)
            {
                AssignEveryoneWorkTypes = new List<AssignEveryoneWorkType>(DefaultAssignEveryoneWorkTypes);
            }
            if (DisabledWorkTypesForForeigners == null)
            {
                DisabledWorkTypesForForeigners = new List<DisabledWorkType>(DefaultDisabledWorkTypesForForeigners);
            }
            if (DisabledWorkTypesForSlaves == null)
            {
                DisabledWorkTypesForSlaves = new List<DisabledWorkType>(DefaultDisabledWorkTypesForSlaves);
            }
            if (WorkTypeSettings == null)
            {
                WorkTypeSettings = new List<WorkTypeSettings>(DefaultWorkTypeSettings);
            }
        }
    }
}