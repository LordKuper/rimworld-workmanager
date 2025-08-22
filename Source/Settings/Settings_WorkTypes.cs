using System;
using System.Collections.Generic;
using System.Linq;
using LordKuper.Common.Filters;
using LordKuper.Common.Helpers;
using LordKuper.Common.UI;
using LordKuper.Common.UI.Widgets;
using UnityEngine;
using Verse;
using Strings = LordKuper.WorkManager.Resources.Strings.Settings.WorkTypes;

namespace LordKuper.WorkManager;

/// <summary>
///     Partial class for managing WorkType-related settings, including UI logic for displaying and editing work type
///     assignment rules.
/// </summary>
public partial class Settings
{
    /// <summary>
    ///     Local input ID for allowed workers pawn capacity.
    /// </summary>
    private const int AllowedWorkersPawnCapacityInputLocalId = 0;

    /// <summary>
    ///     Local input ID for allowed workers pawn skill.
    /// </summary>
    private const int AllowedWorkersPawnSkillInputLocalId = AllowedWorkersPawnCapacityInputLocalId + 100;

    /// <summary>
    ///     Local input ID for allowed workers pawn stat.
    /// </summary>
    private const int AllowedWorkersPawnStatInputLocalId = AllowedWorkersPawnSkillInputLocalId + 100;

    /// <summary>
    ///     Local input ID for dedicated workers pawn capacity.
    /// </summary>
    private const int DedicatedWorkersPawnCapacityInputLocalId = AllowedWorkersPawnCapacityInputLocalId + 50;

    /// <summary>
    ///     Local input ID for dedicated workers pawn skill.
    /// </summary>
    private const int DedicatedWorkersPawnSkillInputLocalId = DedicatedWorkersPawnCapacityInputLocalId + 100;

    /// <summary>
    ///     Local input ID for dedicated workers pawn stat.
    /// </summary>
    private const int DedicatedWorkersPawnStatInputLocalId = DedicatedWorkersPawnSkillInputLocalId + 100;

    /// <summary>
    ///     Provides a function to get the label for a dedicated worker mode.
    /// </summary>
    private static readonly Func<DedicatedWorkerMode, string> DedicatedWorkerModeLabel =
        Resources.Strings.DedicatedWorkerMode.GetDedicatedWorkerModeLabel;

    /// <summary>
    ///     Caches all available dedicated worker modes.
    /// </summary>
    private static readonly List<DedicatedWorkerMode> DedicatedWorkerModesCache =
        Enum.GetValues(typeof(DedicatedWorkerMode)).Cast<DedicatedWorkerMode>().ToList();

    /// <summary>
    ///     Provides a function to get the tooltip for a dedicated worker mode.
    /// </summary>
    private static readonly Func<DedicatedWorkerMode, string> DedicatedWorkerModeTooltip =
        Resources.Strings.DedicatedWorkerMode.GetDedicatedWorkerModeTooltip;

    /// <summary>
    ///     Provides a function to get the label for a nullable dedicated worker mode.
    /// </summary>
    private static readonly Func<DedicatedWorkerMode?, string> DedicatedWorkerNullableModeLabel =
        Resources.Strings.DedicatedWorkerMode.GetDedicatedWorkerModeLabel;

    /// <summary>
    ///     Caches all available nullable dedicated worker modes.
    /// </summary>
    private static readonly List<DedicatedWorkerMode?> DedicatedWorkerNullableModesCache =
        new DedicatedWorkerMode?[] { null }
            .Concat(Enum.GetValues(typeof(DedicatedWorkerMode)).Cast<DedicatedWorkerMode?>()).ToList();

    /// <summary>
    ///     Provides a function to get the tooltip for a nullable dedicated worker mode.
    /// </summary>
    private static readonly Func<DedicatedWorkerMode?, string> DedicatedWorkerNullableModeTooltip =
        Resources.Strings.DedicatedWorkerMode.GetDedicatedWorkerModeTooltip;

    /// <summary>
    ///     List of pawns allowed to work for the selected work type rule.
    /// </summary>
    private readonly List<Pawn> _allowedWorkers = [];

    /// <summary>
    ///     Scroll position for the allowed workers list UI.
    /// </summary>
    private Vector2 _allowedWorkersScrollPosition;

