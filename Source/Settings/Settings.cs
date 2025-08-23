using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using LordKuper.Common.UI;
using UnityEngine;
using Verse;
using Strings = LordKuper.WorkManager.Resources.Strings.Settings;

namespace LordKuper.WorkManager;

/// <summary>
///     Represents the mod settings for the Work Manager.
/// </summary>
[UsedImplicitly]
public partial class Settings : ModSettings
{
    /// <summary>
    ///     The list of tab records for the settings window.
    /// </summary>
    private readonly List<TabRecord> _tabs = [];

    /// <summary>
    ///     The currently active tab in the settings window.
    /// </summary>
    private SettingsTabs _currentTab;

    /// <summary>
    ///     Indicates whether the settings have been initialized.
    /// </summary>
    private bool _isInitialized;

    /// <summary>
    ///     The scroll position for the settings window.
    /// </summary>
    private Vector2 _scrollPosition;

    /// <summary>
    ///     Draws the contents of the specified tab in the settings window.
    /// </summary>
    /// <param name="rect">The rectangle area to draw the tab contents.</param>
    /// <param name="tab">The tab to display.</param>
    private void DoTab(Rect rect, SettingsTabs tab)
    {
        switch (tab)
        {
            case SettingsTabs.WorkPriorities:
                DoWorkPrioritiesTab(rect);
                break;
            case SettingsTabs.WorkTypes:
                DoWorkTypesTab(rect);
                break;
            case SettingsTabs.Schedule:
                DoScheduleTab(rect);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(tab));
        }
    }

    /// <summary>
    ///     Draws the window contents for the settings, including tabs and their contents.
    /// </summary>
    /// <param name="rect">The rectangle area to draw the window contents.</param>
    internal void DoWindowContents(Rect rect)
    {
        Initialize();
        var activeTabRect = Tabs.DoTabs(rect, _tabs);
        DoTab(activeTabRect, _currentTab);
    }

    /// <summary>
    ///     Exposes the mod settings data for saving and loading.
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        ExposeVersionData();
        ExposeWorkPrioritiesData();
        ExposeWorkTypesData();
        ExposeSchedulesData();
    }

    /// <summary>
    ///     Initializes the settings if not already initialized.
    /// </summary>
    internal void Initialize()
    {
        if (_isInitialized) return;
        _isInitialized = true;
        InitializeTabs();
        InitializeSchedules();
        Validate();
    }

    /// <summary>
    ///     Initializes the tab records for the settings window.
    /// </summary>
    private void InitializeTabs()
    {
        _tabs.Add(new TabRecord(Strings.WorkPriorities.Title, () =>
        {
            _currentTab = SettingsTabs.WorkPriorities;
            _scrollPosition.Set(0, 0);
        }, () => _currentTab == SettingsTabs.WorkPriorities));
        _tabs.Add(new TabRecord(Strings.WorkTypes.Title, () =>
        {
            _currentTab = SettingsTabs.WorkTypes;
            _scrollPosition.Set(0, 0);
        }, () => _currentTab == SettingsTabs.WorkTypes));
        _tabs.Add(new TabRecord(Strings.Schedule.Title, () =>
        {
            _currentTab = SettingsTabs.Schedule;
            _scrollPosition.Set(0, 0);
        }, () => _currentTab == SettingsTabs.Schedule));
    }

    /// <summary>
    ///     Resets all work-related configurations to their default states.
    /// </summary>
    /// <remarks>
    ///     This method resets work priorities, work types, and schedules to their default values. It is
    ///     intended to restore the system to its initial configuration.
    /// </remarks>
    private void ResetAll()
    {
        ResetWorkPriorities();
        ResetWorkTypes();
        ResetSchedules();
    }

    /// <summary>
    ///     Validates the current state of the object to ensure it meets required conditions.
    /// </summary>
    /// <remarks>
    ///     This method performs internal validation checks. It is
    ///     intended to ensure the object is in a consistent and valid state before further processing.
    /// </remarks>
    internal void Validate()
    {
        ValidateVersion();
        ValidateWorkPriorities();
        ValidateWorkTypes();
    }
}