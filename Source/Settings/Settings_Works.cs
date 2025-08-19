using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LordKuper.WorkManager.Helpers;
using UnityEngine;
using Verse;
using Strings = LordKuper.WorkManager.Resources.Strings.Settings.WorkTypes;

namespace LordKuper.WorkManager;

public partial class Settings
{
    private float _assignEveryoneAllowDedicatedColumnWidth;
    private float _assignEveryonePriorityColumnWidth;
    private float _assignEveryoneTableWidth;
    private float _assignEveryoneWorkTypeNameColumnWidth;
    private WorkTypeSettings _selectedWorkTypeSettings;
    public List<AssignEveryoneWorkType> AssignEveryoneWorkTypes = [..DefaultAssignEveryoneWorkTypes];
    public List<DisabledWorkType> DisabledWorkTypesForForeigners = [..DefaultDisabledWorkTypesForForeigners];
    public List<DisabledWorkType> DisabledWorkTypesForSlaves = [..DefaultDisabledWorkTypesForSlaves];
    public List<WorkTypeSettings> WorkTypeSettings = [..DefaultWorkTypeSettings];

    private static IEnumerable<AssignEveryoneWorkType> DefaultAssignEveryoneWorkTypes =>
    [
        new("Firefighter", 1, false),
        new("Patient", 1, false),
        new("PatientBedRest", 1, false),
        new("BasicWorker", 1, false),
        new("Hauling", 4, true),
        new("Cleaning", 4, true)
    ];

    private static IEnumerable<DisabledWorkType> DefaultDisabledWorkTypesForForeigners =>
    [
        new("Art"), new("Crafting"), new("Tailoring"), new("Smithing"), new("Construction")
    ];

    private static IEnumerable<DisabledWorkType> DefaultDisabledWorkTypesForSlaves =>
    [
        new("Art"), new("Crafting"), new("Tailoring"), new("Smithing"), new("Construction")
    ];

    private static IEnumerable<WorkTypeSettings> DefaultWorkTypeSettings =>
    [
        new(),
        new("Doctor")
        {
            DedicatedWorkersMode = DedicatedWorkerMode.PawnCount
        }
    ];

