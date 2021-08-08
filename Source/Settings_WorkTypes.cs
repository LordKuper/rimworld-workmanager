using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Strings = WorkManager.Resources.Strings.Settings.WorkTypes;

namespace WorkManager
{
    public partial class Settings
    {
        private static float _allowDedicatedColumnWidth;
        private static float _priorityColumnWidth;

        private static float _workTypeNameColumnWidth;
        private static float _workTypeTableWidth;

        public static List<AssignEveryoneWorkType> AssignEveryoneWorkTypes =
            new List<AssignEveryoneWorkType>(DefaultAssignEveryoneWorkTypes);

        private static IEnumerable<AssignEveryoneWorkType> DefaultAssignEveryoneWorkTypes =>
            new[]
            {
                new AssignEveryoneWorkType("Firefighter", 1, false),
                new AssignEveryoneWorkType("Patient", 1, false),
                new AssignEveryoneWorkType("PatientBedRest", 1, false),
                new AssignEveryoneWorkType("BasicWorker", 1, false), new AssignEveryoneWorkType("Hauling", 4, true),
                new AssignEveryoneWorkType("Cleaning", 4, true)
            };

        private static void CalculateAssignEveryoneWorkTypesColumnsWidths(Rect rect)
        {
            _priorityColumnWidth = rect.width * 0.25f;
            _allowDedicatedColumnWidth = rect.width * 0.1f;
            _workTypeNameColumnWidth = rect.width - (_priorityColumnWidth + _allowDedicatedColumnWidth);
        }

        private static void DoAssignEveryoneWorkTypesColumnsHeaders(Rect rect)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            var nameRect = new Rect(rect.x, rect.y, _workTypeNameColumnWidth, rect.height);
            Widgets.Label(nameRect, Strings.WorkTypeName);
            TooltipHandler.TipRegion(nameRect, Strings.WorkTypeNameTooltip);
            Text.Anchor = TextAnchor.MiddleCenter;
            var priorityRect = new Rect(nameRect.xMax, rect.y, _priorityColumnWidth, rect.height);
            Widgets.Label(priorityRect, Strings.WorkTypePriority);
            TooltipHandler.TipRegion(priorityRect, Strings.WorkTypePriorityTooltip);
            var allowDedicatedRect = new Rect(priorityRect.xMax, rect.y, _allowDedicatedColumnWidth, rect.height);
            Widgets.Label(allowDedicatedRect, Strings.AllowDedicated);
            TooltipHandler.TipRegion(allowDedicatedRect, Strings.AllowDedicatedTooltip);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DoWorkTypeRow(Rect rect, AssignEveryoneWorkType workType)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            var color = GUI.color;
            var nameRect = new Rect(rect.x, rect.y, _workTypeNameColumnWidth, rect.height).ContractedBy(4f);
            if (!workType.IsWorkTypeLoaded)
            {
                TooltipHandler.TipRegion(nameRect, Strings.WorkTypeNotLoadedTooltip);
                GUI.color = Color.red;
            }
            Widgets.Label(nameRect, workType.Label);
            GUI.color = color;
            workType.Priority = (int)Widgets.HorizontalSlider(
                new Rect(rect.x + _workTypeNameColumnWidth, rect.y, _priorityColumnWidth, rect.height).ContractedBy(4f),
                workType.Priority, 1f, MaxPriority, true, workType.Priority.ToString(), roundTo: 1);
            Widgets.Checkbox(
                rect.x + _workTypeNameColumnWidth + _priorityColumnWidth + (_allowDedicatedColumnWidth / 2f - 12),
                rect.y, ref workType.AllowDedicated, 24f, paintable: true);
            Text.Anchor = TextAnchor.UpperLeft;
        }

        private static void DoWorkTypesTab(Rect rect)
        {
            var listing = new Listing_Standard();
            var height = Text.LineHeight + 35f + Text.LineHeight * 2 + Text.SpaceBetweenLines +
                         AssignEveryoneWorkTypes.Count * 35f;
            var viewRect = new Rect(rect.x, 0, rect.width - 16f, height);
            Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
            listing.Begin(viewRect);
            var headerRect = listing.GetRect(Text.LineHeight);
            TooltipHandler.TipRegion(headerRect, Strings.AssignEveryoneWorkTypesTooltip);
            Widgets.Label(headerRect, Strings.AssignEveryoneWorkTypes);
            var buttonRowRect = listing.GetRect(35f);
            var buttonRect = new Rect(buttonRowRect.x, buttonRowRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Strings.AddAssignEveryoneWorkType))
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
            if (Widgets.ButtonText(buttonRect, Strings.DeleteAssignEveryoneWorkType,
                active: AssignEveryoneWorkTypes.Any()))
            {
                Find.WindowStack.Add(new FloatMenu(AssignEveryoneWorkTypes.OrderBy(workType => workType.Label)
                    .Select(workType =>
                        new FloatMenuOption(workType.Label, () => { AssignEveryoneWorkTypes.Remove(workType); }))
                    .ToList()));
            }
            buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Strings.ResetAssignEveryoneWorkTypes))
            {
                AssignEveryoneWorkTypes.Clear();
                AssignEveryoneWorkTypes.AddRange(DefaultAssignEveryoneWorkTypes);
            }
            var columnHeadersRect = listing.GetRect(Text.LineHeight * 2 + Text.SpaceBetweenLines);
            if (Math.Abs(columnHeadersRect.width - _workTypeTableWidth) > 0.1f)
            {
                _workTypeTableWidth = columnHeadersRect.width;
                CalculateAssignEveryoneWorkTypesColumnsWidths(columnHeadersRect);
            }
            DoAssignEveryoneWorkTypesColumnsHeaders(columnHeadersRect);
            foreach (var workType in AssignEveryoneWorkTypes) { DoWorkTypeRow(listing.GetRect(35f), workType); }
            listing.End();
            Widgets.EndScrollView();
        }

        private static void ExposeWorkTypesData()
        {
            Scribe_Collections.Look(ref AssignEveryoneWorkTypes, nameof(AssignEveryoneWorkTypes), LookMode.Deep);
        }

        private static void InitializeWorkTypes()
        {
            if (AssignEveryoneWorkTypes == null)
            {
                AssignEveryoneWorkTypes = new List<AssignEveryoneWorkType>(DefaultAssignEveryoneWorkTypes);
            }
        }
    }
}