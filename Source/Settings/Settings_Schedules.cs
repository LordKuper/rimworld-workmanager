using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Strings = LordKuper.WorkManager.Resources.Strings.Settings.Schedule;

namespace LordKuper.WorkManager;

public partial class Settings
{
    private float _pawnThresholdColumnWidth;
    private float _timeAssignmentColumnWidth;
    private float _workShiftNumberColumnWidth;
    private float _workShiftTableWidth;
    public List<WorkShift> ColonistWorkShifts = [..DefaultColonistWorkShifts];
    public bool ManageWorkSchedule = true;
    public List<WorkShift> NightOwlWorkShifts = [..DefaultNightOwlWorkShifts];
    public int ScheduleUpdateFrequency = 2;

    [NotNull]
    private static IEnumerable<string> DefaultAfternoonWorkShift
    {
        get
        {
            var shift = new string[24];
            for (var i = 0; i < 24; i++)
            {
                shift[i] = i switch
                {
                    >= 8 and < 16 => "Sleep",
                    < 2 or >= 16 and < 22 => "Work",
                    >= 6 => "Joy",
                    _ => "Anything"
                };
            }
            return shift;
        }
    }

    private static IEnumerable<WorkShift> DefaultColonistWorkShifts =>
    [
        new(DefaultMorningWorkShift, 1), new(DefaultAfternoonWorkShift, 6), new(DefaultNightWorkShift, 9)
    ];

    [NotNull]
    private static IEnumerable<string> DefaultMorningWorkShift
    {
        get
        {
            var shift = new string[24];
            for (var i = 0; i < 24; i++)
            {
                shift[i] = i switch
                {
                    < 6 or >= 22 => "Sleep",
                    >= 8 and < 16 => "Work",
                    < 8 or >= 20 => "Joy",
                    _ => "Anything"
                };
            }
            return shift;
        }
    }

    [NotNull]
    private static IEnumerable<string> DefaultNightOwlWorkShift
    {
        get
        {
            var shift = new string[24];
            for (var i = 0; i < 24; i++)
            {
                shift[i] = i switch
                {
                    >= 11 and < 19 => "Sleep",
                    < 5 or >= 21 => "Work",
                    < 7 or >= 19 => "Joy",
                    _ => "Anything"
                };
            }
            return shift;
        }
    }

    private static IEnumerable<WorkShift> DefaultNightOwlWorkShifts => [new(DefaultNightOwlWorkShift, 1)];

    [NotNull]
    private static IEnumerable<string> DefaultNightWorkShift
    {
        get
        {
            var shift = new string[24];
            for (var i = 0; i < 24; i++)
            {
                shift[i] = i switch
                {
                    >= 12 and < 20 => "Sleep",
                    >= 0 and < 8 => "Work",
                    >= 20 and < 24 => "Joy",
                    _ => "Anything"
                };
            }
            return shift;
        }
    }

    private void CalculateWorkShiftColumnsWidths(Rect rect)
    {
        _workShiftNumberColumnWidth = rect.width * 0.1f;
        _pawnThresholdColumnWidth = rect.width * 0.25f;
        _timeAssignmentColumnWidth = rect.width - (_workShiftNumberColumnWidth + _pawnThresholdColumnWidth);
    }