    private void DoAssignEveryoneWorkTypes([NotNull] Listing listing)
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
            Rect buttonRect = new(buttonRowRect.x, buttonRowRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Strings.AddWorkType))
                Find.WindowStack.Add(new FloatMenu([
                    .. DefDatabase<WorkTypeDef>.AllDefsListForReading
                        .Where(w => w.visible && !Enumerable.Any(AssignEveryoneWorkTypes,
                            workType => workType.WorkTypeDefName.Equals(w.defName, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(wtd => wtd.labelShort).Select(wtd => new FloatMenuOption(wtd.labelShort,
                            () => { AssignEveryoneWorkTypes.Add(new AssignEveryoneWorkType(wtd.defName, 1, false)); }))
                ]));
            buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
            if (Widgets.ButtonText(buttonRect, Strings.DeleteWorkType, active: AssignEveryoneWorkTypes.Any()))
                Find.WindowStack.Add(new FloatMenu([
                    .. AssignEveryoneWorkTypes.OrderBy(workType => workType.Label).Select(workType =>
                        new FloatMenuOption(workType.Label, () => { _ = AssignEveryoneWorkTypes.Remove(workType); }))
                ]));
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
                Rect nameRect = new(columnHeadersRect.x, columnHeadersRect.y, _assignEveryoneWorkTypeNameColumnWidth,
                    columnHeadersRect.height);
                Widgets.Label(nameRect, Strings.WorkTypeName);
                TooltipHandler.TipRegion(nameRect, Strings.WorkTypeNameTooltip);
                Text.Anchor = TextAnchor.MiddleCenter;
                Rect priorityRect = new(nameRect.xMax, columnHeadersRect.y, _assignEveryonePriorityColumnWidth,
                    columnHeadersRect.height);
                Widgets.Label(priorityRect, Strings.WorkTypePriority);
                TooltipHandler.TipRegion(priorityRect, Strings.WorkTypePriorityTooltip);
                Rect allowDedicatedRect = new(priorityRect.xMax, columnHeadersRect.y,
                    _assignEveryoneAllowDedicatedColumnWidth, columnHeadersRect.height);
                Widgets.Label(allowDedicatedRect, Strings.AllowDedicated);
                TooltipHandler.TipRegion(allowDedicatedRect, Strings.AllowDedicatedTooltip);
                Text.Anchor = TextAnchor.UpperLeft;
            }

            void DoRow([NotNull] AssignEveryoneWorkType workType)
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
                    MaxWorkTypePriority, true, workType.Priority.ToString(), roundTo: 1);
                Widgets.Checkbox(
                    rect.x + _assignEveryoneWorkTypeNameColumnWidth + _assignEveryonePriorityColumnWidth +
                    (_assignEveryoneAllowDedicatedColumnWidth / 2f - 12), rect.y, ref workType.AllowDedicated,
                    paintable: true);
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }
    }

    private static void DoDisabledWorkTypeRow(Rect rect, [NotNull] DisabledWorkType workType)
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

    private void DoDisabledWorkTypesButtonRow([NotNull] Listing listing, List<DisabledWorkType> disabledWorkTypes)
    {
        var buttonRowRect = listing.GetRect(35f);
        Rect buttonRect = new(buttonRowRect.x, buttonRowRect.y, 150f, 35f);
        if (Widgets.ButtonText(buttonRect, Strings.AddWorkType))
        {
            List<FloatMenuOption> options = [];
            options.AddRange(DefDatabase<WorkTypeDef>.AllDefsListForReading
                .Where(w => w.visible && !Enumerable.Any(disabledWorkTypes,
                    workType => workType.WorkTypeDefName.Equals(w.defName, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(workTypeDef => workTypeDef.labelShort).Select(workTypeDef =>
                    new FloatMenuOption(workTypeDef.labelShort,
                        () => { disabledWorkTypes.Add(new DisabledWorkType(workTypeDef.defName)); })));
            Find.WindowStack.Add(new FloatMenu(options));
        }
        buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
        if (Widgets.ButtonText(buttonRect, Strings.DeleteWorkType, active: disabledWorkTypes.Any()))
            Find.WindowStack.Add(new FloatMenu([
                .. disabledWorkTypes.OrderBy(workType => workType.Label).Select(workType =>
                    new FloatMenuOption(workType.Label, () => { _ = disabledWorkTypes.Remove(workType); }))
            ]));
        buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
        if (Widgets.ButtonText(buttonRect, Strings.ResetWorkTypes))
        {
            disabledWorkTypes.Clear();
            disabledWorkTypes.AddRange(DefaultDisabledWorkTypesForForeigners);
        }
    }

    private void DoDisabledWorkTypesForForeigners([NotNull] Listing listing)
    {
        var headerRect = listing.GetRect(Text.LineHeight);
        TooltipHandler.TipRegion(headerRect, Strings.DisabledWorkTypesForForeignersTooltip);
        Widgets.Label(headerRect, Strings.DisabledWorkTypesForForeigners);
        DoDisabledWorkTypesButtonRow(listing, DisabledWorkTypesForForeigners);
        DoDisabledWorkTypesTable(listing, DisabledWorkTypesForForeigners);
    }

    private void DoDisabledWorkTypesForSlaves([NotNull] Listing listing)
    {
        var headerRect = listing.GetRect(Text.LineHeight);
        TooltipHandler.TipRegion(headerRect, Strings.DisabledWorkTypesForSlavesTooltip);
        Widgets.Label(headerRect, Strings.DisabledWorkTypesForSlaves);
        DoDisabledWorkTypesButtonRow(listing, DisabledWorkTypesForSlaves);
        DoDisabledWorkTypesTable(listing, DisabledWorkTypesForSlaves);
    }

    private static void DoDisabledWorkTypesTable([NotNull] Listing listing,
        [NotNull] IEnumerable<DisabledWorkType> disabledWorkTypes)
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

    private void DoWorksTab(Rect rect)
    {
        Listing_Standard listing = new();
        var contentHeight = Text.LineHeight + 35f + Text.LineHeight * 2 + Text.SpaceBetweenLines +
                            AssignEveryoneWorkTypes.Count * 35f + listing.verticalSpacing + Text.LineHeight + 35f +
                            Text.LineHeight * 2 + Text.SpaceBetweenLines + DisabledWorkTypesForForeigners.Count * 35f +
                            10000f;
        if (ModsConfig.IdeologyActive)
            contentHeight += listing.verticalSpacing + Text.LineHeight + 35f + Text.LineHeight * 2 +
                             Text.SpaceBetweenLines + DisabledWorkTypesForSlaves.Count * 35f;
        DoButtonRow();
        Rect contentRect = new(rect.x, rect.y + UIHelper.ButtonHeight + listing.verticalSpacing, rect.width,
            rect.height - (UIHelper.ButtonHeight + listing.verticalSpacing));
        Rect viewRect = new(contentRect.x, contentRect.y, contentRect.width - 16f, contentHeight);
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
                    Common.Resources.Strings.Actions.Select))
                Find.WindowStack.Add(new FloatMenu([
                    .. WorkTypeSettings.Select(wts =>
                        new FloatMenuOption(wts.WorkTypeName, () => _selectedWorkTypeSettings = wts))
                ]));
            if (Widgets.ButtonText(
                    new Rect(rect.x + buttonWidth + UIHelper.ButtonGap, rect.y, buttonWidth, UIHelper.ButtonHeight),
                    Strings.AddWorkType))
                Find.WindowStack.Add(new FloatMenu([
                    .. DefDatabase<WorkTypeDef>.AllDefsListForReading
                        .Where(wtd => wtd.visible && !Enumerable.Any(WorkTypeSettings,
                            wts => wtd.defName.Equals(wts.WorkTypeDefName, StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(wtd => wtd.labelShort).Select(wtd => new FloatMenuOption(wtd.labelShort, () =>
                        {
                            WorkTypeSettings wts = new(wtd.defName);
                            WorkTypeSettings.Add(wts);
                            _selectedWorkTypeSettings = wts;
                        }))
                ]));
            if (Widgets.ButtonText(
                    new Rect(rect.x + (buttonWidth + UIHelper.ButtonGap) * 2, rect.y, buttonWidth,
                        UIHelper.ButtonHeight), Strings.DeleteWorkType))
                Find.WindowStack.Add(new FloatMenu([
                    .. WorkTypeSettings.Where(wts => wts.WorkTypeDefName != null).Select(wts =>
                        new FloatMenuOption(wts.WorkTypeName, () =>
                        {
                            _ = WorkTypeSettings.Remove(wts);
                            if (wts == _selectedWorkTypeSettings) _selectedWorkTypeSettings = null;
                        }))
                ]));
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

    private void ExposeWorksData()
    {
        Scribe_Collections.Look(ref AssignEveryoneWorkTypes, nameof(AssignEveryoneWorkTypes), LookMode.Deep);
        Scribe_Collections.Look(ref DisabledWorkTypesForForeigners, nameof(DisabledWorkTypesForForeigners),
            LookMode.Deep);
        Scribe_Collections.Look(ref DisabledWorkTypesForSlaves, nameof(DisabledWorkTypesForSlaves), LookMode.Deep);
        Scribe_Collections.Look(ref WorkTypeSettings, nameof(WorkTypeSettings), LookMode.Deep);
    }

    private void InitializeWorks()
    {
        AssignEveryoneWorkTypes ??= [..DefaultAssignEveryoneWorkTypes];
        DisabledWorkTypesForForeigners ??= [..DefaultDisabledWorkTypesForForeigners];
        DisabledWorkTypesForSlaves ??= [..DefaultDisabledWorkTypesForSlaves];
        WorkTypeSettings ??= [..DefaultWorkTypeSettings];
    }
}