    /// <summary>
    ///     Cached height of the allowed workers section content.
    /// </summary>
    private float _allowedWorkersSectionContentHeight;

    /// <summary>
    ///     Cached height of the assignment section content.
    /// </summary>
    private float _assignmentSectionContentHeight;

    /// <summary>
    ///     Cached height of the dedicated workers section content.
    /// </summary>
    private float _dedicatedWorkersSectionContentHeight;

    /// <summary>
    ///     Currently selected work type rule for editing.
    /// </summary>
    private WorkTypeAssignmentRule _selectedWorkTypeRule;

    /// <summary>
    ///     List of work type assignment rules, initialized with default rules.
    /// </summary>
    private List<WorkTypeAssignmentRule> _workTypeRules = [.. WorkTypeAssignmentRule.DefaultRules];

    /// <summary>
    ///     Cached height of the bottom part of the work types tab.
    /// </summary>
    private float _workTypesBottomHeight;

    /// <summary>
    ///     Cached height of the scrollable content in the work types tab.
    /// </summary>
    private float _workTypesScrollableContentHeight;

    /// <summary>
    ///     Cached height of the top part of the work types tab.
    /// </summary>
    private float _workTypesTopHeight;

    /// <summary>
    ///     Gets or sets the currently selected work type rule for editing.
    ///     When set, updates the allowed workers list.
    /// </summary>
    private WorkTypeAssignmentRule SelectedWorkTypeRule
    {
        get => _selectedWorkTypeRule;
        set
        {
            if (Equals(_selectedWorkTypeRule, value)) return;
            _selectedWorkTypeRule = value;
            UpdateAllowedWorkers();
        }
    }

    /// <summary>
    ///     Gets the list of work type assignment rules as a read-only list.
    /// </summary>
    internal IReadOnlyList<WorkTypeAssignmentRule> WorkTypeRules => _workTypeRules;

    /// <summary>
    ///     Gets the height of the bottom part of the work types tab, calculating if not cached.
    /// </summary>
    private float WorkTypesBottomHeight
    {
        get
        {
            if (_workTypesBottomHeight <= 0f)
                _workTypesBottomHeight = PawnBox.GetPawnBoxHeight(2) + Labels.SectionHeaderHeight + Layout.ElementGap;
            return _workTypesBottomHeight;
        }
    }

    /// <summary>
    ///     Draws a label indicating that no work type rule is selected.
    /// </summary>
    /// <param name="rect">The rectangle in which to draw the label.</param>
    /// <returns>The height of the label drawn.</returns>
    private float DoNoWorkTypeRuleSelectedLabel(Rect rect)
    {
        var labelRect = Layout.GetTopRowRect(rect, Labels.LabelHeight, out _);
        Labels.DoLabel(labelRect, Strings.NoRuleSelected, TextAnchor.MiddleCenter);
        return labelRect.height;
    }