    private void DoColonistWorkShifts([NotNull] Listing listing)
    {
        var headerRect = listing.GetRect(Text.LineHeight);
        TooltipHandler.TipRegion(headerRect, Strings.ColonistWorkShiftsTooltip);
        Widgets.Label(headerRect, Strings.ColonistWorkShifts);
        var buttonRowRect = listing.GetRect(35f);
        Rect buttonRect = new(buttonRowRect.x, buttonRowRect.y, 150f, 35f);
        if (Widgets.ButtonText(buttonRect, Strings.AddWorkShift))
            ColonistWorkShifts.Add(new WorkShift { PawnThreshold = ColonistWorkShifts.Last().PawnThreshold });
        buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
        if (Widgets.ButtonText(buttonRect, Strings.DeleteWorkShift, active: ColonistWorkShifts.Count > 1))
        {
            List<FloatMenuOption> options = [];
            for (var i = 1; i < ColonistWorkShifts.Count; i++)
            {
                var workShift = ColonistWorkShifts[i];
                options.Add(
                    new FloatMenuOption($"Work shift #{i + 1}", () => { ColonistWorkShifts.Remove(workShift); }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
        if (Widgets.ButtonText(buttonRect, Strings.ResetWorkShifts))
        {
            ColonistWorkShifts.Clear();
            ColonistWorkShifts.AddRange(DefaultColonistWorkShifts);
        }
        var columnHeadersRect = listing.GetRect(Text.LineHeight * 2 + Text.SpaceBetweenLines);
        if (Math.Abs(columnHeadersRect.width - _workShiftTableWidth) > 0.1f)
        {
            _workShiftTableWidth = columnHeadersRect.width;
            CalculateWorkShiftColumnsWidths(columnHeadersRect);
        }
        DoWorkShiftColumnsHeaders(columnHeadersRect);
        for (var i = 0; i < ColonistWorkShifts.Count; i++)
        {
            var workShift = ColonistWorkShifts[i];
            int minThreshold;
            int maxThreshold;
            if (i == 0)
            {
                minThreshold = 1;
                maxThreshold = 1;
            }
            else
            {
                minThreshold = ColonistWorkShifts[i - 1].PawnThreshold;
                maxThreshold = i == ColonistWorkShifts.Count - 1 ? 20 : ColonistWorkShifts[i + 1].PawnThreshold;
            }
            DoWorkShiftRow(listing.GetRect(35f), workShift, i, minThreshold, maxThreshold);
        }
    }

    private void DoNightOwlWorkShifts([NotNull] Listing listing)
    {
        var headerRect = listing.GetRect(Text.LineHeight);
        TooltipHandler.TipRegion(headerRect, Strings.NightOwlWorkShiftsTooltip);
        Widgets.Label(headerRect, Strings.NightOwlWorkShifts);
        var buttonRowRect = listing.GetRect(35f);
        Rect buttonRect = new(buttonRowRect.x, buttonRowRect.y, 150f, 35f);
        if (Widgets.ButtonText(buttonRect, Strings.AddWorkShift))
            NightOwlWorkShifts.Add(new WorkShift { PawnThreshold = NightOwlWorkShifts.Last().PawnThreshold });
        buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
        if (Widgets.ButtonText(buttonRect, Strings.DeleteWorkShift, active: NightOwlWorkShifts.Count > 1))
        {
            List<FloatMenuOption> options = [];
            for (var i = 1; i < NightOwlWorkShifts.Count; i++)
            {
                var workShift = NightOwlWorkShifts[i];
                options.Add(
                    new FloatMenuOption($"Work shift #{i + 1}", () => { NightOwlWorkShifts.Remove(workShift); }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }
        buttonRect = new Rect(buttonRect.xMax + 10f, buttonRect.y, 150f, 35f);
        if (Widgets.ButtonText(buttonRect, Strings.ResetWorkShifts))
        {
            NightOwlWorkShifts.Clear();
            NightOwlWorkShifts.AddRange(DefaultNightOwlWorkShifts);
        }
        var columnHeadersRect = listing.GetRect(Text.LineHeight * 2 + Text.SpaceBetweenLines);
        if (Math.Abs(columnHeadersRect.width - _workShiftTableWidth) > 0.1f)
        {
            _workShiftTableWidth = columnHeadersRect.width;
            CalculateWorkShiftColumnsWidths(columnHeadersRect);
        }
        DoWorkShiftColumnsHeaders(columnHeadersRect);
        for (var i = 0; i < NightOwlWorkShifts.Count; i++)
        {
            var workShift = NightOwlWorkShifts[i];
            int minThreshold;
            int maxThreshold;
            if (i == 0)
            {
                minThreshold = 1;
                maxThreshold = 1;
            }
            else
            {
                minThreshold = NightOwlWorkShifts[i - 1].PawnThreshold;
                maxThreshold = i == NightOwlWorkShifts.Count - 1 ? 20 : NightOwlWorkShifts[i + 1].PawnThreshold;
            }
            DoWorkShiftRow(listing.GetRect(35f), workShift, i, minThreshold, maxThreshold);
        }
    }

    private void DoScheduleTab(Rect rect)
    {
        Listing_Standard listing = new();
        const int boolSettingsCount = 1;
        const int numericSettingsCount = 1;
        var height = boolSettingsCount * (Text.LineHeight + listing.verticalSpacing) +
                     numericSettingsCount * (Text.LineHeight * 1.5f + listing.verticalSpacing) +
                     listing.verticalSpacing + Text.LineHeight * 1.5f + listing.verticalSpacing + Text.LineHeight +
                     35f + Text.LineHeight * 2 + Text.SpaceBetweenLines + 35 * ColonistWorkShifts.Count +
                     listing.verticalSpacing + Text.LineHeight + 35f + Text.LineHeight * 2 + Text.SpaceBetweenLines +
                     35 * NightOwlWorkShifts.Count;
        Rect viewRect = new(rect.x, 0, rect.width - 16f, height);
        Widgets.BeginScrollView(rect, ref _scrollPosition, viewRect);
        listing.Begin(viewRect);
        listing.CheckboxLabeled(Strings.ManageWorkSchedule, ref ManageWorkSchedule, Strings.ManageWorkScheduleTooltip);
        var optionRect = listing.GetRect(Text.LineHeight * 1.5f);
        var fieldRect = optionRect;
        var labelRect = optionRect;
        fieldRect.xMin = optionRect.xMax - optionRect.width * (1 / 2f);
        labelRect.xMax = fieldRect.xMin;
        TooltipHandler.TipRegion(optionRect, Strings.UpdateFrequencyTooltip);
        Widgets.DrawHighlightIfMouseover(optionRect);
        Widgets.Label(labelRect, Strings.UpdateFrequency);
        ScheduleUpdateFrequency = (int)Widgets.HorizontalSlider(fieldRect.ContractedBy(4f), ScheduleUpdateFrequency, 1f,
            24f, true, ScheduleUpdateFrequency.ToString(), roundTo: 1);
        listing.Gap(listing.verticalSpacing);
        listing.GapLine(listing.verticalSpacing);
        DoTimeAssignmentSelector(listing.GetRect(Text.LineHeight * 1.5f));
        listing.GapLine(listing.verticalSpacing);
        DoColonistWorkShifts(listing);
        listing.GapLine(listing.verticalSpacing);
        DoNightOwlWorkShifts(listing);
        listing.End();
        Widgets.EndScrollView();
    }

    private static void DoTimeAssignmentSelector(Rect rect)
    {
        var defCount = DefDatabase<TimeAssignmentDef>.AllDefsListForReading.Count;
        var buttonWidth = rect.width / defCount;
        var selectorRect = rect;
        selectorRect.xMax = selectorRect.x + buttonWidth;
        for (var i = 0; i < defCount; i++)
        {
            var def = DefDatabase<TimeAssignmentDef>.AllDefsListForReading[i];
            var buttonRect = selectorRect.ContractedBy(2f);
            GUI.DrawTexture(buttonRect, def.ColorTexture);
            if (Widgets.ButtonInvisible(buttonRect))
            {
                TimeAssignmentSelector.selectedAssignment = def;
                SoundDefOf.Tick_High.PlayOneShotOnCamera();
            }
            GUI.color = Color.white;
            if (Mouse.IsOver(buttonRect)) Widgets.DrawHighlight(buttonRect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Widgets.Label(buttonRect, def.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
            if (TimeAssignmentSelector.selectedAssignment == def)
                Widgets.DrawBox(buttonRect, 2);
            else
                UIHighlighter.HighlightOpportunity(buttonRect, def.cachedHighlightNotSelectedTag);
            selectorRect.x += buttonWidth;
        }
    }

    private void DoWorkShiftColumnsHeaders(Rect rect)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        Rect numberRect = new(rect.x, rect.y, _workShiftNumberColumnWidth, rect.height);
        Widgets.Label(numberRect, Strings.WorkShiftNumberColumnHeader);
        Rect timeAssignmentRect = new(numberRect.xMax, rect.y, _timeAssignmentColumnWidth, rect.height);
        Widgets.Label(timeAssignmentRect, Strings.WorkShiftColumnHeader);
        TooltipHandler.TipRegion(timeAssignmentRect, Strings.WorkShiftColumnHeaderTooltip);
        Rect thresholdRect = new(timeAssignmentRect.xMax, rect.y, _pawnThresholdColumnWidth, rect.height);
        Widgets.Label(thresholdRect, Strings.WorkShiftThresholdColumnHeader);
        TooltipHandler.TipRegion(thresholdRect, Strings.WorkShiftThresholdColumnHeaderTooltip);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DoWorkShiftRow(Rect rect, [NotNull] WorkShift workShift, int number, int minThreshold,
        int maxThreshold)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        var numberRect = new Rect(rect.x, rect.y, _workShiftNumberColumnWidth, rect.height).ContractedBy(4f);
        Widgets.Label(numberRect, (number + 1).ToString("N0"));
        var timeAssignmentRect =
            new Rect(numberRect.xMax, rect.y, _timeAssignmentColumnWidth, rect.height).ContractedBy(4f);
        var cellWidth = timeAssignmentRect.width / 24f;
        Text.Font = GameFont.Tiny;
        for (var hour = 0; hour < 24; hour++)
        {
            var cellRect = new Rect(timeAssignmentRect.x + hour * cellWidth, timeAssignmentRect.y, cellWidth,
                timeAssignmentRect.height).ContractedBy(1f);
            var assignment = workShift.GetTimeAssignment(hour);
            GUI.DrawTexture(cellRect, assignment.ColorTexture);
            Widgets.Label(cellRect.ContractedBy(2f), hour.ToString("N0"));
            if (Mouse.IsOver(cellRect))
            {
                Widgets.DrawBox(cellRect, 2);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    TimeAssignmentSelector.selectedAssignment != assignment)
                {
                    SoundDefOf.Designate_DragStandard_Changed_NoCam.PlayOneShotOnCamera();
                    workShift.SetTimeAssignment(hour, TimeAssignmentSelector.selectedAssignment);
                }
            }
        }
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        var thresholdRect = new Rect(timeAssignmentRect.xMax, rect.y, _pawnThresholdColumnWidth, rect.height)
            .ContractedBy(4f);
        if (number == 0)
            Widgets.Label(thresholdRect, workShift.PawnThreshold.ToString("N0"));
        else
            workShift.PawnThreshold = (int)Widgets.HorizontalSlider(thresholdRect, workShift.PawnThreshold,
                minThreshold, maxThreshold, true, workShift.PawnThreshold.ToString(), roundTo: 1);
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void ExposeSchedulesData()
    {
        Scribe_Values.Look(ref ManageWorkSchedule, nameof(ManageWorkSchedule), true);
        Scribe_Values.Look(ref ScheduleUpdateFrequency, nameof(ScheduleUpdateFrequency), 2);
        Scribe_Collections.Look(ref ColonistWorkShifts, nameof(ColonistWorkShifts), LookMode.Deep);
        Scribe_Collections.Look(ref NightOwlWorkShifts, nameof(NightOwlWorkShifts), LookMode.Deep);
    }

    private void InitializeSchedules()
    {
        if (ScheduleUpdateFrequency == 0) ScheduleUpdateFrequency = 2;
        ColonistWorkShifts ??= [..DefaultColonistWorkShifts];
        NightOwlWorkShifts ??= [..DefaultNightOwlWorkShifts];
    }

    private void ResetSchedules()
    {
        ManageWorkSchedule = true;
        ScheduleUpdateFrequency = 2;
        ColonistWorkShifts.Clear();
        ColonistWorkShifts.AddRange(DefaultColonistWorkShifts);
        NightOwlWorkShifts.Clear();
        NightOwlWorkShifts.AddRange(DefaultNightOwlWorkShifts);
    }
}