    /// <summary>
    ///     Draws the UI for editing a selected work type rule.
    /// </summary>
    /// <param name="rect">The rectangle in which to draw the rule UI.</param>
    /// <returns>The total height of the UI drawn.</returns>
    private float DoWorkTypeRule(Rect rect)
    {
        var y = 0f;
        var defaultRule = SelectedWorkTypeRule.DefName == null;
        var header = defaultRule
            ? Strings.WorkTypeRuleHeaderDefault
            : string.Format(Strings.WorkTypeRuleHeader, SelectedWorkTypeRule.Label);
        var tooltip = defaultRule
            ? $"{Strings.WorkTypeRuleHeaderTooltipDefault}{Environment.NewLine}{Environment.NewLine}{SelectedWorkTypeRule.Description}"
            : $"{string.Format(Strings.WorkTypeRuleHeaderTooltip, SelectedWorkTypeRule.Label)}{Environment.NewLine}{Environment.NewLine}{SelectedWorkTypeRule.Description}";
        y += Sections.DoSectionHeader(rect, header, tooltip, out var remRect);
        y += Sections.DoLabeledSectionBox(remRect, _assignmentSectionContentHeight, Strings.AssignmentSettingsLabel,
            Strings.AssignmentSettingsTooltip, out var assignmentRect, out remRect);
        var assignmentContentHeight = 0f;
        if (defaultRule)
        {
            var value = SelectedWorkTypeRule.EnsureWorkerAssigned == true;
            assignmentContentHeight += Fields.DoLabeledCheckbox(assignmentRect, 0, null, ref value,
                Strings.EnsureWorkerAssignedLabel, Strings.GetEnsureWorkerAssignedTooltip(false), null,
                out assignmentRect);
            SelectedWorkTypeRule.EnsureWorkerAssigned = value;
        }
        else
        {
            assignmentContentHeight += Fields.DoLabeledCheckbox(assignmentRect, 0, null,
                ref SelectedWorkTypeRule.EnsureWorkerAssigned, Strings.EnsureWorkerAssignedLabel,
                Strings.GetEnsureWorkerAssignedTooltip(true), null, out assignmentRect);
        }
        if (SelectedWorkTypeRule.EnsureWorkerAssigned == true)
            assignmentContentHeight += Fields.DoLabeledIntegerSlider(assignmentRect, 1, null,
                Strings.MinWorkerNumberLabel, Strings.MinWorkerNumberTooltip, ref SelectedWorkTypeRule.MinWorkerNumber,
                1, 10, 1, null, out assignmentRect);
        if (defaultRule)
        {
            var value = SelectedWorkTypeRule.AssignEveryone == true;
            assignmentContentHeight += Fields.DoLabeledCheckbox(assignmentRect, 0, null, ref value,
                Strings.AssignEveryoneLabel, Strings.GetAssignEveryoneTooltip(false), null, out assignmentRect);
            SelectedWorkTypeRule.AssignEveryone = value;
        }
        else
        {
            var value = SelectedWorkTypeRule.AssignEveryone;
            assignmentContentHeight += Fields.DoLabeledCheckbox(assignmentRect, 0, null, ref value,
                Strings.AssignEveryoneLabel, Strings.GetAssignEveryoneTooltip(true), null, out assignmentRect);
            SelectedWorkTypeRule.AssignEveryone = value;
        }
        if (SelectedWorkTypeRule.AssignEveryone == true)
            assignmentContentHeight += Fields.DoLabeledIntegerSlider(assignmentRect, 1, null,
                Strings.AssignEveryonePriorityLabel, Strings.AssignEveryonePriorityTooltip,
                ref SelectedWorkTypeRule.AssignEveryonePriority, 1, MaxWorkTypePriority, 1, null, out assignmentRect);
        if (Event.current.type == EventType.Layout)
            _assignmentSectionContentHeight = assignmentContentHeight;
        if (WorkManagerMod.Settings.UseDedicatedWorkers)
        {
            y += Sections.DoLabeledSectionBox(remRect, _dedicatedWorkersSectionContentHeight,
                Strings.DedicatedWorkerSettingsLabel, Strings.DedicatedWorkerSettingsTooltip,
                out var dedicatedWorkersRect, out remRect);
            var dedicatedWorkersContentHeight = 0f;
            if (defaultRule)
            {
                var value = SelectedWorkTypeRule.DedicatedWorkerSettings.AllowDedicated == true;
                dedicatedWorkersContentHeight += Fields.DoLabeledCheckbox(dedicatedWorkersRect, 0, null, ref value,
                    Strings.AllowDedicatedWorkerLabel, Strings.GetAllowDedicatedWorkerTooltip(false), null,
                    out dedicatedWorkersRect);
                SelectedWorkTypeRule.DedicatedWorkerSettings.AllowDedicated = value;
            }
            else
            {
                dedicatedWorkersContentHeight += Fields.DoLabeledCheckbox(dedicatedWorkersRect, 0, null,
                    ref SelectedWorkTypeRule.DedicatedWorkerSettings.AllowDedicated, Strings.AllowDedicatedWorkerLabel,
                    Strings.GetAllowDedicatedWorkerTooltip(true), null, out dedicatedWorkersRect);
            }
            if (SelectedWorkTypeRule.DedicatedWorkerSettings.AllowDedicated == true)
            {
                var mode = SelectedWorkTypeRule.DedicatedWorkerSettings.Mode;
                if (defaultRule)
                    dedicatedWorkersContentHeight += Fields.DoLabeledSelector(dedicatedWorkersRect, 1, null,
                        Strings.DedicatedWorkerModeLabel, Strings.DedicatedWorkerModeTooltip,
                        mode ?? DedicatedWorkerMode.Constant, DedicatedWorkerModesCache, DedicatedWorkerModeLabel,
                        DedicatedWorkerModeTooltip, m => { SelectedWorkTypeRule.DedicatedWorkerSettings.Mode = m; },
                        null, out dedicatedWorkersRect);
                else
                    dedicatedWorkersContentHeight += Fields.DoLabeledSelector(dedicatedWorkersRect, 1, null,
                        Strings.DedicatedWorkerModeLabel, Strings.DedicatedWorkerModeTooltip, mode,
                        DedicatedWorkerNullableModesCache, DedicatedWorkerNullableModeLabel,
                        DedicatedWorkerNullableModeTooltip,
                        m => { SelectedWorkTypeRule.DedicatedWorkerSettings.Mode = m; }, null,
                        out dedicatedWorkersRect);
                switch (SelectedWorkTypeRule.DedicatedWorkerSettings.Mode)
                {
                    case DedicatedWorkerMode.Constant:
                        dedicatedWorkersContentHeight += Fields.DoLabeledIntegerSlider(dedicatedWorkersRect, 1, null,
                            Strings.ConstantWorkerCountLabel, Strings.ConstantWorkerCountTooltip,
                            ref SelectedWorkTypeRule.DedicatedWorkerSettings.ConstantWorkerCount,
                            DedicatedWorkerSettings.ConstantWorkerCountMin,
                            DedicatedWorkerSettings.ConstantWorkerCountMax, 1, null, out dedicatedWorkersRect);
                        break;
                    case DedicatedWorkerMode.WorkTypeCount:
                        dedicatedWorkersContentHeight += Fields.DoLabeledFloatSlider(dedicatedWorkersRect, 1, null,
                            Strings.WorkTypeCountFactorLabel, Strings.WorkTypeCountFactorTooltip,
                            ref SelectedWorkTypeRule.DedicatedWorkerSettings.WorkTypeCountFactor,
                            DedicatedWorkerSettings.WorkTypeCountFactorMin,
                            DedicatedWorkerSettings.WorkTypeCountFactorMax, 0.05f, null, out dedicatedWorkersRect);
                        break;
                    case DedicatedWorkerMode.CapablePawnRatio:
                        dedicatedWorkersContentHeight += Fields.DoLabeledFloatSlider(dedicatedWorkersRect, 1, null,
                            Strings.CapablePawnRatioFactorLabel, Strings.CapablePawnRatioFactorTooltip,
                            ref SelectedWorkTypeRule.DedicatedWorkerSettings.CapablePawnRatioFactor,
                            DedicatedWorkerSettings.CapablePawnRatioFactorMin,
                            DedicatedWorkerSettings.CapablePawnRatioFactorMax, 0.1f, null, out dedicatedWorkersRect);
                        break;
                    case DedicatedWorkerMode.PawnCount:
                        dedicatedWorkersContentHeight += Fields.DoLabeledFloatSlider(dedicatedWorkersRect, 1, null,
                            Strings.PawnCountFactorLabel, Strings.PawnCountFactorTooltip,
                            ref SelectedWorkTypeRule.DedicatedWorkerSettings.PawnCountFactor,
                            DedicatedWorkerSettings.PawnCountFactorMin, DedicatedWorkerSettings.PawnCountFactorMax,
                            0.1f, null, out dedicatedWorkersRect);
                        dedicatedWorkersContentHeight += PawnFilterWidget.DoPawnFilter(dedicatedWorkersRect,
                            SelectedWorkTypeRule.DedicatedWorkerSettings.PawnCountFilter, PawnFilterSections.All,
                            WorkManagerMod.GetModInputId(DedicatedWorkersPawnSkillInputLocalId),
                            WorkManagerMod.GetModInputId(DedicatedWorkersPawnStatInputLocalId),
                            WorkManagerMod.GetModInputId(DedicatedWorkersPawnCapacityInputLocalId), null, out _);
                        break;
                    case null:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            if (Event.current.type == EventType.Layout)
                _dedicatedWorkersSectionContentHeight = dedicatedWorkersContentHeight;
        }
        y += Sections.DoLabeledSectionBox(remRect, _allowedWorkersSectionContentHeight, Strings.AllowedWorkersLabel,
            Strings.AllowedWorkersTooltip, out var allowedWorkersRect, out _);
        var allowedWorkersContentHeight = PawnFilterWidget.DoPawnFilter(allowedWorkersRect,
            SelectedWorkTypeRule.AllowedWorkers, PawnFilterSections.All,
            WorkManagerMod.GetModInputId(AllowedWorkersPawnSkillInputLocalId),
            WorkManagerMod.GetModInputId(AllowedWorkersPawnStatInputLocalId),
            WorkManagerMod.GetModInputId(AllowedWorkersPawnCapacityInputLocalId), UpdateAllowedWorkers, out _);
        if (Event.current.type == EventType.Layout)
            _allowedWorkersSectionContentHeight = allowedWorkersContentHeight;
        return y;
    }

    /// <summary>
    ///     Draws the main Work Types tab, including the top and scrollable parts.
    /// </summary>
    /// <param name="rect">The rectangle in which to draw the tab.</param>
    private void DoWorkTypesTab(Rect rect)
    {
        var doBottomPart = SelectedWorkTypeRule != null && Find.CurrentMap != null && Find.Maps.Count > 0;
        Tabs.DoTab(rect, _workTypesTopHeight, DoWorkTypesTabTopPart, _workTypesScrollableContentHeight,
            ref _scrollPosition, DoWorkTypesTabScrollablePart, doBottomPart ? WorkTypesBottomHeight : 0f,
            doBottomPart ? DoWorkTypesTabBottomPart : null);
    }

    /// <summary>
    ///     Draws the bottom part of the Work Types tab.
    /// </summary>
    /// <param name="rect">The rectangle in which to draw the bottom part.</param>
    private void DoWorkTypesTabBottomPart(Rect rect)
    {
        var headerRect = Sections.GetSectionHeaderRect(rect, out var remRect);
        var buttonRect = Layout.GetRightColumnRect(headerRect, headerRect.width / 4f, out headerRect);
        Layout.GetRightColumnRect(headerRect, Layout.ElementGap, out headerRect);
        Sections.DoSectionHeaderLabel(headerRect, Strings.AvailablePawnsLabel, Strings.AvailablePawnsTooltip);
        Buttons.DoActionButton(buttonRect, Common.Resources.Strings.Actions.Refresh, UpdateAllowedWorkers);
        Layout.DoVerticalGap(remRect, out remRect);
        PawnBox.DoPawnBox(remRect, ref _allowedWorkersScrollPosition, _allowedWorkers);
    }

    /// <summary>
    ///     Draws the scrollable part of the Work Types tab, including the selected rule or a placeholder label.
    /// </summary>
    /// <param name="rect">The rectangle in which to draw the scrollable content.</param>
    private void DoWorkTypesTabScrollablePart(Rect rect)
    {
        var y = 0f;
        if (SelectedWorkTypeRule == null)
            y += DoNoWorkTypeRuleSelectedLabel(rect);
        else
            y += DoWorkTypeRule(rect);
        if (Event.current.type == EventType.Layout) _workTypesScrollableContentHeight = y;
    }

    /// <summary>
    ///     Manages the user interface and interactions for the top section of the Work Types tab.
    /// </summary>
    /// <remarks>
    ///     This method handles the display and functionality of action buttons for managing work type
    ///     rules, including selecting, adding, deleting, and resetting work type assignments. It dynamically determines
    ///     which work types can be added or deleted based on the current state of the rules and available
    ///     definitions.
    /// </remarks>
    /// <param name="rect">The rectangular area within which the UI elements are rendered.</param>
    private void DoWorkTypesTabTopPart(Rect rect)
    {
        var workTypeRules = _workTypeRules;
        var workTypeRulesCount = workTypeRules.Count;
        var allDefs = WorkManagerGameComponent.Instance.AllWorkTypes;
        List<WorkTypeDef> addableDefs;
        var canAdd = false;
        if (allDefs.Count > 0)
        {
            addableDefs = new List<WorkTypeDef>(allDefs.Count);
            foreach (var def in allDefs)
            {
                if (!def.visible) continue;
                var found = false;
                for (var j = 0; j < workTypeRulesCount; j++)
                {
                    var r = workTypeRules[j];
                    if (def.defName.Equals(r.DefName, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) addableDefs.Add(def);
            }
            canAdd = addableDefs.Count > 0;
        }
        else
        {
            addableDefs = [];
        }
        var canDelete = false;
        List<WorkTypeAssignmentRule> deletableRules;
        if (workTypeRulesCount > 0)
        {
            deletableRules = new List<WorkTypeAssignmentRule>(workTypeRulesCount);
            for (var i = 0; i < workTypeRulesCount; i++)
            {
                var r = workTypeRules[i];
                if (r.DefName != null)
                {
                    deletableRules.Add(r);
                    canDelete = true;
                }
            }
        }
        else
        {
            deletableRules = [];
        }
        _workTypesTopHeight = Buttons.DoActionButtonGrid(rect, [
            new ActionButton(Common.Resources.Strings.Actions.Select, () =>
            {
                Find.WindowStack.Add(new FloatMenu([
                    .. workTypeRules.Select(r => new FloatMenuOption(r.Label, () => { SelectedWorkTypeRule = r; }))
                ]));
            }, Strings.SelectWorkTypeTooltip),
            new ActionButton(Common.Resources.Strings.Actions.Add, () =>
            {
                Find.WindowStack.Add(new FloatMenu([
                    .. addableDefs.OrderBy(def => def.labelShort).Select(def => new FloatMenuOption(def.GetLabel(),
                        () =>
                        {
                            var rule = WorkTypeAssignmentRule.CreateRule(def.defName);
                            workTypeRules.Add(rule);
                            SelectedWorkTypeRule = rule;
                        }))
                ]));
            }, Strings.AddWorkTypeTooltip, canAdd),
            new ActionButton(Common.Resources.Strings.Actions.Delete, () =>
            {
                Find.WindowStack.Add(new FloatMenu([
                    .. deletableRules.Select(r => new FloatMenuOption(r.Label, () =>
                    {
                        _ = workTypeRules.Remove(r);
                        if (r == SelectedWorkTypeRule) SelectedWorkTypeRule = null;
                    }))
                ]));
            }, Strings.DeleteWorkTypeTooltip, canDelete),
            new ActionButton(Common.Resources.Strings.Actions.Reset, () =>
            {
                workTypeRules.Clear();
                workTypeRules.AddRange(WorkTypeAssignmentRule.DefaultRules);
                SelectedWorkTypeRule = null;
            }, Strings.ResetWorkTypesTooltip)
        ], out _);
    }

    /// <summary>
    ///     Exposes work type rules data for saving and loading.
    /// </summary>
    private void ExposeWorkTypesData()
    {
        if (Scribe.mode == LoadSaveMode.Saving) ValidateWorkTypes();
        Scribe_Collections.Look(ref _workTypeRules, nameof(WorkTypeRules), LookMode.Deep);
    }

    /// <summary>
    ///     Updates the list of allowed workers based on the currently selected work type rule.
    /// </summary>
    /// <remarks>
    ///     This method clears the existing list of allowed workers and repopulates it by applying  the
    ///     filtering logic defined in the selected work type rule. If no rule is selected or  the rule does not specify
    ///     allowed workers, the list remains empty.
    /// </remarks>
    private void UpdateAllowedWorkers()
    {
        _allowedWorkers.Clear();
        if (SelectedWorkTypeRule?.AllowedWorkers == null) return;
        if (Find.CurrentMap == null || Find.Maps == null || !Find.Maps.Any()) return;
        _allowedWorkers.AddRange(
            SelectedWorkTypeRule.AllowedWorkers.GetFilteredPawns(Find.Maps, SelectedWorkTypeRule.Def));
    }

    /// <summary>
    ///     Validates the work type rules list, ensuring it is initialized with default rules if null.
    /// </summary>
    private void ValidateWorkTypes()
    {
        _workTypeRules ??= [.. WorkTypeAssignmentRule.DefaultRules];
    }
}